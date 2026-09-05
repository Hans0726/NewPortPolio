using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LobbyDeckController : MonoBehaviour
{
    private NetworkGateway _gateway;
    private UIPopup_Deck _view;
    private LobbyDeckState _deckState;
    [SerializeField] private CardDatabase _cardDatabase;

    private bool _initialized;

    public void Initialize(
        NetworkGateway gateway,
        UIPopup_Deck view,
        LobbyDeckState deckState,
        PlayerCardState playerCardState)
    {
        if (_initialized)
            return;

        _gateway = gateway;
        _view = view;
        _deckState = deckState;  


        _gateway.PlayerDeckInfoReceived += SetDeck;
        _deckState.Changed += RefreshView;
        _view.SaveDeck += UpdatePlayerDeckInfo;
        _view.OnOwnedCardSelected += HandleOwnedCardSelected;
        _view.OnDeckCardSelected += HandleDeckCardSelected;

        _initialized = true;

        // Restore on lobby re-entry, or when the initial packet arrived before subscription.
        if (playerCardState.TryGetSnapshot(out var cachedCards))
        {
            SetDeck(cachedCards);
        }
    }

    public void SetDeck(List<(short cardId, bool isInDeck)> deckFromServer)
    {
        // CardDatabase에서 cardId로 CardData SO를 찾아옴
        _cardDatabase.Initialize();

        List<CardData> ownedCards = new List<CardData>();
        List<short> currentDeckCardIds = new List<short>();

        foreach ((short id, bool isIn) card in deckFromServer) // 패킷 필드명은 실제 정의에 맞게
        {
            CardData cardDataSO = _cardDatabase.GetCardDataById(card.id);

            if (cardDataSO != null)
            {
                ownedCards.Add(cardDataSO); // 소유한 카드 목록에 SO 참조 추가

                if (card.isIn)
                {
                    currentDeckCardIds.Add(cardDataSO.cardId); // 덱에 포함된 카드 ID 추가
                }
            }
            else
            {
                Debug.LogWarning($"CardData for cardId {card.id} not found in database. Skipping.");
            }
        }

        _deckState.Initialize(ownedCards, currentDeckCardIds);
        Debug.Log($"Player cards initialized. Owned: {_deckState.OwnedPlayerCards.Count}, In Deck: {_deckState.NumCardInDeck}");
        // 이 시점에서 CurrentDeckCardIds 리스트를 InGameCardManager에게 전달할 준비가 됨
        _view.Render(_deckState.CreateViewModel());
    }

    public void UpdatePlayerDeckInfo()
    {
        if (!_deckState.IsDeckComplete)
        {
            GameManager.Instance.ShowWarningPopup($"덱에 카드가 부족합니다.\n{_deckState.MaxDeckSize}장의 카드를 구성해야 합니다");
            return;
        }

        List<Card> deckPacketCards = new List<Card>();
        foreach (CardData cardData in _deckState.OwnedPlayerCards)
        {
            bool isInCurrentDeck = _deckState.CurrentDeckCardIds.Contains(cardData.cardId);
            deckPacketCards.Add(new Card { cardId = cardData.cardId, isInDeck = isInCurrentDeck });
        }
        Debug.Log($"Sending updated deck to server. Card count: {_deckState.NumCardInDeck}");

        _gateway.UpdatePlayerDeckInfo(deckPacketCards);
    }


    private void HandleOwnedCardSelected(short cardId)
    {
        _deckState.TryAddCardToDeck(cardId);
    }

    private void HandleDeckCardSelected(short cardId)
    {
        _deckState.TryRemoveCardFromDeck(cardId);
    }

    private void RefreshView()
    {
        _view.Render(_deckState.CreateViewModel());
    }


    public void Dispose()
    {
        if (!_initialized)
            return;

        _gateway.PlayerDeckInfoReceived -= SetDeck;
        _view.SaveDeck -= UpdatePlayerDeckInfo;
        _deckState.Changed -= RefreshView;
        _view.OnOwnedCardSelected -= HandleOwnedCardSelected;
        _view.OnDeckCardSelected -= HandleDeckCardSelected;

        _initialized = false;
    }
}
