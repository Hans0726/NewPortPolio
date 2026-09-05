using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class InGameFlowController : MonoBehaviour
{
    [Header("Round")]
    [SerializeField] private int _currentRound = 1;
    [SerializeField] private int _maxRound = 99;

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

        if (_combatService != null)
        {
            _combatService.OwnedAttackUnitDestroyed += HandleOwnedAttackUnitDestroyed;
            _combatService.OwnedAttackUnitHealthChanged += HandleOwnedAttackUnitHealthChanged;
            _combatService.OwnedDefenseTargetChanged += HandleOwnedDefenseTargetChanged;
            _combatService.OwnedUnitMovementChanged += HandleOwnedUnitMovementChanged;
            _combatService.OwnedDefenseAttack += HandleOwnedDefenseAttack;
            _combatService.OwnedAttackUnitReachedDestination += HandleOwnedAttackReachedDestination;
        }

        _preparationController.TestCombatRequested += BeginCombat;
        _preparationController.TestNextRoundRequested += StartNextRoundForTest;
        if (_gateway != null)
        {
            _gateway.TurnStarted += ApplyTurnStart;
            _gateway.CombatStartRequested += BeginCombat;
            _gateway.GameResultReceived += FinishGame;
            _gateway.OpponentAttackUnitDestroyed += HandleOpponentAttackUnitDestroyed;
            _gateway.OpponentAttackUnitHealthChanged += HandleOpponentAttackUnitHealthChanged;
            _gateway.OpponentDefenseTargetChanged += HandleOpponentDefenseTargetChanged;
            _gateway.OpponentUnitMovementReceived += HandleOpponentUnitMovement;
            _gateway.OpponentDefenseAttackReceived += HandleOpponentDefenseAttack;
            _gateway.OpponentAttackReachedDestination += HandleOpponentAttackReachedDestination;
        }
        _matchState.LifeChanged += (playerLife, opponentLife) =>
        {
            _gateway.SendPlayerLife(playerLife);
        };

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
        if (_combatService != null)
        {
            _combatService.OwnedAttackUnitDestroyed -= HandleOwnedAttackUnitDestroyed;
            _combatService.OwnedAttackUnitHealthChanged -= HandleOwnedAttackUnitHealthChanged;
            _combatService.OwnedDefenseTargetChanged -= HandleOwnedDefenseTargetChanged;
            _combatService.OwnedUnitMovementChanged -= HandleOwnedUnitMovementChanged;
            _combatService.OwnedDefenseAttack -= HandleOwnedDefenseAttack;
            _combatService.OwnedAttackUnitReachedDestination -= HandleOwnedAttackReachedDestination;
        }
        if (_gateway != null)
        {
            _gateway.TurnStarted -= ApplyTurnStart;
            _gateway.CombatStartRequested -= BeginCombat;
            _gateway.GameResultReceived -= FinishGame;
            _gateway.OpponentAttackUnitDestroyed -= HandleOpponentAttackUnitDestroyed;
            _gateway.OpponentAttackUnitHealthChanged -= HandleOpponentAttackUnitHealthChanged;
            _gateway.OpponentDefenseTargetChanged -= HandleOpponentDefenseTargetChanged;
            _gateway.OpponentUnitMovementReceived -= HandleOpponentUnitMovement;
            _gateway.OpponentDefenseAttackReceived -= HandleOpponentDefenseAttack;
            _gateway.OpponentAttackReachedDestination -= HandleOpponentAttackReachedDestination;
        }

        _initialized = false;
    }

    private void HandleOpeningSequenceFinished()
    {
        _cardState.DrawInitialHand();
        GameManager.Instance?.PlaySfx(GameSfx.CardDraw);
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
            GameManager.Instance?.PlaySfx(GameSfx.CardDraw);
            _hudView.ShowHandForNewRound();
        }

        _preparationController.BeginPreparation(
            _matchState.CurrentRound,
            packet.turnTime);
        GameManager.Instance?.PlaySfx(GameSfx.RoundStart);
    }

    private void BeginCombat()
    {
        if (_combatInProgress || _matchState.IsGameEnded || _combatService == null) return;

        _combatInProgress = true;
        _matchState.SetPhase(InGamePhase.Combat);
        _hudView.HideHandForCombat();
        _hudView.SetPreparationTimeOrCurrentRoundText(-1);
        _cardPlayController.EnterCombat();
        _combatService.StartCombatRound(
            _cardState.SelectedAttackCards,
            _cardPlayController.OpponentAttackCards,
            _matchState.ApplyDestinationDamage,
            CompleteCombatRound);
    }

    private void CompleteCombatRound()
    {
        _combatInProgress = false;
        _cardPlayController.ExitCombat();
        if (_matchState.IsGameEnded) return;
        _preparationController.NotifyCombatRoundFinished();
    }

    private void StartNextRoundForTest()
    {
        _matchState.ApplyRound(_matchState.CurrentRound + 1);
        _cardState.PrepareNextRound();
        _cardState.DrawCards(_cardsToDrawPerRound);
        _hudView.ShowHandForNewRound();
        _preparationController.BeginPreparation(_matchState.CurrentRound, 30);
    }

    private void FinishGame(S_GameResult packet)
    {
        // 퇴장 시 Broadcast로 인한 방어코드
        if (_matchState.IsGameEnded)
            return;

        _combatInProgress = false;
        _combatService.StopCombatRound();
        _cardPlayController.ExitCombat();
        InGameResult result = _matchState.DecideResult(packet.winnerId == _gateway.LocalPlayerId);
        _hudView.ShowGameResult(result);
        GameManager.Instance?.PlaySfx(
            result == InGameResult.Victory ? GameSfx.Victory : GameSfx.Defeat);

        Debug.Log($"[InGameFlowController] Game finished: {result}");

        _gateway.SendPlayerLeave();
        GameManager.Instance.LoadScene(GameScene.A_Lobby,5f);
    }

    private void HandleOwnedAttackUnitDestroyed(int networkUnitId)
    {
        if (!GameConfig.ENABLE_TEST_MODE)
        {
            _gateway?.SendOwnedAttackUnitDestroyed(networkUnitId);
        }
    }

    private void HandleOpponentAttackUnitDestroyed(int networkUnitId)
    {
        _combatService?.DestroyOpponentAttackUnit(networkUnitId);
    }

    private void HandleOwnedAttackUnitHealthChanged(int networkUnitId, int currentHealth)
    {
        _gateway?.SendOwnedAttackUnitHealth(networkUnitId, currentHealth);
    }

    private void HandleOpponentAttackUnitHealthChanged(int networkUnitId, int currentHealth)
    {
        _combatService?.ApplyOpponentAttackUnitHealth(networkUnitId, currentHealth);
    }

    private void HandleOwnedDefenseTargetChanged(int defenseUnitId, int targetUnitId)
    {
        if (!GameConfig.ENABLE_TEST_MODE)
        {
            _gateway?.SendDefenseTarget(defenseUnitId, targetUnitId);
        }
    }

    private void HandleOpponentDefenseTargetChanged(int defenseUnitId, int targetUnitId)
    {
        _combatService?.ApplyOpponentDefenseTarget(defenseUnitId, targetUnitId);
    }

    private void HandleOwnedUnitMovementChanged(
        CombatUnitType unitType,
        int unitId,
        Vector3 position,
        bool flipX,
        bool isHiding)
    {
        _gateway?.SendOwnedUnitMovement(unitType, unitId, position, flipX, isHiding);
    }

    private void HandleOpponentUnitMovement(S_UnitMove packet)
    {
        if (packet == null || !System.Enum.IsDefined(typeof(CombatUnitType), packet.unitType)) return;

        _combatService?.ApplyOpponentUnitMovement(
            (CombatUnitType)packet.unitType,
            packet.unitId,
            new Vector3(packet.x, packet.y, 0f),
            packet.flipX,
            packet.isHiding);
    }

    private void HandleOwnedDefenseAttack(int attackerId, int targetId, int damage)
    {
        _gateway?.SendOwnedDefenseAttack(attackerId, targetId, damage);
    }

    private void HandleOpponentDefenseAttack(S_UnitAttack packet)
    {
        if (packet == null) return;
        _combatService?.ApplyOpponentDefenseAttack(
            packet.attackerId,
            packet.targetId,
            packet.damage);
    }

    private void HandleOwnedAttackReachedDestination(int unitId)
    {
        _gateway?.SendOwnedAttackReachedDestination(unitId);
    }

    private void HandleOpponentAttackReachedDestination(int unitId)
    {
        _combatService?.ApplyOpponentAttackReachedDestination(unitId);
    }

    private void OnApplicationQuit()
    {
        if (GameConfig.ENABLE_TEST_MODE) return;
        _gateway.SendPlayerLeave();
    }
}
