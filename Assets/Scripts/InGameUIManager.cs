using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    [Header("Opening Sequence UI")]
    [SerializeField] private CanvasGroup _openingSequenceCanvasGroup;
    [SerializeField] private TextMeshProUGUI _openingSequenceText;
    [SerializeField] private float _openingFadeDuration = 1f;
    [SerializeField] private float _openingDisplayDuration = 2f;

    [Header("Hand References")]
    [SerializeField] private Transform _handContainer;
    [SerializeField] private GameObject _cardUIPrefab;
    [SerializeField] private RectTransform _deckPosition;
    [SerializeField] private RectTransform _dropZone;
    [SerializeField] private Canvas _mainCanvas;
    [SerializeField] private UIPopup_CardSelect _cardSelectPopup;
    [SerializeField] private GameObject _blockingPanel;
    [SerializeField] private Transform _cardPoolContainer;

    [Header("Hand Layout")]
    [SerializeField] private float _spreadAngle = 10f;
    [SerializeField] private float _cardSpacing = 100f;
    [SerializeField] private float _baseYPosition = 150f;
    [SerializeField] private float _collapsedYPosition = -50f;
    [SerializeField] private float _hoverScaleMultiplier = 1.2f;
    [SerializeField] private float _hoverYOffset = 50f;
    [SerializeField] private float _arcCorrectionFactor = 5f;
    [SerializeField] private Vector2 _expandedCardScale = Vector2.one;
    [SerializeField] private Vector2 _collapsedCardScale = new Vector2(0.8f, 0.8f);
    [SerializeField] private float _lerpSpeed = 10f;

    [Header("Battle HUD")]
    [SerializeField] private TextMeshProUGUI _preparationTimeText;
    [SerializeField] private TextMeshProUGUI _playerCurrentCost;
    [SerializeField] private GameObject _usedAttackCardsContent;
    [SerializeField] private GameObject _usedDefenseCardsContent;
    [SerializeField] private Vector2 _usedCardUISize = new Vector2(246f, 90f);
    [SerializeField] private Button _btnTurnEnd;

    private readonly List<GameObject> _usedCardUIRoots = new List<GameObject>();

    private InGameHandUI _handUI;
    private BattlePreparationController _preparationController;
    private bool _isInitialized;
    private bool _openingSequenceStarted;

    public bool IsDragging => _handUI != null && _handUI.IsDragging;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolvePreparationTimeText();
        ConfigureChildComponents();
    }

    private void Start()
    {
        if (!GameConfig.ENABLE_TEST_MODE) return;

        Debug.LogWarning("--- RUNNING IN TEST MODE ---");
        InGameCardManager.Instance?.TestInitialize();
        Initialize();
        ShowOpeningSequence();
    }

    private void OnDestroy()
    {
        if (GameTurnManager.Instance != null && _isInitialized)
        {
            GameTurnManager.Instance.OnCostChanged -= UpdateCostDisplay;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Initialize()
    {
        if (_isInitialized) return;

        _handUI.Initialize();
        if (GameTurnManager.Instance != null)
        {
            GameTurnManager.Instance.OnCostChanged += UpdateCostDisplay;
            UpdateCostDisplay(GameTurnManager.Instance.CurrentCost);
        }

        _isInitialized = true;
    }

    public void ShowOpeningSequence()
    {
        if (_openingSequenceStarted) return;

        _openingSequenceStarted = true;
        StartCoroutine(OpeningSequenceCoroutine());
    }

    private IEnumerator OpeningSequenceCoroutine()
    {
        if (GameConfig.ENABLE_TEST_MODE)
        {
            FinishOpeningSequence();
            yield break;
        }

        if (_openingSequenceCanvasGroup == null || _openingSequenceText == null)
        {
            Debug.LogWarning("[InGameUIManager] Opening sequence UI is missing. Skipping animation.");
            FinishOpeningSequence();
            yield break;
        }

        _openingSequenceCanvasGroup.alpha = 0f;
        _openingSequenceCanvasGroup.gameObject.SetActive(true);
        _openingSequenceText.text = "제한 시간 내에 전투를 준비하세요!";

        _openingSequenceCanvasGroup.DOFade(1f, _openingFadeDuration);
        yield return new WaitForSeconds(_openingFadeDuration + _openingDisplayDuration);

        _openingSequenceCanvasGroup.DOFade(0f, _openingFadeDuration);
        yield return new WaitForSeconds(_openingFadeDuration);

        _openingSequenceCanvasGroup.gameObject.SetActive(false);
        FinishOpeningSequence();
    }

    private void FinishOpeningSequence()
    {
        if (InGameCardManager.Instance == null)
        {
            Debug.LogError("[InGameUIManager] InGameCardManager is missing.");
            return;
        }

        InGameCardManager.Instance.DrawInitialHand();
        _preparationController.NotifyOpeningSequenceFinished();
    }

    private void ConfigureChildComponents()
    {
        _handUI = GetComponent<InGameHandUI>();
        if (_handUI == null)
        {
            _handUI = gameObject.AddComponent<InGameHandUI>();
        }

        _handUI.Configure(
            _handContainer,
            _cardUIPrefab,
            _deckPosition,
            _mainCanvas,
            _cardSelectPopup,
            _blockingPanel,
            _cardPoolContainer,
            _spreadAngle,
            _cardSpacing,
            _baseYPosition,
            _collapsedYPosition,
            _hoverScaleMultiplier,
            _hoverYOffset,
            _arcCorrectionFactor,
            _expandedCardScale,
            _collapsedCardScale,
            _lerpSpeed);

        _preparationController = GetComponent<BattlePreparationController>();
        if (_preparationController == null)
        {
            _preparationController = gameObject.AddComponent<BattlePreparationController>();
        }

        _preparationController.Configure(_handUI, this, _btnTurnEnd);
    }

    private void ResolvePreparationTimeText()
    {
        if (_preparationTimeText != null) return;

        GameObject timerObject = GameObject.Find("TxtRoundInfo");
        if (timerObject != null)
        {
            _preparationTimeText = timerObject.GetComponent<TextMeshProUGUI>();
        }
    }

    public void SetPreparationTime(int remainingSeconds)
    {
        if (_preparationTimeText != null)
        {
            _preparationTimeText.text = $"전투 준비 남은 시간: {remainingSeconds}";
        }
    }

    public void AddUsedAttackCard(CardData card)
    {
        AddUsedCardToInfoPanel(card, _usedAttackCardsContent);
    }

    public void AddUsedDefenseCard(CardData card)
    {
        AddUsedCardToInfoPanel(card, _usedDefenseCardsContent);
    }

    private void AddUsedCardToInfoPanel(CardData card, GameObject contentRoot)
    {
        if (card == null || contentRoot == null || _cardUIPrefab == null)
        {
            Debug.LogWarning("[InGameUIManager] Cannot add used card UI because a reference is missing.");
            return;
        }

        GameObject cardRoot = Instantiate(_cardUIPrefab, contentRoot.transform);
        cardRoot.name = $"UsedCardUI_{card.cardName}";
        cardRoot.SetActive(true);
        ConfigureUsedCardRect(cardRoot);

        CardUI cardUI = cardRoot.GetComponentInChildren<CardUI>(true);
        if (cardUI == null)
        {
            Destroy(cardRoot);
            Debug.LogError("[InGameUIManager] CardUI component was not found in the used-card prefab.");
            return;
        }

        cardUI.InitializeDisplay(card);
        cardUI.UpdateView(true);
        FitUsedCardInDeckDisplay(cardRoot);

        CanvasGroup canvasGroup = cardRoot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        _usedCardUIRoots.Add(cardRoot);
    }

    private void ConfigureUsedCardRect(GameObject cardRoot)
    {
        RectTransform rectTransform = cardRoot.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = _usedCardUISize;

        LayoutElement layoutElement = cardRoot.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = cardRoot.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = false;
        layoutElement.minWidth = _usedCardUISize.x;
        layoutElement.minHeight = _usedCardUISize.y;
        layoutElement.preferredWidth = _usedCardUISize.x;
        layoutElement.preferredHeight = _usedCardUISize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private static void FitUsedCardInDeckDisplay(GameObject cardRoot)
    {
        RectTransform cardInDeckRect = cardRoot.transform.Find("CardUIInDeck") as RectTransform;
        if (cardInDeckRect == null) return;

        cardInDeckRect.anchorMin = Vector2.zero;
        cardInDeckRect.anchorMax = Vector2.one;
        cardInDeckRect.pivot = new Vector2(0.5f, 0.5f);
        cardInDeckRect.anchoredPosition = Vector2.zero;
        cardInDeckRect.sizeDelta = Vector2.zero;
        cardInDeckRect.localScale = Vector3.one;
    }

    private void UpdateCostDisplay(int currentCost)
    {
        _handUI?.UpdateCardInteractableStates(currentCost);

        if (_playerCurrentCost != null)
        {
            _playerCurrentCost.text = $"현재 코스트: {currentCost}";
        }
    }

    public void HideHandForCombat()
    {
        _handUI?.HideForCombat();
    }

    public void ShowHandForNewRound()
    {
        _handUI?.ShowForNewRound();
    }

    public void SetHoveredCard(CardUI cardUI)
    {
        _handUI?.SetHoveredCard(cardUI);
    }

    public void ClearHoveredCard(CardUI cardUI)
    {
        _handUI?.ClearHoveredCard(cardUI);
    }

    public void OnCardBeginDrag(CardUI cardUI)
    {
        _handUI?.OnCardBeginDrag(cardUI);
    }

    public void OnCardDrag(PointerEventData eventData)
    {
        _handUI?.OnCardDrag(eventData);
    }

    public void OnCardEndDrag(CardUI cardUI, PointerEventData eventData)
    {
        _handUI?.OnCardEndDrag(cardUI, eventData);
    }

    public void RestoreCardToHand(GameObject cardRoot, int originalIndex)
    {
        _handUI?.RestoreCardToHand(cardRoot, originalIndex);
    }

    public float GetHandPlayableEffectTime()
    {
        return _handUI != null ? _handUI.GetPlayableEffectTime() : 0f;
    }
}
