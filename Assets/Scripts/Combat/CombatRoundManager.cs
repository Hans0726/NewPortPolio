using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatRoundManager : MonoBehaviour
{
    private static CombatRoundManager _instance;

    public static CombatRoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<CombatRoundManager>();
            }

            if (_instance == null)
            {
                GameObject managerObject = new GameObject("CombatRoundManager");
                _instance = managerObject.AddComponent<CombatRoundManager>();
            }

            return _instance;
        }
    }

    [Header("Attack Unit Spawn")]
    [SerializeField] private WaypointPath _enemyAttackPath;
    [SerializeField] private Transform _attackUnitRoot;
    [SerializeField] private float _spawnInterval = 0.35f;
    [SerializeField] private float _unitZ = -1f;

    [Header("Render")]
    [SerializeField] private string _unitSortingLayerName = "Layer 1";
    [SerializeField] private int _attackUnitSortingOrder = 80;

    private Coroutine _spawnRoutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    public void StartCombatRound(IReadOnlyList<CardData> attackCards)
    {
        if (attackCards == null || attackCards.Count == 0)
        {
            Debug.Log("[CombatRoundManager] No attack cards selected for this combat round.");
            return;
        }

        EnsureAttackPath();
        if (_enemyAttackPath == null || _enemyAttackPath.Count == 0)
        {
            Debug.LogWarning("[CombatRoundManager] Enemy attack path is missing. Create a WaypointPath object with child points.");
            return;
        }

        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
        }

        _spawnRoutine = StartCoroutine(SpawnAttackUnits(attackCards));
    }

    private IEnumerator SpawnAttackUnits(IReadOnlyList<CardData> attackCards)
    {
        for (int i = 0; i < attackCards.Count; i++)
        {
            CardData card = attackCards[i];
            if (card != null && card.cardType == CardType.Attack)
            {
                SpawnAttackUnit(card);
                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        _spawnRoutine = null;
    }

    private void SpawnAttackUnit(CardData card)
    {
        GameObject unitObject = new GameObject($"AttackUnit_{card.cardName}");
        if (_attackUnitRoot != null)
        {
            unitObject.transform.SetParent(_attackUnitRoot, true);
        }

        AttackUnit unit = unitObject.AddComponent<AttackUnit>();
        unit.Initialize(card, _enemyAttackPath, ResolveSortingLayerID(), _attackUnitSortingOrder, _unitZ);
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
