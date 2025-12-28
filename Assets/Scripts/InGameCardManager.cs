using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public class InGameCardManager : MonoBehaviour
{
    public static InGameCardManager Instance { get; private set; }

    [Header("Card Database")]
    [SerializeField] private CardDatabase _cardDatabase; // 인스펙터에서 할당

    private List<CardData> _playerDeck = new List<CardData>(); // 실제 게임에서 사용할 덱
    public List<CardData> PlayerDeck => _playerDeck;

    private List<CardData> _playerHand = new List<CardData>(); // 현재 플레이어의 핸드
    public List<CardData> PlayerHand => _playerHand;

    private List<CardData> _playerDiscardPile = new List<CardData>(); // 버려진 카드 더미 (선택 사항)
    public List<CardData> PlayerDiscardPile => _playerDiscardPile;

    public int InitialHandSize = 3; // 초기 핸드 크기

    // 이벤트 (UI 등 다른 곳에서 핸드 변경을 감지할 수 있도록)
    public event Action<CardData> OnCardDrawn;  // 카드를 뽑았을 때
    public event Action<CardData> OnCardPlayed; // 카드를 사용했을 때
    public event Action OnInitialHandDrawn;     // 초기 핸드 드로우가 '완료'되었을 때

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _cardDatabase.Initialize(); // 데이터베이스 초기화   
    }

    public void Initialize(List<short> deckCardIds)
    {
        Debug.Log("[InGameCardManager] Initializing...");
        _cardDatabase.Initialize();
        InitializeDeck(deckCardIds);
        ShuffleDeck();
    }

    // 테스트용 초기화 함수 (GameManager에서 호출)
    public void TestInitialize()
    {
        Debug.Log("[InGameCardManager] Test Initializing...");
        _cardDatabase.Initialize();
        for (int i = 0; i < 10; i++)
        {
            // 공격카드 0~7 / 수비카드 100~101까지 밖에 없으므로
            short randomAttackCardId = (short)UnityEngine.Random.Range(0, 8);
            short randomDefenseCardId = (short)UnityEngine.Random.Range(100, 102);
            short cointoss = (short)UnityEngine.Random.Range(0, 2);
            short resId = cointoss == 0 ? randomAttackCardId : randomDefenseCardId;

            CardData cardData = _cardDatabase.GetCardDataById(resId);
            if (cardData != null)
            {
                _playerDeck.Add(cardData);
            }
        }
        ShuffleDeck();
    }

    /// <summary>
    /// 로비에서 전달받은 덱 카드 ID 리스트로 게임 덱을 초기화합니다.
    /// GameManager 등을 통해 호출됩니다.
    /// </summary>
    public void InitializeDeck(List<short> deckCardIds)
    {
        _playerDeck.Clear();
        _playerHand.Clear();
        _playerDiscardPile.Clear();

        if (_cardDatabase == null)
        {
            Debug.LogError("Cannot initialize deck, CardDatabase is missing.");
            return;
        }
        if (deckCardIds == null || deckCardIds.Count == 0)
        {
            Debug.LogError("Cannot initialize deck, received empty or null deckCardIds list.");
            return;
        }

        Debug.Log($"Initializing InGame Deck with {deckCardIds.Count} cards.");
        foreach (short cardId in deckCardIds)
        {
            CardData cardData = _cardDatabase.GetCardDataById(cardId);
            if (cardData != null)
            {
                _playerDeck.Add(cardData);
                // Debug.Log($"Added to deck: {cardData.cardName} (ID: {cardData.cardId})");
            }
            else
            {
                Debug.LogWarning($"CardData for ID {cardId} not found in database. Skipping.");
            }
        }
    }

    /// <summary>
    /// 현재 덱을 섞습니다.
    /// </summary>
    public void ShuffleDeck()
    {
        if (_playerDeck == null || _playerDeck.Count == 0)
        {
            Debug.LogWarning("Deck is empty or null, cannot shuffle.");
            return;
        }

        System.Random rng = new System.Random();
        _playerDeck = _playerDeck.OrderBy(a => rng.Next()).ToList();
        Debug.Log("Player deck shuffled.");
    }

    /// <summary>
    /// 지정된 수만큼 덱에서 카드를 뽑아 핸드로 가져옵니다.
    /// 덱이 비면 버려진 카드를 섞어 덱으로 가져올 수 있습니다 (선택적).
    /// </summary>
    public void DrawCards(int amountToDraw)
    {
        if (_playerDeck == null)
        {
            Debug.LogError("PlayerDeck is null. Cannot draw cards.");
            return;
        }

        for (int i = 0; i < amountToDraw; i++)
        {
            if (_playerDeck.Count == 0)
            {
                // 덱이 비었을 때 처리 (예: 버린 카드 더미를 섞어서 덱으로)
                if (_playerDiscardPile.Count > 0)
                {
                    Debug.Log("Deck is empty. Shuffling discard pile into deck.");
                    _playerDeck.AddRange(_playerDiscardPile);
                    _playerDiscardPile.Clear();
                    ShuffleDeck(); // 새 덱 섞기
                    // OnDeckChanged 이벤트는 ShuffleDeck 내부에서 호출됨
                }
                else
                {
                    Debug.LogWarning("Deck is empty and discard pile is also empty. Cannot draw more cards.");
                    break; // 더 이상 뽑을 카드 없음
                }
            }

            // 덱이 여전히 비어있다면 (버린 카드도 없었다면) 중단
            if (_playerDeck.Count == 0) break;


            CardData drawnCard = _playerDeck[0];
            _playerDeck.RemoveAt(0);
            _playerHand.Add(drawnCard); 
        }
        Debug.Log($"Drew {amountToDraw} cards (or less if deck empty). Hand size: {_playerHand.Count}");
    }

    /// <summary>
    /// 게임 시작 시 초기 핸드를 뽑습니다.
    /// </summary>
    public void DrawInitialHand()
    {
        Debug.Log($"Drawing initial hand of {InitialHandSize} cards.");
        DrawCards(InitialHandSize); // 내부적으로 OnCardDrawn을 호출하지 않도록 DrawCards 수정

        // 초기 핸드 드로우가 모두 끝났음을 알림
        OnInitialHandDrawn?.Invoke();
    }

    // 게임 중 카드 한 장 뽑는 함수
    public void DrawOneCard()
    {
        if (_playerDeck.Count == 0)
        {
            // TODO 덱 없음 처리 ...
            return;
        }
        CardData drawnCard = _playerDeck[0];
        _playerDeck.RemoveAt(0);
        _playerHand.Add(drawnCard);
        OnCardDrawn?.Invoke(drawnCard); // 게임 중에는 한 장씩 이벤트 발생
    }

    // 핸드에서 카드 사용 함수
    public bool PlayCardFromHand(CardData cardToPlay)
    {
        if (_playerHand.Contains(cardToPlay))
        {
            // 실제 카드 사용 로직 (자원 소모, 효과 발동 등)은 GameManager나 다른 곳에서 처리
            _playerHand.Remove(cardToPlay);
            _playerDiscardPile.Add(cardToPlay);
            OnCardPlayed?.Invoke(cardToPlay); // 사용한 카드 정보를 이벤트로 전달
            return true;
        }
        Debug.LogWarning($"Card {cardToPlay.cardName} not found in hand.");
        return false;
    }
}