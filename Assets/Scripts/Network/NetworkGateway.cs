using System;
using System.Collections.Generic;

public class NetworkGateway
{
    private readonly NetworkMananger _networkManager;

    public NetworkGateway(NetworkMananger networkManager)
    {
        _networkManager = networkManager;
    }

    #region LobbyCard
    public event Action<List<(short, bool)>> PlayerDeckInfoReceived;
    
    public void PlayerDeckInfoHandle(S_PlayerDeckInfo packet)
    {
        List<(short, bool)> cardDataList = new List<(short cardId, bool isInDeck)>();
        foreach (S_PlayerDeckInfo.Card deckFromServer in packet.cards)
        {
            cardDataList.Add((deckFromServer.cardId, deckFromServer.isInDeck));
        }
        PlayerDeckInfoReceived?.Invoke(cardDataList);
    }

    public void UpdatePlayerDeckInfo(IReadOnlyList<Card> cards)
    {
        C_PlayerDeckInfo deckPacket = new C_PlayerDeckInfo();

        foreach (Card card in cards)
        {
            deckPacket.cards.Add(new C_PlayerDeckInfo.Card { cardId = card.cardId, isInDeck = card.isInDeck });
        }

        _networkManager.Send(deckPacket.Serialize());
    }
    #endregion

    #region Matchmaking
    public event Action MatchingRequestAccepted;
    public event Action MatchingSuccess;

    public void MatchingRequestAcceptedHandle(S_PlayerMatchingReqOk packet)
    {
        MatchingRequestAccepted?.Invoke();
    }

    public void MatchSuccessHandle(S_MatchingSuccess packet)
    {
        MatchingSuccess?.Invoke();
    }

    public void RequestMatching()
    {
        C_PlayerMatchingReq packet = new C_PlayerMatchingReq();
        _networkManager.Send(packet.Serialize());
    }

    public void CancelMatching()
    {
        C_PlayerMatchingReqCancel packet = new C_PlayerMatchingReqCancel();
        _networkManager.Send(packet.Serialize());
    }
    #endregion

    #region InGame

    public event Action<S_TurnStart> TurnStarted;
    public event Action CombatStartRequested;
    public event Action<S_CardSelectResult> OpponentCardSelectionReceived;
    public event Action<S_UnitPlacementResult> OpponentDefensePlacementReceived;
    public event Action<S_LifeUpdate> LifeUpdated;
    public event Action<S_GameResult> GameResultReceived;

    public void TurnStartHandle(S_TurnStart packet) => TurnStarted?.Invoke(packet);

    public void TurnEndHandle(S_TurnEnd packet) => CombatStartRequested?.Invoke();

    public void CardSelectResultHandle(S_CardSelectResult packet) =>
        OpponentCardSelectionReceived?.Invoke(packet);

    public void UnitPlacementResultHandle(S_UnitPlacementResult packet) =>
        OpponentDefensePlacementReceived?.Invoke(packet);

    public void LifeUpdateHandle(S_LifeUpdate packet) => LifeUpdated?.Invoke(packet);

    public void GameResultHandle(S_GameResult packet) => GameResultReceived?.Invoke(packet);

    public void SendTurnStartReady()
    {
        C_TurnStartReady packet = new C_TurnStartReady { ready = true };
        _networkManager.Send(packet.Serialize());
    }

    public void SendTurnEnd()
    {
        _networkManager.Send(new C_TurnEnd().Serialize());
    }

    public void SendSelectedAttackCard(short cardId)
    {
        C_CardSelect packet = new C_CardSelect();
        packet.selectedCardIdss.Add(new C_CardSelect.SelectedCardIds { cardId = cardId });
        _networkManager.Send(packet.Serialize());
    }

    public void SendDefensePlacement(short cardId, float x, float y)
    {
        C_UnitPlacement packet = new C_UnitPlacement
        {
            cardId = cardId,
            x = x,
            y = y
        };
        _networkManager.Send(packet.Serialize());
    }

    #endregion
}
