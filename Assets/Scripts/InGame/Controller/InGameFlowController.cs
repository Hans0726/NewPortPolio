using UnityEngine;

public class InGameFlowController : MonoBehaviour
{
    [Header("Round")]
    [SerializeField] private int _currentRound = 1;
    [SerializeField] private int _maxRound = 10;

    [Header("Cost")]
    [SerializeField] private int _startingCost = 1;
    [SerializeField] private int _maxCost = 10;

    [Header("Life")]
    [SerializeField] private int _playerLife = 10;
    [SerializeField] private int _opponentLife = 10;
    [SerializeField] private int _lifeDamagePerAttackUnit = 1;

    [Header("Cards")]
    [SerializeField] private int _cardsToDrawPerRound = 3;

    private InGameMatchState _matchState;
    private InGameCardState _cardState;
    private InGameHUDView _hudView;
    private BattlePreparationController _preparationController;
    private CardPlayController _cardPlayController;
    private CombatRoundManager _combatService;
    private NetworkGateway _gateway;
    private bool _combatInProgress;
    private bool _initialized;

    public InGameMatchState MatchState => _matchState;

    public InGameMatchState CreateMatchState()
    {
        return new InGameMatchState(
            _currentRound,
            _maxRound,
            _startingCost,
            _maxCost,
            _playerLife,
            _opponentLife,
            _lifeDamagePerAttackUnit);
    }

    public void Initialize(
        InGameMatchState matchState,
        InGameCardState cardState,
        InGameHUDView hudView,
        BattlePreparationController preparationController,
        CardPlayController cardPlayController,
        CombatRoundManager combatService,
        NetworkGateway gateway)
    {
        if (_initialized) return;

        _matchState = matchState;
        _cardState = cardState;
        _hudView = hudView;
        _preparationController = preparationController;
        _cardPlayController = cardPlayController;
        _combatService = combatService;
        _gateway = gateway;

        _preparationController.TestCombatRequested += BeginCombat;
        _preparationController.TestNextRoundRequested += StartNextRoundForTest;
        if (_gateway != null)
        {
            _gateway.TurnStarted += ApplyTurnStart;
            _gateway.CombatStartRequested += BeginCombat;
        }

        _initialized = true;
    }

    public void StartGame()
    {
        if (!_initialized)
        {
            Debug.LogError("[InGameFlowController] Initialize must be called first.");
            return;
        }

        _matchState.SetPhase(InGamePhase.Opening);
        _hudView.PlayOpeningSequence(HandleOpeningSequenceFinished);
    }

    public void Dispose()
    {
        if (!_initialized) return;

        _preparationController.TestCombatRequested -= BeginCombat;
        _preparationController.TestNextRoundRequested -= StartNextRoundForTest;
        if (_gateway != null)
        {
            _gateway.TurnStarted -= ApplyTurnStart;
            _gateway.CombatStartRequested -= BeginCombat;
        }

        _initialized = false;
    }

    private void HandleOpeningSequenceFinished()
    {
        _cardState.DrawInitialHand();
        _hudView.ShowInitialHand();
        _preparationController.NotifyOpeningSequenceFinished();
    }

    private void ApplyTurnStart(S_TurnStart packet)
    {
        if (packet == null || _matchState.IsGameEnded) return;

        bool isNewRound = _matchState.ApplyRound(packet.turnNumber);
        if (isNewRound)
        {
            _cardState.PrepareNextRound();
            _cardState.DrawCards(_cardsToDrawPerRound);
            _hudView.ShowHandForNewRound();
        }

        _preparationController.BeginPreparation(
            _matchState.CurrentRound,
            packet.turnTime);
    }

    private void BeginCombat()
    {
        if (_combatInProgress || _matchState.IsGameEnded || _combatService == null) return;

        _combatInProgress = true;
        _matchState.SetPhase(InGamePhase.Combat);
        _hudView.HideHandForCombat();
        _combatService.StartCombatRound(
            _cardState.SelectedAttackCards,
            _cardPlayController.OpponentAttackCards,
            ApplyAttackUnitDestinationDamage,
            CompleteCombatRound);
    }

    private void ApplyAttackUnitDestinationDamage(AttackUnitOwner owner)
    {
        _matchState.ApplyDestinationDamage(owner);
        if (_matchState.PlayerLife <= 0 || _matchState.OpponentLife <= 0)
        {
            FinishGame();
        }
    }

    private void CompleteCombatRound()
    {
        _combatInProgress = false;
        if (_matchState.IsGameEnded) return;

        if (_matchState.PlayerLife <= 0 ||
            _matchState.OpponentLife <= 0 ||
            _matchState.CurrentRound >= _matchState.MaxRound)
        {
            FinishGame();
            return;
        }

        _preparationController.NotifyCombatRoundFinished();
    }

    private void StartNextRoundForTest()
    {
        if (_matchState.CurrentRound >= _matchState.MaxRound)
        {
            FinishGame();
            return;
        }

        _matchState.ApplyRound(_matchState.CurrentRound + 1);
        _cardState.PrepareNextRound();
        _cardState.DrawCards(_cardsToDrawPerRound);
        _hudView.ShowHandForNewRound();
        _preparationController.BeginPreparation(_matchState.CurrentRound, 30);
    }

    private void FinishGame()
    {
        _combatInProgress = false;
        InGameResult result = _matchState.DecideResult();
        Debug.Log($"[InGameFlowController] Game finished: {result}");
    }
}
