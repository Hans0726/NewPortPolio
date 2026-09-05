using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchingController : MonoBehaviour
{
    private NetworkGateway _gateway;
    private UIPopup_Matching _view;
    private LobbyDeckState _deckState;
    private bool _initialized;
    private bool _isMatching = false;
    private Coroutine _timerCoroutine;
    private TimeSpan _currentTime;

    public void Initialize(
        NetworkGateway gateway,
        UIPopup_Matching view,
        LobbyDeckState deckState)
    {
        if (_initialized)
            return;

        _gateway = gateway;
        _view = view;
        _deckState = deckState;

        _gateway.MatchingRequestAccepted += MatchingReqOk;
        _gateway.MatchingSuccess += MatchingSuccess;

        _view.MatchingRequest += RequestMatching;
        _view.MatchingCancelRequest += CancelMatching;

        _initialized = true;
    }

    public void RequestMatching()
    {
        if (_deckState.IsDeckComplete == false)
        {
            _view.UpdateMatchingStatusText($"덱에 카드가 부족합니다.\n{_deckState.MaxDeckSize}장의 카드를 구성해야 합니다.");
            return;
        }
        _gateway.RequestMatching();
        _view.UpdateMatchingStatusText("매칭 요청 중...");
    }

    public void CancelMatching()
    {
        _isMatching = false;

        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        _gateway.CancelMatching();
    }

    public void MatchingReqOk()
    {
        _isMatching = true;
        _currentTime = TimeSpan.Zero;
        _timerCoroutine = StartCoroutine(RunMatchingTimer());
    }

    public void MatchingSuccess()
    {
        _isMatching = false;
        _view.SetCancelInteractable();
        _view.UpdateMatchingStatusText("매칭 성공");        
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        GameManager.Instance.SetDeckForNextMatch(
        _deckState.CurrentDeckCardIds);

        GameManager.Instance.LoadScene(GameScene.B_InGame);
    }
    private IEnumerator RunMatchingTimer()
    {
        _currentTime = TimeSpan.Zero;

        while (_isMatching)
        {
            _view.SetElapsedTime(_currentTime);
            yield return new WaitForSeconds(1f);
            _currentTime += TimeSpan.FromSeconds(1);
        }
    }

    public void Dispose()
    {
        if (!_initialized)
            return;

        _isMatching = false;

        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        _gateway.MatchingRequestAccepted -= MatchingReqOk;
        _gateway.MatchingSuccess -= MatchingSuccess;

        _view.MatchingRequest -= RequestMatching;
        _view.MatchingCancelRequest -= CancelMatching;

        _initialized = false;
    }
}
