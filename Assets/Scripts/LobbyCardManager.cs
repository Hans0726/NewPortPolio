using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCardManager : MonoBehaviour
{
    private static LobbyCardManager _instance = null;
    public static LobbyCardManager Instance { get { return _instance; } }

    [SerializeField] private CardDatabase _cardDatabase;

    // 플레이어가 소유한 모든 카드의 CardData SO 참조 리스트
    private List<CardData> _ownedPlayerCards = new List<CardData>();
    public List<CardData> OwnedPlayerCards { get { return _ownedPlayerCards; } }

    // 플레이어의 현재 덱에 포함된 카드 ID 리스트 (IngameCardManager로 전달될 정보)
    private List<short> _currentDeckCardIds = new List<short>();
    public List<short> CurrentDeckCardIds { get { return _currentDeckCardIds; } }
    public int NumCardInDeck => _currentDeckCardIds.Count;

    [SerializeField] private GameObject _cardPrefab;

    // 덱 구성 변경 시 UIPopup_Deck에 알릴 이벤트
    public event Action OnDeckCompositionChanged;

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    // 서버로부터 받은 S_PlayerDeckInfo 패킷으로 소유 카드 정보 초기화
    public void InitializePlayerDeck(S_PlayerDeckInfo packet)
    {
        _ownedPlayerCards.Clear();
        _currentDeckCardIds.Clear();

        if (packet == null || packet.cards == null)
        {
            Debug.LogError("Received null packet or null cards list.");
            return;
        }

        // CardDatabase에서 cardId로 CardData SO를 찾아옴
        _cardDatabase.Initialize();

        foreach (S_PlayerDeckInfo.Card cardInfoFromServer in packet.cards) // 패킷 필드명은 실제 정의에 맞게
        {

            CardData cardDataSO = _cardDatabase.GetCardDataById(cardInfoFromServer.cardId);

            if (cardDataSO != null)
            {
                _ownedPlayerCards.Add(cardDataSO); // 소유한 카드 목록에 SO 참조 추가

                if (cardInfoFromServer.isInDeck)
                {
                    _currentDeckCardIds.Add(cardDataSO.cardId); // 덱에 포함된 카드 ID 추가
                }
            }
            else
            {
                Debug.LogWarning($"CardData for cardId {cardInfoFromServer.cardId} not found in database. Skipping.");
            }
        }

        Debug.Log($"Player cards initialized. Owned: {_ownedPlayerCards.Count}, In Deck: {NumCardInDeck}");

        // 이 시점에서 _currentDeckCardIds 리스트를 IngameCardManager에게 전달할 준비가 됨
    }

    public bool TryAddCardToDeck(short cardId)
    {
        if (_currentDeckCardIds.Count >= UIPopup_Deck.MAX_NUM_CARDS)
        {
            Debug.Log("Deck is full.");
            return false;
        }
        if (!_currentDeckCardIds.Contains(cardId))
        {
            _currentDeckCardIds.Add(cardId);
            OnDeckCompositionChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool TryRemoveCardFromDeck(short cardId)
    {
        if (_currentDeckCardIds.Contains(cardId))
        {
            _currentDeckCardIds.Remove(cardId);
            OnDeckCompositionChanged?.Invoke(); // UI 갱신 알림
            return true;
        }
        return false; // 덱에 없는 카드
    }

    public void SendUpdatedDeckToServer()
    {
        C_PlayerDeckInfo deckPacket = new C_PlayerDeckInfo();
        foreach (CardData cardData in _ownedPlayerCards)
        {
            bool isInCurrentDeck = _currentDeckCardIds.Contains(cardData.cardId);
            deckPacket.cards.Add(new C_PlayerDeckInfo.Card { cardId = cardData.cardId, isInDeck = isInCurrentDeck });
        }
        Debug.Log($"Sending updated deck to server. Card count: {deckPacket.cards.Count}");
        NetworkMananger.Instance.Send(deckPacket.Serialize());
    }
}