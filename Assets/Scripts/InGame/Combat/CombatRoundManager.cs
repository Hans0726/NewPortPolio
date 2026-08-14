using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class CombatRoundManager : MonoBehaviour
{
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

    public void Configure(DefensePlacementManager placementService)
    {
        _placementService = placementService;
    }

    public void StartCombatRound(IReadOnlyList<CardData> playerAttackCards, IReadOnlyList<CardData> opponentAttackCards = null, Action<AttackUnitOwner> onAttackUnitReachedDestination = null, Action onCombatRoundFinished = null)
    {
        EnsureAttackPath();
        _placementService?.SetPlacedUnitsCombatActive(true);
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
        AttackUnitRegistry.InActivateAttackUnits();
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
                SpawnAttackUnit(card, path, owner, reversePath);
                yield return new WaitForSeconds(_spawnInterval);
            }
        }
    }

    private void SpawnAttackUnit(CardData card, WaypointPath path, AttackUnitOwner owner, bool reversePath)
    {
        GameObject unitObject = new GameObject($"AttackUnit_{owner}_{card.cardName}");
        if (_attackUnitRoot != null)
        {
            unitObject.transform.SetParent(_attackUnitRoot, true);
        }

        AttackUnit unit = unitObject.AddComponent<AttackUnit>();
        unit.Initialize(card, path, reversePath, ResolveSortingLayerID(), _attackUnitSortingOrder, _unitZ, _attackUnitBaseScale, _attackUnitBottomAnchorYOffset, owner, _onAttackUnitReachedDestination);
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
