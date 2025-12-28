using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // 여전히 개별 소멸 애니메이션 등에 사용 가능
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    // --- 상태 구분을 위한 enum ---
    public enum HandState
    {
        Idle,           // 아무것도 안 하는 상태
        InitialDrawing, // 초기 드로우 애니메이션 중
        InInteraction   // 일반적인 상호작용 상태 (LateUpdate 제어)
    }
    private HandState _currentHandState = HandState.Idle;

    [Header("Opening Sequence UI")]
    [SerializeField] private CanvasGroup _openingSequenceCanvasGroup;
    [SerializeField] private TextMeshProUGUI _openingSequenceText;
    [SerializeField] private float _openingFadeDuration = 1.0f;
    [SerializeField] private float _openingDisplayDuration = 2.0f;

    [Space(20)]
    [Header("References")]
    [SerializeField] private Transform _handContainer;
    [SerializeField] private GameObject _cardUIPrefab;
    [SerializeField] private RectTransform _deckPosition;
    [SerializeField] private RectTransform _dropZone; // 드래그 드롭 판단 영역
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private Canvas _mainCanvas;

    [Header("Layout Settings")]
    [SerializeField] private float _spreadAngle = 10f;
    [SerializeField] private float _cardSpacing = 100f;
    [SerializeField] private float _baseYPosition = 150f;
    [SerializeField] private float _collapsedYPosition = -50f;
    [SerializeField] private float _hoverScaleMultiplier = 1.2f;
    [SerializeField] private float _hoverYOffset = 50f;
    [SerializeField] private float _arcCorrectionFactor = 5f; // 아치 모양 보정 계수

    [Header("Animation Settings")]
    [SerializeField] private Vector2 _expandedCardScale = Vector2.one;
    [SerializeField] private Vector2 _collapsedCardScale = new Vector2(0.8f, 0.8f);
    [SerializeField] private float _lerpSpeed = 10f;

    // --- 상태 변수 ---
    private CardUI _hoveredCard = null;
    private CardUI _draggedCard = null;
    private bool _isHandExpanded = false;

    // 오브젝트 풀링
    [SerializeField] private Transform _cardPoolContainer;
    private List<GameObject> _cardUIPool = new List<GameObject>();
    private List<GameObject> _activeHandCardRoots = new List<GameObject>();
    private const int INITIAL_POOL_SIZE = 15; // 최대 핸드 수 + 여유분


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (GameTurnManager.Instance != null)
        {
            GameTurnManager.Instance.OnCostChanged += UpdateCardInteractableStates;
        }
    }

    void OnDestroy()
    {
        if (GameTurnManager.Instance != null)
        {
            GameTurnManager.Instance.OnCostChanged -= UpdateCardInteractableStates;
        }
    }

    void Start()
    {
        // GameManager가 없다는 것은 이 씬에서 바로 시작했다는 의미
        if (GameManager.Instance == null)
        {
            // --- 테스트 모드 초기화 흐름 ---
            Debug.LogWarning("--- RUNNING IN TEST MODE ---");

            // 1. 데이터 매니저(CardManager) 테스트 초기화
            if (InGameCardManager.Instance != null)
            {
                InGameCardManager.Instance.TestInitialize(); // 테스트 덱 생성 및 셔플
            }
            else { Debug.LogError("TEST MODE: InGameCardManager instance not found!"); }

            // 2. UI 매니저 초기화 (이벤트 구독)
            Initialize(); // 자신의 초기화 함수 호출

            // 3. 오프닝 시퀀스 시작
            // 오프닝 시퀀스가 끝나면 DrawInitialHand가 호출되어야 함
            ShowOpeningSequence();
        }
        // else: 실제 모드에서는 GameManager가 LoadInGameSceneAndInitialize에서
        // 각 매니저의 Initialize 함수를 순서대로 호출해 줄 것이므로, 여기서는 아무것도 안 함.
    }

    public void Initialize()
    {
        Debug.Log("[InGameUIManager] Initializing...");
        InitializeObjectPool();

        if (InGameCardManager.Instance != null)
        {
            InGameCardManager.Instance.OnInitialHandDrawn += HandleInitialHandDrawn;
        }
        else
        {
            Debug.LogError("[InGameUIManager] InGameCardManager.Instance is null during Initialize!");
        }
    }

    #region UI Initialization
    public void ShowOpeningSequence()
    {
        StartCoroutine(OpeningSequenceCoroutine());
    }

    private IEnumerator OpeningSequenceCoroutine()
    {
        if (_openingSequenceCanvasGroup == null || _openingSequenceText == null)
        {
            Debug.LogError("Opening sequence UI elements are not assigned in InGameUIManager!");
            yield break;
        }

        _openingSequenceCanvasGroup.alpha = 0f;
        _openingSequenceCanvasGroup.gameObject.SetActive(true);
        _openingSequenceText.text = "제한 시간 내에 전투를 준비하세요!";

        Debug.Log("InGameUIManager: Opening Sequence Fading In");
        _openingSequenceCanvasGroup.DOFade(1f, _openingFadeDuration);
        yield return new WaitForSeconds(_openingFadeDuration);

        Debug.Log("InGameUIManager: Opening Sequence Displaying");
        yield return new WaitForSeconds(_openingDisplayDuration);

        Debug.Log("InGameUIManager: Opening Sequence Fading Out");
        _openingSequenceCanvasGroup.DOFade(0f, _openingFadeDuration).OnComplete(() =>
        {
            _openingSequenceCanvasGroup.gameObject.SetActive(false);
            Debug.Log("InGameUIManager: Opening Sequence Finished");
            if (InGameCardManager.Instance != null)
            {
                InGameCardManager.Instance.DrawInitialHand();
            }
        });
    }

    // ★★★ 초기 드로우 처리 함수 (DOTween Sequence만 담당) ★★★
    private void HandleInitialHandDrawn()
    {
        _currentHandState = HandState.InitialDrawing; // 상태 변경

        List<CardData> initialHandData = InGameCardManager.Instance.PlayerHand;

        // 이전에 활성화된 카드가 있다면 정리
        foreach (var go in _activeHandCardRoots) ReturnCardUIRootToPool(go);
        _activeHandCardRoots.Clear();

        DG.Tweening.Sequence seq = DOTween.Sequence();
        for (int i = 0; i < initialHandData.Count; i++)
        {
            CardUI newCardUI = AddCardToHandView(initialHandData[i]);
            GameObject newCardGO = newCardUI.RootGameObject;
            Transform cardTransform = newCardGO.transform;

            cardTransform.SetParent(_handContainer, false);
            cardTransform.position = _deckPosition.position;
            cardTransform.localScale = Vector3.zero;
            newCardGO.SetActive(true);
            newCardUI.SetPlayableState(GameTurnManager.Instance.CurrentCost);

            // 중앙에 모이는 애니메이션
            seq.Insert(i * 0.1f, cardTransform.DOMove(_handContainer.position, 0.5f).SetEase(Ease.OutQuad));
            seq.Insert(i * 0.1f, cardTransform.DOScale(_collapsedCardScale, 0.5f));
        }

        seq.OnComplete(() =>
        {
            Debug.Log("Initial draw animation (to center) finished.");
            _currentHandState = HandState.InInteraction; // 이제 LateUpdate 제어 시작
            
        });
    }
    #endregion

    #region Card Pooling
    private void InitializeObjectPool()
    {
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            GameObject cardRootGO = Instantiate(_cardUIPrefab, _cardPoolContainer);
            cardRootGO.name = $"Pooled_CardUI_{i}";
            cardRootGO.SetActive(false);
            _cardUIPool.Add(cardRootGO);
        }
    }

    private GameObject GetCardUIRootFromPool()
    {
        GameObject cardRootInstance = _cardUIPool.FirstOrDefault(go => !go.activeSelf);
        if (cardRootInstance == null)
        {
            cardRootInstance = Instantiate(_cardUIPrefab, _cardPoolContainer);
            _cardUIPool.Add(cardRootInstance);
            Debug.LogWarning("CardUI Pool extended.");
        }

        return cardRootInstance;
    }

    private void ReturnCardUIRootToPool(GameObject cardRoot)
    {
        if (cardRoot != null)
        {
            cardRoot.SetActive(false);
            cardRoot.transform.SetParent(_cardPoolContainer, false);
        }
    }
    #endregion

    private CardUI AddCardToHandView(CardData drawnCard)
    {
        GameObject cardRootGO = GetCardUIRootFromPool();

        CardUI cardUI = cardRootGO.GetComponentInChildren<CardUI>();
        if (cardUI != null)
        {
            cardUI.InitializeDisplay(drawnCard); // uiManager 참조 전달 제거
            _activeHandCardRoots.Add(cardRootGO);
        }
        else
        {
            ReturnCardUIRootToPool(cardRootGO);
            return null;
        }

        return cardUI;
    }

    // 카드 제거 처리
    private void RemoveCardFromHandView(GameObject playedCard)
    {
        _activeHandCardRoots.Remove(playedCard);

        // 사라지는 애니메이션 후 풀로 반환
        playedCard.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
            .OnComplete(() => ReturnCardUIRootToPool(playedCard));
    }

    void LateUpdate()
    {
        if (_currentHandState != HandState.InInteraction) return;

        // --- 1. 마우스 아래에 있는 '핸드 카드' 찾기 ---
        _hoveredCard = null; // 매 프레임 초기화
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            CardUI cardUI = result.gameObject.GetComponentInParent<CardUI>();
            // 레이캐스트된 UI가 _activeHandCardRoots에 포함된 것인지 확인 (핸드 카드인지 판별)
            if (cardUI != null && _activeHandCardRoots.Contains(cardUI.RootGameObject))
            {
                _hoveredCard = cardUI;
                break; // 가장 위에 있는 카드 하나만 인정
            }
        }

        // --- 2. 핸드의 목표 상태(확대/축소) 결정 ---
        // ★★★ 핵심 변경: 호버된 카드가 '하나라도 있으면' 핸드는 확장된 상태가 목표 ★★★
        bool targetExpandedState = (_hoveredCard != null || _draggedCard != null);

        // ★★★ 부드러운 전환을 위해 Lerp 사용 (선택 사항이지만 추천) ★★★
        // 현재 상태(_isHandExpanded)를 목표 상태(targetExpandedState)로 점진적으로 변경할 수 있음
        // 하지만 지금은 즉시 변경하는 것이 더 간단
        _isHandExpanded = targetExpandedState;


        // --- 3. UI 업데이트 ---
        AnimateHandToTargetState();
    }


    private void AnimateHandToTargetState()
    {
        int cardCount = _activeHandCardRoots.Count;

        // --- 1. 렌더링 순서(Sibling Index) 설정 ---
        // 드로우 순서(_activeHandCardRoots의 순서)를 기본 렌더링 순서로 사용
        for (int i = 0; i < cardCount; i++)
        {
            _activeHandCardRoots[i].transform.SetSiblingIndex(i);
        }
        // 만약 호버된 카드가 있다면, 그 카드만 맨 위로 올림
        if (_isHandExpanded && _hoveredCard)
        {
            GameObject card = _hoveredCard.RootGameObject;
            card.transform.SetAsLastSibling();
        }

        // --- 2. 레이아웃 계산 및 애니메이션 ---
        float startAngle = -(cardCount - 1) / 2.0f * _spreadAngle;
        float startX = -(cardCount - 1) / 2.0f * _cardSpacing;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardRootGO = _activeHandCardRoots[i]; // 드로우 순서대로 가져옴
            Transform cardTransform = cardRootGO.transform;
            CardUI cardUI = cardRootGO.GetComponentInChildren<CardUI>();

            // 목표 위치/회전/크기 계산
            float targetAngle = startAngle + i * _spreadAngle;
            float targetX = startX + i * _cardSpacing;

            float cardHeight = cardUI.RectTransform.rect.height;
            float rotationRadius = cardHeight * 0.5f; // 회전 반지름
            float radianAngle = Mathf.Abs(targetAngle) * Mathf.Deg2Rad;
            float yRiseDueToRotation = (1 - Mathf.Cos(radianAngle)) * rotationRadius;

            float targetY;

            if (_isHandExpanded)
            {
                targetY = _baseYPosition - (yRiseDueToRotation * _arcCorrectionFactor);
                if (cardUI == _hoveredCard)
                {
                    targetY += _hoverYOffset;
                }
            }
            else // 축소된 상태일 때
            {
                // 축소 시에도 부채꼴 모양을 유지하되, 전체적인 기준 높이만 낮춤
                targetY = _collapsedYPosition - (yRiseDueToRotation * _arcCorrectionFactor);
            }


            // 회전 및 크기 설정 (이전과 동일)
            Quaternion targetRotation = Quaternion.Euler(0, 0, -targetAngle);
            Vector2 targetScale = _isHandExpanded ? _expandedCardScale : _collapsedCardScale;
            if (_isHandExpanded && cardUI == _hoveredCard)
            {
                targetScale *= _hoverScaleMultiplier;
                targetRotation = Quaternion.identity;
            }

            Vector3 targetPosition = new Vector3(targetX, targetY, 0);

            // Lerp로 부드럽게 이동
            cardTransform.localPosition = Vector3.Lerp(cardTransform.localPosition, targetPosition, Time.deltaTime * _lerpSpeed);
            cardTransform.localRotation = Quaternion.Slerp(cardTransform.localRotation, targetRotation, Time.deltaTime * _lerpSpeed);
            cardTransform.localScale = Vector3.Lerp(cardTransform.localScale, targetScale, Time.deltaTime * _lerpSpeed);
        }
    }

    public void OnCardBeginDrag(CardUI cardUI)
    {
        _activeHandCardRoots.Remove(cardUI.RootGameObject); // 핸드에서 잠시 제거
        _draggedCard = cardUI;
        _draggedCard.RootGameObject.transform.rotation = Quaternion.identity; // 회전 초기화
        _draggedCard.RootGameObject.transform.SetParent(_mainCanvas.transform, true); // 캔버스로 이동

        _draggedCard.CanvasGroup.blocksRaycasts = false;
    }

    public void OnCardDrag(PointerEventData eventData)
    {
        RectTransform parentRect = _mainCanvas.transform as RectTransform;
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                _mainCanvas.worldCamera, // Screen Space-Camera 모드일 때
                out localPoint))
        {
            _draggedCard.RootGameObject.transform.localPosition = localPoint;
        }
    }

    public void OnCardEndDrag(CardUI cardUI, PointerEventData eventData)
    {
        if (_draggedCard == null) return;
        _draggedCard.CanvasGroup.blocksRaycasts = true;

        // 드롭 영역 확인
        if (RectTransformUtility.RectangleContainsScreenPoint(_dropZone, eventData.position))
        {
            Debug.Log($"Card {_draggedCard.CurrentCardData.cardName} Played!");
            // 실제 카드 사용 로직
            InGameCardManager.Instance.PlayCardFromHand(_draggedCard.CurrentCardData);
            RemoveCardFromHandView(_draggedCard.RootGameObject);
        }
        else
        {
            // 핸드로 복귀
            _activeHandCardRoots.Add(cardUI.RootGameObject);
            _draggedCard.transform.SetParent(_handContainer, true);
            _draggedCard.SetPlayableState(GameTurnManager.Instance.CurrentCost);
        }
        _draggedCard = null;
    }

    private void UpdateCardInteractableStates(int currentCost)
    {
        foreach (var cardRootGO in _activeHandCardRoots)
        {
            CardUI cardUI = cardRootGO.GetComponent<CardUI>();
            if (cardUI != null && cardUI.CurrentCardData != null)
            {
                bool canAfford = currentCost >= cardUI.CurrentCardData.cost;
                cardUI.SetPlayableState(canAfford);
            }
        }
    }
}