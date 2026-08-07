using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopup_Deck : UIPopup
{
    [Header("[CardList Display]")]
    [SerializeField] private Transform _ownedCardContainer;
    [SerializeField] private Transform _deckCardContainer;

    [Header("[Card Prefab]")]
    [SerializeField] private GameObject _cardUIRootPrefab; // CardUI의 루트 프리팹

    [Header("[Filter Buttons]")]
    [SerializeField] private Button _btnShowAll;
    [SerializeField] private Button _btnShowAttackCard;
    [SerializeField] private Button _btnShowDefenseCard;

    [Header("[Pagination]")]
    [SerializeField] private Button _leftArrowButton;
    [SerializeField] private Button _rightArrowButton;
    private const int CARDS_PER_PAGE_OWNED = 8;
    private int _currentPageOwned = 0;

    [Header("[Deck Info]")]
    [SerializeField] private TextMeshProUGUI _txtNumCurrentCard;
    [SerializeField] private TextMeshProUGUI _txtNumTotalCard;

    private List<GameObject> _cardUIRootPool = new List<GameObject>();

    // 활성화된 CardUI의 루트 오브젝트를 CardData와 매핑 (빠른 참조 및 상태 관리용)
    private Dictionary<CardData, GameObject> _activeCardRootsMap = new Dictionary<CardData, GameObject>();

    private CardType _currentFilterType = CardType.UnDefined;

    private LobbyDeckViewModel _viewModel;
    public event Action<short> OnOwnedCardSelected;
    public event Action<short> OnDeckCardSelected;
    public event Action SaveDeck;
    

    protected override void Awake()
    {
        base.Awake();
        // InitializeObjectPool은 LobbyDeckState가 준비된 후 호출되도록 변경 고려
        // 또는 LobbyDeckState가 데이터 로드 완료 이벤트를 발생시켜 그때 초기화
        // 여기서는 OnEnable에서 InitialDisplay가 호출될 때 풀이 비어있으면 채우는 방식으로 변경

        _btnShowAll.onClick.AddListener(() => { _currentFilterType = CardType.UnDefined; _currentPageOwned = 0; RefreshAllCardDisplays(); });
        _btnShowAttackCard.onClick.AddListener(() => { _currentFilterType = CardType.Attack; _currentPageOwned = 0; RefreshAllCardDisplays(); });
        _btnShowDefenseCard.onClick.AddListener(() => { _currentFilterType = CardType.Defense; _currentPageOwned = 0; RefreshAllCardDisplays(); });
        _btnClose.onClick.AddListener(() => { SaveDeck?.Invoke(); });

        _leftArrowButton.onClick.AddListener(OnLeftArrowClick);
        _rightArrowButton.onClick.AddListener(OnRightArrowClick);
        OnPopupOpened += () => {

            if (_viewModel == null)
            {
                _txtNumCurrentCard.text = "불러오는 중...";
                return;
            }

            _currentPageOwned = 0;
            ObjectPoolInitialized(); // 풀 초기화 보장
            RefreshAllCardDisplays(); 

        }; // 팝업 열릴 때마다 전체 UI 갱신


    }

    private void OnDestroy()
    {
        foreach (var cardUIRoot in _cardUIRootPool)
        {
            if (cardUIRoot != null) Destroy(cardUIRoot);
        }
        _cardUIRootPool.Clear();
        _activeCardRootsMap.Clear();
    }

    public void Render(LobbyDeckViewModel viewModel)
    {
        _viewModel = viewModel;

        ObjectPoolInitialized();
        RefreshAllCardDisplays();
        UpdateDeckCountText(viewModel.OwnedCards,viewModel.DeckCardIds.Count);
    }

    private void ObjectPoolInitialized()
    {
        if (_viewModel == null)
        {
            Debug.LogWarning("[UIPopup_Deck] Deck data is not ready.");
            return;
        }

        // 풀 크기는 게임 내 모든 카드 종류의 수만큼 필요
        int requiredPoolSize = _viewModel.OwnedCards.Count;

        if (_cardUIRootPool.Count < requiredPoolSize)
        {
            Debug.Log($"[ObjectPoolInitialized] Current pool size: {_cardUIRootPool.Count}, Required: {requiredPoolSize}. Expanding pool.");
            for (int i = _cardUIRootPool.Count; i < requiredPoolSize; i++)
            {
                GameObject cardRootGO = Instantiate(_cardUIRootPrefab, this.transform);
                cardRootGO.name = $"Pooled_CardUI_{_cardUIRootPool.Count}";
                cardRootGO.SetActive(false);
                _cardUIRootPool.Add(cardRootGO);
            }
        }

        // _activeCardRootsMap 채우는 로직 (모든 카드 마스터에 대해 UI 루트 미리 생성 및 매핑)
        if (_activeCardRootsMap.Count != requiredPoolSize)
        {
            foreach (var go in _activeCardRootsMap.Values) go.SetActive(false); // 기존 맵 객체 비활성화
            _activeCardRootsMap.Clear();
            foreach (var pooledGO in _cardUIRootPool) pooledGO.SetActive(false);

            for (int i = 0; i < requiredPoolSize; i++)
            {
                CardData cardData = _viewModel.OwnedCards[i]; // 모든 카드 마스터 순회
                if (i < _cardUIRootPool.Count)
                {
                    GameObject cardRootGO = _cardUIRootPool[i];
                    CardUI cardUI = cardRootGO.GetComponentInChildren<CardUI>();
                    if (cardUI != null)
                    {
                        cardUI.InitializeDisplay(cardData);
                        cardUI.OnOwnedCardClicked = HandleOwnedCardClicked; // 왼쪽 목록에서 클릭
                        cardUI.OnDeckCardClicked = HandleDeckCardClicked;   // 오른쪽 목록에서 클릭
                        _activeCardRootsMap[cardData] = cardRootGO; // 모든 카드에 대해 맵핑
                    }
                    else Debug.LogError($"CardUI component not found on pooled object for {cardData.cardName}");
                }
                else
                {
                    Debug.LogError("Not enough objects in pool during EnsureObjectPoolInitialized. This shouldn't happen if pool size is correct.");
                }
            }
        }
    }


    // UI를 한 번에 모두 갱신하는 함수 (초기화, 필터/페이지 변경 시)
    private void RefreshAllCardDisplays()
    {
        // 모든 _activeCardRootsMap의 UI들을 일단 비활성화하고 풀 컨테이너로 (정리)
        foreach (var kvp in _activeCardRootsMap)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(false);
                kvp.Value.transform.SetParent(this.transform, false);
            }
        }

        // === 소유 카드 목록 그리기 시작 ===
        List<CardData> cardsForOwnedDisplay = GetFilteredOwnedCardsForDisplay();
        int startIndex = _currentPageOwned * CARDS_PER_PAGE_OWNED;
        int endIndex = Mathf.Min(startIndex + CARDS_PER_PAGE_OWNED, cardsForOwnedDisplay.Count);

        for (int i = 0; i < (endIndex - startIndex); i++) // 실제 표시할 개수만큼만 루프
        {
            CardData cardData = cardsForOwnedDisplay[startIndex + i];
            if (_activeCardRootsMap.TryGetValue(cardData, out GameObject cardRootGO) && cardRootGO != null)
            {
                cardRootGO.transform.SetParent(_ownedCardContainer, false);
                cardRootGO.transform.SetSiblingIndex(i); // 소유 카드 목록 내에서의 순서
                CardUI cardUI = cardRootGO.GetComponentInChildren<CardUI>();
                if (cardUI != null) cardUI.UpdateView(false);
                cardRootGO.SetActive(true);
            }
        }
        // === 소유 카드 목록 그리기 끝 ===

        // === 덱 카드 목록 그리기 시작 ===
        // `CurrentDeckCardIds`는 이미 ID 순으로 정렬되어 있다고 가정하거나, 여기서 `OrderBy` 사용
        foreach (short cardId in _viewModel.DeckCardIds.OrderBy(id => id))
        {
            CardData cardData = _viewModel.OwnedCards.FirstOrDefault(c => c.cardId == cardId);
            if (cardData != null && _activeCardRootsMap.TryGetValue(cardData, out GameObject cardRootGO) && cardRootGO != null)
            {
                cardRootGO.transform.SetParent(_deckCardContainer, false);
                // SetSiblingIndex는 SortDeckContainerChildren에서 처리하므로 여기서는 생략 가능
                CardUI cardUI = cardRootGO.GetComponentInChildren<CardUI>();
                if (cardUI != null) cardUI.UpdateView(true);
                cardRootGO.SetActive(true);
            }
        }
        SortDeckContainerChildren(); // 덱 목록 최종 정렬 (필수)

        UpdateDeckCountText(_viewModel.OwnedCards, _viewModel.DeckCardIds.Count);
        UpdateArrowButtons(cardsForOwnedDisplay.Count);
    }


    // 소유 카드 목록에서 카드 클릭 시
    private void HandleOwnedCardClicked(CardUI clickedCard)
    {
        OnOwnedCardSelected?.Invoke(clickedCard.CurrentCardData.cardId);
    }

    // 덱 목록에서 카드 클릭 시
    private void HandleDeckCardClicked(CardUI clickedCard)
    {
        OnDeckCardSelected?.Invoke(clickedCard.CurrentCardData.cardId);
    }

    private void SortDeckContainerChildren()
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in _deckCardContainer) // _deckCardContainer의 직접적인 자식들만 가져옴
        {
            children.Add(child);
        }

        // CardUI 컴포넌트 및 CardData를 기준으로 정렬
        children.Sort((t1, t2) => {
            CardUI cui1 = t1.GetComponentInChildren<CardUI>(true); // 루트의 자식에서 CardUI를 찾음
            CardUI cui2 = t2.GetComponentInChildren<CardUI>(true);

            if (cui1 != null && cui1.CurrentCardData != null && cui2 != null && cui2.CurrentCardData != null)
            {
                return cui1.CurrentCardData.cardId.CompareTo(cui2.CurrentCardData.cardId);
            }

            // CardUI나 CardData가 없는 경우 예외 처리 (예: 뒤로 보내거나, 로그 출력)
            if (cui1 == null || cui1.CurrentCardData == null) return 1; // t1을 뒤로
            if (cui2 == null || cui2.CurrentCardData == null) return -1; // t2를 뒤로
            return 0;
        });

        // 정렬된 순서대로 SiblingIndex 재설정
        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
        }
    }

    // 소유 카드 목록 표시 갱신 (필터, 페이지 변경 시)
    private void RefreshOwnedCardsDisplay()
    {
        // 현재 화면에 보이는 소유 카드 UI만 비활성화 (덱에 있는 카드는 건드리지 않음)
        foreach (CardData cardData in _activeCardRootsMap.Keys.ToList()) // ToList()로 복사본 순회
        {
            if (!_viewModel.DeckCardIds.Contains(cardData.cardId))
            {
                _activeCardRootsMap[cardData].SetActive(false);
                _activeCardRootsMap[cardData].transform.SetParent(this.transform, false); // 풀 컨테이너로
            }
        }

        List<CardData> filteredOwnedCardsToDisplay = GetFilteredOwnedCardsForDisplay();
        int startIndex = _currentPageOwned * CARDS_PER_PAGE_OWNED;
        int endIndex = Mathf.Min(startIndex + CARDS_PER_PAGE_OWNED, filteredOwnedCardsToDisplay.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            CardData cardData = filteredOwnedCardsToDisplay[i];
            if (_activeCardRootsMap.TryGetValue(cardData, out GameObject cardRootGO))
            {
                cardRootGO.transform.SetParent(_ownedCardContainer, false);
                
                CardUI cardUI = cardRootGO.GetComponentInChildren<CardUI>();
                cardUI.UpdateView(false); // 큰 카드 모습
                cardRootGO.SetActive(true);
            }
        }
        UpdateArrowButtons(filteredOwnedCardsToDisplay.Count);
    }

    // GetFilteredOwnedCardsForDisplay: 현재 덱에 없는 소유 카드 중 현재 필터에 맞는 카드만 반환
    private List<CardData> GetFilteredOwnedCardsForDisplay()
    {
        List<CardData> availableForDisplay = _viewModel.OwnedCards
                                            .Where(c => !_viewModel.DeckCardIds.Contains(c.cardId))
                                            .ToList();
        return _currentFilterType switch
        {
            CardType.Attack => availableForDisplay.Where(card => card.cardType == CardType.Attack).ToList(),
            CardType.Defense => availableForDisplay.Where(card => card.cardType == CardType.Defense).ToList(),
            _ => availableForDisplay
        };
    }

    public void UpdateDeckCountText(IReadOnlyList<CardData> ownPlayerData, int numCardInDeck)
    {
        _txtNumTotalCard.text = $"/ {ownPlayerData.Count}";
        _txtNumCurrentCard.text = numCardInDeck.ToString();
    }

    private void UpdateArrowButtons(int totalFilteredOwnedCards)
    {
        _leftArrowButton.gameObject.SetActive(_currentPageOwned > 0);
        _rightArrowButton.gameObject.SetActive((_currentPageOwned + 1) * CARDS_PER_PAGE_OWNED < totalFilteredOwnedCards);
    }

    void OnLeftArrowClick()
    {
        if (_currentPageOwned > 0)
        {
            _currentPageOwned--;
            RefreshOwnedCardsDisplay(); // 소유 카드 목록만 페이지 갱신
        }
    }

    void OnRightArrowClick()
    {
        List<CardData> filteredOwnedCardsToDisplay = GetFilteredOwnedCardsForDisplay();
        if ((_currentPageOwned + 1) * CARDS_PER_PAGE_OWNED < filteredOwnedCardsToDisplay.Count)
        {
            _currentPageOwned++;
            RefreshOwnedCardsDisplay(); // 소유 카드 목록만 페이지 갱신
        }
    }

    public void ShowDeckValidationWarning()
    {
        GameObject warningPopup = transform.parent.gameObject.transform.Find("UIPopup_Warning")?.gameObject;
        if (warningPopup == null)
        {
            warningPopup = Instantiate(Resources.Load<GameObject>("Prefabs/UIPopup"), transform.parent);
            warningPopup.name = "UIPopup_Warning";
        }

        UIPopup warning = warningPopup.GetComponent<UIPopup>();
        warningPopup.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0f);
        warning.OpenPopup(
            $"덱에 카드가 부족합니다.\n{_viewModel.MaxDeckSize}장의 카드를 구성해야 합니다.",
            0.5f);
    }
}
