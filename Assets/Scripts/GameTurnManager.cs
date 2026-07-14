using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTurnManager : MonoBehaviour
{
    public static GameTurnManager Instance { get; private set; }
    public event Action<int> OnCostChanged; // 현재 자원을 전달하는 이벤트    
    [SerializeField] private int _currentCost = 99;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Reset()
    {
        _currentCost = 1;
    }

    public int CurrentCost
    {
        get => _currentCost;
        set
        {
            _currentCost = value;
            OnCostChanged?.Invoke(_currentCost); // 자원이 변경될 때마다 이벤트 발생
        }
    }

    public void TurnStart(S_TurnStart packet)
    {
        packet.turnNumber++;
        packet.turnTime = 0;
        CurrentCost++;
    }

    public void TurnEnd()
    {
        if (InGameCardManager.Instance == null)
        {
            Debug.LogWarning("[GameTurnManager] Cannot start combat round. InGameCardManager is missing.");
            return;
        }

        CombatRoundManager.Instance.StartCombatRound(InGameCardManager.Instance.SelectedAttackCards);
    }

    public void DeductCost(int amount)
    {
        CurrentCost = Mathf.Max(0, CurrentCost - amount);
    }
}
