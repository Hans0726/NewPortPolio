using System.Collections.Generic;
using UnityEngine;

public class LobbySceneInstaller : MonoBehaviour
{
    [SerializeField] private MatchingController _matchmakingController;
    [SerializeField] private LobbyDeckController _lobbyDeckController;
    [SerializeField] private UIPopup_Matching _matchingPopupView;
    [SerializeField] private UIPopup_Deck _deckPopupView;
    private LobbyDeckState _lobbyDeckState;

    private void Awake()
    {
        _lobbyDeckState = new LobbyDeckState(maxDeckSize: 10);
    }

    private void Start()
    {
        NetworkGateway gateway = NetworkMananger.Instance.Gateway;

        _lobbyDeckController.Initialize(
        gateway,
        _deckPopupView,
        _lobbyDeckState);

        _matchmakingController.Initialize(
            gateway,
            _matchingPopupView,
            _lobbyDeckState);
    }

    private void OnDestroy()
    {
        _matchmakingController.Dispose();
        _lobbyDeckController.Dispose();
    }
}
