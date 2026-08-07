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
}
