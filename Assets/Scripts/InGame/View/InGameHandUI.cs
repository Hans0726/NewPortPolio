using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class InGameHandUI : MonoBehaviour
{
    public event Action<CardData, GameObject> CardUseRequested;

    private enum HandState
    {
        Idle,
        InitialDrawing,
        InInteraction
    }

    private const int InitialPoolSize = 15;

    private Transform _handContainer;
    private GameObject _cardUIPrefab;
    private RectTransform _deckPosition;
    private Canvas _mainCanvas;
    private UIPopup_CardSelect _cardSelectPopup;
    private GameObject _blockingPanel;
    private Transform _cardPoolContainer;

    private float _spreadAngle;
    private float _cardSpacing;
    private float _baseYPosition;
    private float _collapsedYPosition;
    private float _hoverScaleMultiplier;
    private float _hoverYOffset;
    private float _arcCorrectionFactor;
    private Vector2 _expandedCardScale;
    private Vector2 _collapsedCardScale;
    private float _lerpSpeed;

    private readonly List<GameObject> _cardUIPool = new List<GameObject>();
    private readonly List<GameObject> _activeHandCardRoots = new List<GameObject>();

    private HandState _currentHandState = HandState.Idle;
    private CardUI _hoveredCard;
    private CardUI _draggedCard;
    private GameObject _pendingSelectedCardRoot;
    private int _pendingCardOriginalIndex = -1;
    private int _draggedCardOriginalIndex = -1;
    private bool _isHandExpanded;
    private bool _isHandInteractionLocked = true;
    private bool _isConfigured;
    private bool _isInitialized;
    private InGameCardState _cardState;
    private Func<int> _currentCostProvider;

    public bool IsDragging => _draggedCard != null;

    private void OnDestroy()
    {
    }

    public void Configure(
        Transform handContainer,
        GameObject cardUIPrefab,
        RectTransform deckPosition,
        Canvas mainCanvas,
        UIPopup_CardSelect cardSelectPopup,
        GameObject blockingPanel,
        Transform cardPoolContainer,
        float spreadAngle,
        float cardSpacing,
        float baseYPosition,
        float collapsedYPosition,
        float hoverScaleMultiplier,
        float hoverYOffset,
        float arcCorrectionFactor,
        Vector2 expandedCardScale,
        Vector2 collapsedCardScale,
        float lerpSpeed)
    {
        _handContainer = handContainer;
        _cardUIPrefab = cardUIPrefab;
        _deckPosition = deckPosition;
        _mainCanvas = mainCanvas;
        _cardSelectPopup = cardSelectPopup;
        _blockingPanel = blockingPanel;
        _cardPoolContainer = cardPoolContainer;
        _spreadAngle = spreadAngle;
        _cardSpacing = cardSpacing;
        _baseYPosition = baseYPosition;
        _collapsedYPosition = collapsedYPosition;
        _hoverScaleMultiplier = hoverScaleMultiplier;
        _hoverYOffset = hoverYOffset;
        _arcCorrectionFactor = arcCorrectionFactor;
        _expandedCardScale = expandedCardScale;
        _collapsedCardScale = collapsedCardScale;
        _lerpSpeed = lerpSpeed;
        _isConfigured = true;
    }

    public void Initialize(InGameCardState cardState, Func<int> currentCostProvider)
    {
        if (_isInitialized) return;
        if (!_isConfigured)
        {
            Debug.LogError("[InGameHandUI] Configure must be called before Initialize.");
            return;
        }

        InitializeObjectPool();

        if (cardState == null)
        {
            Debug.LogError("[InGameHandUI] InGameCardState is missing.");
            return;
        }

        _cardState = cardState;
        _currentCostProvider = currentCostProvider;
        _isInitialized = true;
    }

    private void LateUpdate()
    {
        if (_currentHandState != HandState.InInteraction || _isHandInteractionLocked) return;

        bool blockingPanelActive = _blockingPanel != null && _blockingPanel.activeSelf;
        _isHandExpanded = (_hoveredCard != null || _draggedCard != null) && !blockingPanelActive;
        AnimateHandToTargetState();
    }

    private void InitializeObjectPool()
    {
        if (_cardUIPrefab == null || _cardPoolContainer == null)
        {
            Debug.LogError("[InGameHandUI] Card prefab or pool container is missing.");
            return;
        }

        for (int i = _cardUIPool.Count; i < InitialPoolSize; i++)
        {
            GameObject cardRoot = Instantiate(_cardUIPrefab, _cardPoolContainer);
            cardRoot.name = $"Pooled_CardUI_{i}";
            cardRoot.SetActive(false);
            _cardUIPool.Add(cardRoot);
        }
    }

    private GameObject GetCardUIRootFromPool()
    {
        GameObject cardRoot = _cardUIPool.FirstOrDefault(candidate => !candidate.activeSelf);
        if (cardRoot != null) return cardRoot;

        cardRoot = Instantiate(_cardUIPrefab, _cardPoolContainer);
        cardRoot.name = $"Pooled_CardUI_{_cardUIPool.Count}";
        cardRoot.SetActive(false);
        _cardUIPool.Add(cardRoot);
        return cardRoot;
    }

    private void ReturnCardUIRootToPool(GameObject cardRoot)
    {
        if (cardRoot == null) return;

        cardRoot.transform.DOKill();
        cardRoot.SetActive(false);
        cardRoot.transform.SetParent(_cardPoolContainer, false);
    }

    private CardUI AddCardToHandView(CardData card)
    {
        GameObject cardRoot = GetCardUIRootFromPool();
        CardUI cardUI = cardRoot.GetComponentInChildren<CardUI>(true);
        if (cardUI == null)
        {
            ReturnCardUIRootToPool(cardRoot);
            Debug.LogError("[InGameHandUI] CardUI component was not found in the card prefab.");
            return null;
        }

        cardUI.InitializeDisplay(card);
        cardUI.BindHandView(this);
        _activeHandCardRoots.Add(cardRoot);
        return cardUI;
    }

    public void ShowInitialHand()
    {
        _currentHandState = HandState.InitialDrawing;

        foreach (GameObject cardRoot in _activeHandCardRoots.ToList())
        {
            ReturnCardUIRootToPool(cardRoot);
        }

        _activeHandCardRoots.Clear();

        DG.Tweening.Sequence sequence = DOTween.Sequence();
        IReadOnlyList<CardData> hand = _cardState.PlayerHand;
        for (int i = 0; i < hand.Count; i++)
        {
            CardUI cardUI = AddCardToHandView(hand[i]);
            if (cardUI == null) continue;

            GameObject cardRoot = cardUI.RootGameObject;
            Transform cardTransform = cardRoot.transform;
            cardTransform.SetParent(_handContainer, false);
            cardTransform.position = _deckPosition.position;
            cardTransform.localScale = Vector3.zero;
            cardRoot.SetActive(true);
            cardUI.SetPlayableState(_currentCostProvider != null ? _currentCostProvider() : 0);

            sequence.Insert(i * 0.1f, cardTransform.DOMove(_handContainer.position, 0.5f).SetEase(Ease.OutQuad));
            sequence.Insert(i * 0.1f, cardTransform.DOScale(_collapsedCardScale, 0.5f));
        }

        sequence.OnComplete(() =>
        {
            _currentHandState = HandState.InInteraction;
            AnimateHandToTargetState();
        });
    }

    public void HideForCombat()
    {
        _currentHandState = HandState.Idle;
        SetInteractionLocked(true);
        _isHandExpanded = false;
        _hoveredCard = null;
        _draggedCardOriginalIndex = -1;
        _pendingCardOriginalIndex = -1;

        if (_draggedCard != null)
        {
            ReturnHandCardImmediately(_draggedCard.RootGameObject);
            _draggedCard = null;
        }

        if (_pendingSelectedCardRoot != null)
        {
            ReturnHandCardImmediately(_pendingSelectedCardRoot);
            _pendingSelectedCardRoot = null;
        }

        foreach (GameObject cardRoot in _activeHandCardRoots.ToList())
        {
            ReturnHandCardImmediately(cardRoot);
        }

        _activeHandCardRoots.Clear();
    }

    public void ShowForNewRound()
    {
        _isHandInteractionLocked = false;
        _isHandExpanded = false;
        _hoveredCard = null;
        _draggedCard = null;
        _pendingSelectedCardRoot = null;
        _draggedCardOriginalIndex = -1;
        _pendingCardOriginalIndex = -1;
        ShowInitialHand();
    }

    private void ReturnHandCardImmediately(GameObject cardRoot)
    {
        if (cardRoot == null) return;

        CardUI cardUI = cardRoot.GetComponentInChildren<CardUI>(true);
        if (cardUI != null && cardUI.CanvasGroup != null)
        {
            cardUI.CanvasGroup.interactable = false;
            cardUI.CanvasGroup.blocksRaycasts = false;
        }

        ReturnCardUIRootToPool(cardRoot);
    }

    public void SetHoveredCard(CardUI cardUI)
    {
        _hoveredCard = cardUI;
    }

    public void ClearHoveredCard(CardUI cardUI)
    {
        if (_hoveredCard == cardUI)
        {
            _hoveredCard = null;
        }
    }

    private void AnimateHandToTargetState()
    {
        int cardCount = _activeHandCardRoots.Count;
        for (int i = 0; i < cardCount; i++)
        {
            _activeHandCardRoots[i].transform.SetSiblingIndex(i);
        }

        if (_isHandExpanded && _hoveredCard != null)
        {
            _hoveredCard.RootGameObject.transform.SetAsLastSibling();
        }

        float startAngle = -(cardCount - 1) / 2f * _spreadAngle;
        float startX = -(cardCount - 1) / 2f * _cardSpacing;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardRoot = _activeHandCardRoots[i];
            Transform cardTransform = cardRoot.transform;
            CardUI cardUI = cardRoot.GetComponentInChildren<CardUI>();
            if (cardUI == null) continue;

            float targetAngle = startAngle + i * _spreadAngle;
            float targetX = startX + i * _cardSpacing;
            float rotationRadius = cardUI.RectTransform.rect.height * 0.5f;
            float radians = Mathf.Abs(targetAngle) * Mathf.Deg2Rad;
            float rotationRise = (1f - Mathf.Cos(radians)) * rotationRadius;

            float targetY = _isHandExpanded
                ? _baseYPosition - rotationRise * _arcCorrectionFactor
                : _collapsedYPosition - rotationRise * _arcCorrectionFactor;

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, -targetAngle);
            Vector2 targetScale = _isHandExpanded ? _expandedCardScale : _collapsedCardScale;

            if (_isHandExpanded && cardUI == _hoveredCard)
            {
                targetY = _baseYPosition + _hoverYOffset;
                targetScale *= _hoverScaleMultiplier;
                targetRotation = Quaternion.identity;
            }

            Vector3 targetPosition = new Vector3(targetX, targetY, 0f);
            cardTransform.localPosition = Vector3.Lerp(
                cardTransform.localPosition,
                targetPosition,
                Time.deltaTime * _lerpSpeed);
            cardTransform.localRotation = Quaternion.Slerp(
                cardTransform.localRotation,
                targetRotation,
                Time.deltaTime * _lerpSpeed);
            cardTransform.localScale = Vector3.Lerp(
                cardTransform.localScale,
                targetScale,
                Time.deltaTime * _lerpSpeed);
        }
    }

    public void OnCardBeginDrag(CardUI cardUI)
    {
        if (_isHandInteractionLocked || cardUI == null) return;

        _draggedCardOriginalIndex = _activeHandCardRoots.IndexOf(cardUI.RootGameObject);
        _activeHandCardRoots.Remove(cardUI.RootGameObject);
        _draggedCard = cardUI;
        cardUI.RootGameObject.transform.rotation = Quaternion.identity;
        cardUI.RootGameObject.transform.SetParent(_mainCanvas.transform, true);
        cardUI.CanvasGroup.blocksRaycasts = false;
    }

    public void OnCardDrag(PointerEventData eventData)
    {
        if (_isHandInteractionLocked || _draggedCard == null) return;

        RectTransform canvasRect = _mainCanvas.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            _mainCanvas.worldCamera,
            out Vector2 localPoint))
        {
            _draggedCard.RootGameObject.transform.localPosition = localPoint;
        }
    }

    public void OnCardEndDrag(CardUI cardUI, PointerEventData eventData)
    {
        if (_draggedCard == null) return;

        if (_isHandInteractionLocked)
        {
            _draggedCard.CanvasGroup.blocksRaycasts = true;
            return;
        }

        _draggedCard.CanvasGroup.blocksRaycasts = true;
        bool droppedOnCanvas = _draggedCard.RootGameObject.transform.parent == _mainCanvas.transform;

        if (droppedOnCanvas)
        {
            _pendingSelectedCardRoot = _draggedCard.RootGameObject;
            _pendingCardOriginalIndex = _draggedCardOriginalIndex;
            _cardSelectPopup.SetOriginalCardInfo(
                _handContainer,
                _draggedCardOriginalIndex,
                _draggedCard);
            _cardSelectPopup.OpenPopup(cardUI, HandleCardChoice);
        }

        _draggedCard = null;
        _hoveredCard = null;
        _draggedCardOriginalIndex = -1;
    }

    private void HandleCardChoice(CardData card)
    {
        GameObject cardRoot = _pendingSelectedCardRoot;
        _pendingSelectedCardRoot = null;
        CardUseRequested?.Invoke(card, cardRoot);
    }

    public void CommitCardUse(GameObject cardRoot)
    {
        _pendingCardOriginalIndex = -1;
        RemoveCardFromHandView(cardRoot);
    }

    public void RejectCardUse(GameObject cardRoot)
    {
        if (cardRoot == null) return;

        cardRoot.transform.SetParent(_handContainer, false);
        cardRoot.SetActive(true);
        RestoreCardToHand(cardRoot, _pendingCardOriginalIndex);
        _pendingCardOriginalIndex = -1;
        UpdateCardInteractableStates(_currentCostProvider != null ? _currentCostProvider() : 0);
    }

    private void RemoveCardFromHandView(GameObject cardRoot)
    {
        if (cardRoot == null) return;

        _activeHandCardRoots.Remove(cardRoot);
        cardRoot.transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() => ReturnCardUIRootToPool(cardRoot));
    }

    public void RestoreCardToHand(GameObject cardRoot, int originalIndex)
    {
        if (cardRoot == null || _activeHandCardRoots.Contains(cardRoot)) return;

        int clampedIndex = Mathf.Clamp(originalIndex, 0, _activeHandCardRoots.Count);
        _activeHandCardRoots.Insert(clampedIndex, cardRoot);
    }

    public void SetInteractionLocked(bool isLocked)
    {
        _isHandInteractionLocked = isLocked;

        if (isLocked)
        {
            _hoveredCard = null;
            _isHandExpanded = false;
        }

        foreach (GameObject cardRoot in _activeHandCardRoots)
        {
            CardUI cardUI = cardRoot.GetComponentInChildren<CardUI>();
            if (cardUI == null || cardUI.CanvasGroup == null) continue;

            cardUI.CanvasGroup.interactable = !isLocked;
            cardUI.CanvasGroup.blocksRaycasts = !isLocked;
        }

        if (!isLocked && _currentCostProvider != null)
        {
            UpdateCardInteractableStates(_currentCostProvider());
        }
    }

    public void UpdateCardInteractableStates(int currentCost)
    {
        foreach (GameObject cardRoot in _activeHandCardRoots)
        {
            CardUI cardUI = cardRoot.GetComponentInChildren<CardUI>();
            if (cardUI?.CurrentCardData == null) continue;

            cardUI.SetPlayableState(currentCost >= cardUI.CurrentCardData.cost);
        }
    }

    public float GetPlayableEffectTime()
    {
        foreach (GameObject cardRoot in _activeHandCardRoots)
        {
            CardUI cardUI = cardRoot.GetComponentInChildren<CardUI>();
            if (cardUI?.CurrentCardData != null
                && cardUI.IsPlayableEffectActive
                && cardUI.PlayableEffectPS != null)
            {
                return cardUI.PlayableEffectPS.totalTime;
            }
        }

        return 0f;
    }
}
