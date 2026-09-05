using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CombatRoundManager : MonoBehaviour
{
    [Header("Combat Speed")]
    [SerializeField] private bool _enableCombatSpeedUp = true;
    [SerializeField, Min(0f)] private float _speedUpStartSeconds = 20f;
    [SerializeField, Min(0f)] private float _speedUpRampSeconds = 5f;
    [SerializeField, Min(1f)] private float _maxCombatSpeed = 2f;

    private bool _combatActive;
    private float _realCombatElapsed;
    public float CombatSpeed { get; private set; } = 1f;
    public float CombatTime { get; private set; }
    public float CombatDeltaTime { get; private set; }

    // Advance before unit Updates; only the combat clock is accelerated.
    private void Update()
    {
        if (_combatActive) _realCombatElapsed += Time.unscaledDeltaTime;
        float ramp = _speedUpRampSeconds > 0f
            ? Mathf.Clamp01((_realCombatElapsed - _speedUpStartSeconds) / _speedUpRampSeconds)
            : (_realCombatElapsed >= _speedUpStartSeconds ? 1f : 0f);
        CombatSpeed = _combatActive && _enableCombatSpeedUp
            ? Mathf.Lerp(1f, Mathf.Max(1f, _maxCombatSpeed), ramp)
            : 1f;
        CombatDeltaTime = Time.deltaTime * CombatSpeed;
        CombatTime += CombatDeltaTime;
    }

    private void ResetCombatSpeed()
    {
        _combatActive = false;
        _realCombatElapsed = 0f;
        CombatSpeed = 1f;
        CombatDeltaTime = Time.deltaTime;
    }

    private void OnDisable()
    {
        StopCombatRound();
    }

    [Header("Attack Unit Spawn")]
    [SerializeField] private WaypointPath _enemyAttackPath;
    [SerializeField] private Transform _attackUnitRoot;
    [SerializeField] private float _spawnInterval = 0.35f;
    [SerializeField] private float _unitZ = -1f;

    [Header("Render")]
    [SerializeField] private string _unitSortingLayerName = "Layer 1";
    [SerializeField] private int _attackUnitSortingOrder = 80;
    [SerializeField] private float _attackUnitBaseScale = 1f;
    [SerializeField] private float _attackUnitBottomAnchorYOffset = 0f;

    private Coroutine _combatRoutine;
    private Action<AttackUnitOwner> _onAttackUnitReachedDestination;
    private Action _onCombatRoundFinished;
    private DefensePlacementManager _placementService;
    private readonly HashSet<int> _pendingOpponentUnitDestroys = new HashSet<int>();
    private readonly Dictionary<int, int> _pendingOpponentUnitHealth = new Dictionary<int, int>();
    private readonly Dictionary<int, (Vector3 Position, bool FlipX, bool IsHiding)> _pendingOpponentAttackMovements =
        new Dictionary<int, (Vector3, bool, bool)>();
    private readonly HashSet<int> _pendingOpponentDestinations = new HashSet<int>();
    private int _combatSequence;

    public event Action<int> OwnedAttackUnitDestroyed;
    public event Action<int, int> OwnedAttackUnitHealthChanged;
    public event Action<int, int> OwnedDefenseTargetChanged;
    public event Action<CombatUnitType, int, Vector3, bool, bool> OwnedUnitMovementChanged;
    public event Action<int, int, int> OwnedDefenseAttack;
    public event Action<int> OwnedAttackUnitReachedDestination;

    public void Configure(DefensePlacementManager placementService)
    {
        if (_placementService != null)
        {
            _placementService.OwnedDefenseTargetChanged -= HandleOwnedDefenseTargetChanged;
            _placementService.OwnedUnitMovementChanged -= HandleOwnedUnitMovementChanged;
            _placementService.OwnedDefenseAttack -= HandleOwnedDefenseAttack;
        }

        _placementService = placementService;
        if (_placementService != null)
        {
            _placementService.OwnedDefenseTargetChanged += HandleOwnedDefenseTargetChanged;
            _placementService.OwnedUnitMovementChanged += HandleOwnedUnitMovementChanged;
            _placementService.OwnedDefenseAttack += HandleOwnedDefenseAttack;
        }
    }

    public void StartCombatRound(IReadOnlyList<CardData> playerAttackCards, IReadOnlyList<CardData> opponentAttackCards = null, Action<AttackUnitOwner> onAttackUnitReachedDestination = null, Action onCombatRoundFinished = null)
    {
        _combatSequence++;
        ResetCombatSpeed();
        _combatActive = true;
        EnsureAttackPath();
        _placementService?.SetPlacedUnitsCombatActive(true, this);
        _onAttackUnitReachedDestination = onAttackUnitReachedDestination;
        _onCombatRoundFinished = onCombatRoundFinished;

        if (_combatRoutine != null)
        {
            StopCoroutine(_combatRoutine);
        }

        _combatRoutine = StartCoroutine(RunCombatRound(playerAttackCards, opponentAttackCards));
    }

    public void StopCombatRound()
    {
        ResetCombatSpeed();
        AttackUnitRegistry.InActivateAttackUnits();
        _pendingOpponentUnitDestroys.Clear();
        _pendingOpponentUnitHealth.Clear();
        _pendingOpponentAttackMovements.Clear();
        _pendingOpponentDestinations.Clear();
        if (_combatRoutine != null)
        {
            StopCoroutine(_combatRoutine);
            _combatRoutine = null;
        }
        _placementService?.SetPlacedUnitsCombatActive(false);
        _onAttackUnitReachedDestination = null;
        _onCombatRoundFinished = null;
    }

    private IEnumerator RunCombatRound(IReadOnlyList<CardData> playerAttackCards, IReadOnlyList<CardData> opponentAttackCards)
    {
        yield return SpawnAttackUnits(playerAttackCards, _enemyAttackPath, AttackUnitOwner.Player, false);
        yield return SpawnAttackUnits(opponentAttackCards, _enemyAttackPath, AttackUnitOwner.Opponent, true);

        while (AttackUnitRegistry.ActiveCount > 0)
        {
            yield return null;
        }

        _combatRoutine = null;
        ResetCombatSpeed();
        _placementService?.SetPlacedUnitsCombatActive(false);
        Action onFinished = _onCombatRoundFinished;
        _onAttackUnitReachedDestination = null;
        _onCombatRoundFinished = null;
        onFinished?.Invoke();
    }

    private IEnumerator SpawnAttackUnits(IReadOnlyList<CardData> attackCards, WaypointPath path, AttackUnitOwner owner, bool reversePath)
    {
        if (attackCards == null || attackCards.Count == 0)
        {
            yield break;
        }

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[CombatRoundManager] {owner} attack path is missing. Create a WaypointPath object with child points.");
            yield break;
        }

        for (int i = 0; i < attackCards.Count; i++)
        {
            CardData card = attackCards[i];
            if (card != null && card.cardType == CardType.Attack)
            {
                int networkUnitId = (_combatSequence * 10000) + i;
                SpawnAttackUnit(card, path, owner, reversePath, networkUnitId);
                float remaining = _spawnInterval;
                do
                {
                    yield return null;
                    remaining -= CombatDeltaTime;
                } while (remaining > 0f);
            }
        }
    }

    private void SpawnAttackUnit(CardData card, WaypointPath path, AttackUnitOwner owner, bool reversePath, int networkUnitId)
    {
        GameObject unitObject = new GameObject($"AttackUnit_{owner}_{card.cardName}");
        if (_attackUnitRoot != null)
        {
            unitObject.transform.SetParent(_attackUnitRoot, true);
        }

        AttackUnit unit = unitObject.AddComponent<AttackUnit>();
        unit.SetCombatClock(this);
        unit.Initialize(
            card,
            path,
            reversePath,
            ResolveSortingLayerID(),
            _attackUnitSortingOrder,
            _unitZ,
            _attackUnitBaseScale,
            _attackUnitBottomAnchorYOffset,
            owner,
            networkUnitId,
            _onAttackUnitReachedDestination,
            HandleOwnedAttackUnitDestroyed,
            HandleOwnedAttackUnitHealthChanged,
            HandleOwnedUnitMovementChanged,
            HandleOwnedAttackUnitReachedDestination);

        if (owner == AttackUnitOwner.Opponent && _pendingOpponentDestinations.Remove(networkUnitId))
        {
            unit.ApplyAuthoritativeDestroy();
            _onAttackUnitReachedDestination?.Invoke(AttackUnitOwner.Opponent);
            return;
        }

        if (owner == AttackUnitOwner.Opponent && _pendingOpponentUnitDestroys.Remove(networkUnitId))
        {
            _pendingOpponentUnitHealth.Remove(networkUnitId);
            unit.ApplyAuthoritativeDestroy();
            return;
        }

        if (owner == AttackUnitOwner.Opponent &&
            _pendingOpponentUnitHealth.Remove(networkUnitId, out int currentHealth))
        {
            unit.ApplyAuthoritativeHealth(currentHealth);
        }

        if (owner == AttackUnitOwner.Opponent &&
            _pendingOpponentAttackMovements.Remove(networkUnitId, out var movement))
        {
            unit.ApplyAuthoritativeMovement(movement.Position, movement.FlipX, movement.IsHiding);
        }
    }

    public void DestroyOpponentAttackUnit(int networkUnitId)
    {
        AttackUnit unit = AttackUnitRegistry.FindByNetworkId(
            networkUnitId,
            AttackUnitOwner.Opponent);

        if (unit != null)
        {
            unit.ApplyAuthoritativeDestroy();
            return;
        }

        _pendingOpponentUnitDestroys.Add(networkUnitId);
    }

    public void ApplyOpponentAttackUnitHealth(int networkUnitId, int currentHealth)
    {
        AttackUnit unit = AttackUnitRegistry.FindByNetworkId(
            networkUnitId,
            AttackUnitOwner.Opponent);
        if (unit != null)
        {
            unit.ApplyAuthoritativeHealth(currentHealth);
            return;
        }

        _pendingOpponentUnitHealth[networkUnitId] = currentHealth;
    }

    public void ApplyOpponentDefenseTarget(int defenseUnitId, int targetUnitId)
    {
        _placementService?.ApplyOpponentDefenseTarget(defenseUnitId, targetUnitId);
    }

    private void HandleOwnedAttackUnitDestroyed(int networkUnitId)
    {
        OwnedAttackUnitDestroyed?.Invoke(networkUnitId);
    }

    public void ApplyOpponentUnitMovement(
        CombatUnitType unitType,
        int unitId,
        Vector3 ownerPosition,
        bool ownerFlipX,
        bool isHiding)
    {
        bool localFlipX = !ownerFlipX;

        if (unitType == CombatUnitType.Defense)
        {
            Vector3 localPosition = _placementService != null
                ? _placementService.MirrorWorldPosition(ownerPosition)
                : new Vector3(-ownerPosition.x, -ownerPosition.y, ownerPosition.z);
            _placementService?.ApplyOpponentDefenseMovement(unitId, localPosition, localFlipX);
            return;
        }

        AttackUnit unit = AttackUnitRegistry.FindByNetworkId(unitId, AttackUnitOwner.Opponent);
        if (unit != null)
        {
            unit.ApplyAuthoritativeMovement(ownerPosition, localFlipX, isHiding);
            return;
        }

        _pendingOpponentAttackMovements[unitId] = (ownerPosition, localFlipX, isHiding);
    }

    public void ApplyOpponentDefenseAttack(int attackerUnitId, int targetUnitId, int damage)
    {
        _placementService?.PlayOpponentDefenseAttackFeedback(attackerUnitId);
        AttackUnit target = AttackUnitRegistry.FindByNetworkId(
            targetUnitId,
            AttackUnitOwner.Player);
        target?.TakeDamage(damage);
    }

    public void ApplyOpponentAttackReachedDestination(int unitId)
    {
        AttackUnit unit = AttackUnitRegistry.FindByNetworkId(unitId, AttackUnitOwner.Opponent);
        if (unit != null)
        {
            unit.ApplyAuthoritativeDestroy();
            _onAttackUnitReachedDestination?.Invoke(AttackUnitOwner.Opponent);
            return;
        }

        _pendingOpponentDestinations.Add(unitId);
    }

    private void HandleOwnedAttackUnitHealthChanged(int networkUnitId, int currentHealth)
    {
        OwnedAttackUnitHealthChanged?.Invoke(networkUnitId, currentHealth);
    }

    private void HandleOwnedDefenseTargetChanged(int defenseUnitId, int targetUnitId)
    {
        OwnedDefenseTargetChanged?.Invoke(defenseUnitId, targetUnitId);
    }

    private void HandleOwnedUnitMovementChanged(
        CombatUnitType unitType,
        int unitId,
        Vector3 position,
        bool flipX,
        bool isHiding)
    {
        OwnedUnitMovementChanged?.Invoke(unitType, unitId, position, flipX, isHiding);
    }

    private void HandleOwnedDefenseAttack(int attackerId, int targetId, int damage)
    {
        OwnedDefenseAttack?.Invoke(attackerId, targetId, damage);
    }

    private void HandleOwnedAttackUnitReachedDestination(int unitId)
    {
        OwnedAttackUnitReachedDestination?.Invoke(unitId);
    }

    private void EnsureAttackPath()
    {
        if (_enemyAttackPath != null) return;

        _enemyAttackPath = FindAnyObjectByType<WaypointPath>();
    }

    private int ResolveSortingLayerID()
    {
        int sortingLayerId = SortingLayer.NameToID(_unitSortingLayerName);
        if (sortingLayerId == 0 && _unitSortingLayerName != "Default")
        {
            Debug.LogWarning($"[CombatRoundManager] Sorting Layer '{_unitSortingLayerName}' was not found.");
        }

        return sortingLayerId;
    }
}
