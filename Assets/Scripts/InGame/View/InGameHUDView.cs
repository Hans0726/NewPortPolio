using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameHUDView : MonoBehaviour
{
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
    private InGameMatchState _matchState;
    private bool _initialized;
    private bool _openingSequenceStarted;

    public event Action TurnEndRequested;

    public InGameHandUI HandUI => _handUI;

    private void Awake()
    {
        ResolvePreparationTimeText();
        ConfigureHandView();
    }

    public void Initialize(InGameMatchState matchState, InGameCardState cardState)
    {
        if (_initialized) return;
        if (matchState == null || cardState == null)
        {
            Debug.LogError("[InGameHUDView] State references are missing.");
            return;
        }

        _matchState = matchState;
        _matchState.CostChanged += RenderCost;
        _btnTurnEnd?.onClick.AddListener(HandleTurnEndClicked);
        _handUI.Initialize(cardState, () => _matchState.CurrentCost);
        RenderCost(_matchState.CurrentCost);
        SetTurnEndInteractable(false);
        _initialized = true;
    }

    public void Dispose()
    {
        if (!_initialized) return;

        _matchState.CostChanged -= RenderCost;
        _btnTurnEnd?.onClick.RemoveListener(HandleTurnEndClicked);
        _initialized = false;
    }

    public void PlayOpeningSequence(Action onFinished)
    {
        if (_openingSequenceStarted) return;

        _openingSequenceStarted = true;
        StartCoroutine(OpeningSequenceCoroutine(onFinished));
    }

    public void SetPreparationTime(int remainingSeconds)
    {
        if (_preparationTimeText != null)
        {
            _preparationTimeText.text = $"전투 준비 남은 시간: {remainingSeconds}";
        }
    }

    public void SetTurnEndInteractable(bool interactable)
    {
        if (_btnTurnEnd != null)
        {
            _btnTurnEnd.interactable = interactable;
        }
    }

    public void AddUsedAttackCard(CardData card) =>
        AddUsedCardToInfoPanel(card, _usedAttackCardsContent);

    public void AddUsedDefenseCard(CardData card) =>
        AddUsedCardToInfoPanel(card, _usedDefenseCardsContent);

    public void HideHandForCombat() => _handUI?.HideForCombat();

    public void ShowInitialHand() => _handUI?.ShowInitialHand();

    public void ShowHandForNewRound() => _handUI?.ShowForNewRound();

    private IEnumerator OpeningSequenceCoroutine(Action onFinished)
    {
        if (GameConfig.ENABLE_TEST_MODE ||
            _openingSequenceCanvasGroup == null ||
            _openingSequenceText == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        _openingSequenceCanvasGroup.DOKill();
        _openingSequenceCanvasGroup.alpha = 0f;
        _openingSequenceCanvasGroup.gameObject.SetActive(true);
        _openingSequenceText.text = "제한 시간 안에 전투를 준비하세요!";

        yield return _openingSequenceCanvasGroup
            .DOFade(1f, _openingFadeDuration)
            .WaitForCompletion();
        yield return new WaitForSeconds(_openingDisplayDuration);
        yield return _openingSequenceCanvasGroup
            .DOFade(0f, _openingFadeDuration)
            .WaitForCompletion();

        _openingSequenceCanvasGroup.gameObject.SetActive(false);
        onFinished?.Invoke();
    }

    private void ConfigureHandView()
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

    private void RenderCost(int currentCost)
    {
        _handUI?.UpdateCardInteractableStates(currentCost);
        if (_playerCurrentCost != null)
        {
            _playerCurrentCost.text = $"현재 코스트: {currentCost}";
        }
    }

    private void HandleTurnEndClicked() => TurnEndRequested?.Invoke();

    private void AddUsedCardToInfoPanel(CardData card, GameObject contentRoot)
    {
        if (card == null || contentRoot == null || _cardUIPrefab == null) return;

        GameObject cardRoot = Instantiate(_cardUIPrefab, contentRoot.transform);
        cardRoot.name = $"UsedCardUI_{card.cardName}";
        cardRoot.SetActive(true);
        ConfigureUsedCardRect(cardRoot);

        CardUI cardUI = cardRoot.GetComponentInChildren<CardUI>(true);
        if (cardUI == null)
        {
            Destroy(cardRoot);
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

    public void ShowGameResult(InGameResult reason)
    {
        GameObject gameResultPopup = _mainCanvas.transform.Find("UIPopup_GameResult")?.gameObject;
        if (gameResultPopup == null)
        {
            gameResultPopup = Instantiate(Resources.Load<GameObject>("Prefabs/UIPopup"), transform.parent);
            gameResultPopup.name = "UIPopup_GameResult";
        }

        UIPopup gameResult = gameResultPopup.GetComponent<UIPopup>();
        gameResultPopup.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0f);
        gameResult.OpenPopup($"{(reason == InGameResult.Victory ? "승리!":"패배!")}\n잠시 후 로비로 이동됩니다.", 
            0.5f);

    }
}
