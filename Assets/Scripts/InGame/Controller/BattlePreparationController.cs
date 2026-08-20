using System;
using System.Collections;
using UnityEngine;

public class BattlePreparationController : MonoBehaviour
{
    [SerializeField] private int _preparationSeconds = 30;

    private InGameMatchState _matchState;
    private InGameHUDView _hudView;
    private InGameHandUI _handView;
    private CardPlayController _cardPlayController;
    private NetworkGateway _gateway;
    private Coroutine _timerRoutine;
    private bool _isPreparing;
    private bool _readyRequested;
    private bool _readyPendingAfterPlacement;
    private bool _initialized;

    public event Action TestCombatRequested;
    public event Action TestNextRoundRequested;

    public bool IsPreparing => _isPreparing;

    public void Initialize(
        InGameMatchState matchState,
        InGameHUDView hudView,
        InGameHandUI handView,
        CardPlayController cardPlayController,
        NetworkGateway gateway)
    {
        if (_initialized) return;

        _matchState = matchState;
        _hudView = hudView;
        _handView = handView;
        _cardPlayController = cardPlayController;
        _gateway = gateway;

        _hudView.TurnEndRequested += RequestTurnEnd;
        _cardPlayController.PlacementFinished += HandlePlacementFinished;
        _initialized = true;
    }

    public void Dispose()
    {
        if (!_initialized) return;

        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        _hudView.TurnEndRequested -= RequestTurnEnd;
        _cardPlayController.PlacementFinished -= HandlePlacementFinished;
        _initialized = false;
    }

    public void NotifyOpeningSequenceFinished()
    {
        if (GameConfig.ENABLE_TEST_MODE)
        {
            BeginPreparation(_matchState.CurrentRound, _preparationSeconds);
        }
        else
        {
            _gateway?.SendTurnStartReady();
        }
    }

    public void BeginPreparation(int round, int preparationSeconds)
    {
        _isPreparing = true;
        _readyRequested = false;
        _readyPendingAfterPlacement = false;
        _matchState.SetPhase(InGamePhase.Preparation);
        _cardPlayController.SetPreparationActive(true);
        _handView.SetInteractionLocked(false);
        _hudView.SetTurnEndInteractable(true);
        _hudView.SetPreparationTimeOrCurrentRoundText(preparationSeconds);

        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
        }

        int duration = preparationSeconds > 0
            ? preparationSeconds
            : _preparationSeconds;
        _timerRoutine = StartCoroutine(RunPreparationTimer(duration));
    }

    public void RequestTurnEnd()
    {
        if (!_isPreparing || _readyRequested) return;

        if (_cardPlayController.IsPlacementActive)
        {
            _readyPendingAfterPlacement = true;
            _handView.SetInteractionLocked(true);
            _hudView.SetTurnEndInteractable(false);
            return;
        }

        CompletePreparationRequest();
    }

    public void NotifyCombatRoundFinished()
    {
        if (GameConfig.ENABLE_TEST_MODE)
        {
            TestNextRoundRequested?.Invoke();
        }
        else
        {
            _gateway?.SendTurnStartReady();
        }
    }

    private IEnumerator RunPreparationTimer(int duration)
    {
        float remainingTime = Mathf.Max(0, duration);
        while (_isPreparing && !_readyRequested && remainingTime > 0f)
        {
            _hudView.SetPreparationTimeOrCurrentRoundText(Mathf.CeilToInt(remainingTime));
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        _hudView.SetPreparationTimeOrCurrentRoundText(0);
        _timerRoutine = null;

        if (_isPreparing && !_readyRequested)
        {
            RequestTurnEnd();
        }
    }

    private void CompletePreparationRequest()
    {
        _readyRequested = true;
        _isPreparing = false;
        _cardPlayController.SetPreparationActive(false);
        _handView.SetInteractionLocked(true);
        _hudView.SetTurnEndInteractable(false);

        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        if (GameConfig.ENABLE_TEST_MODE)
        {
            TestCombatRequested?.Invoke();
        }
        else
        {
            _gateway?.SendTurnEnd();
        }
    }

    private void HandlePlacementFinished()
    {
        if (_readyPendingAfterPlacement)
        {
            _readyPendingAfterPlacement = false;
            CompletePreparationRequest();
            return;
        }

        if (_isPreparing && !_readyRequested)
        {
            _handView.SetInteractionLocked(false);
        }
    }
}
