using System;
using UnityEngine;

public class GameTurnManager : MonoBehaviour
{
    public static GameTurnManager Instance { get; private set; }

    public event Action<int> OnCostChanged;
    public event Action<int> OnRoundChanged;
    public event Action<int, int> OnLifeChanged;

    [Header("Round")]
    [SerializeField] private int _currentRound = 1;
    [SerializeField] private int _maxRound = 10;

    [Header("Cost")]
    [SerializeField] private int _currentCost = 1;
    [SerializeField] private int _startingCost = 1;
    [SerializeField] private int _maxCost = 10;

    [Header("Life")]
    [SerializeField] private int _playerLife = 10;
    [SerializeField] private int _opponentLife = 10;
    [SerializeField] private int _lifeDamagePerAttackUnit = 1;

    [Header("Cards")]
    [SerializeField] private int _cardsToDrawPerRound = 3;

    private bool _isCombatInProgress;
    private bool _isGameEnded;

    public int CurrentRound => _currentRound;
    public int PlayerLife => _playerLife;
    public int OpponentLife => _opponentLife;

    public int CurrentCost
    {
        get => _currentCost;
        set
        {
            _currentCost = Mathf.Clamp(value, 0, _maxCost);
            OnCostChanged?.Invoke(_currentCost);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _currentRound = Mathf.Clamp(_currentRound, 1, _maxRound);
        CurrentCost = Mathf.Clamp(_startingCost, 1, _maxCost);
        OnRoundChanged?.Invoke(_currentRound);
        OnLifeChanged?.Invoke(_playerLife, _opponentLife);
    }

    private void Reset()
    {
        _currentRound = 1;
        _currentCost = 1;
        _startingCost = 1;
        _maxCost = 10;
        _maxRound = 10;
    }

    public void TurnStart(S_TurnStart packet)
    {
        if (packet != null)
        {
            packet.turnNumber = _currentRound;
            packet.turnTime = 0;
        }
    }

    public void TurnEnd()
    {
        if (_isCombatInProgress || _isGameEnded)
        {
            return;
        }

        if (InGameCardManager.Instance == null)
        {
            Debug.LogWarning("[GameTurnManager] Cannot start combat round. InGameCardManager is missing.");
            return;
        }

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.HideHandForCombat();
        }

        _isCombatInProgress = true;
        InGameCardManager.Instance.DiscardCurrentHand();
        CombatRoundManager.Instance.StartCombatRound(
            InGameCardManager.Instance.SelectedAttackCards,
            null,
            ApplyAttackUnitDestinationDamage,
            CompleteCombatRound);
    }

    public void DeductCost(int amount)
    {
        CurrentCost = Mathf.Max(0, CurrentCost - amount);
    }

    private void ApplyAttackUnitDestinationDamage(AttackUnitOwner owner)
    {
        if (_isGameEnded) return;

        if (owner == AttackUnitOwner.Player)
        {
            _opponentLife = Mathf.Max(0, _opponentLife - _lifeDamagePerAttackUnit);
        }
        else
        {
            _playerLife = Mathf.Max(0, _playerLife - _lifeDamagePerAttackUnit);
        }

        OnLifeChanged?.Invoke(_playerLife, _opponentLife);
        Debug.Log($"[GameTurnManager] Life changed. Player: {_playerLife}, Opponent: {_opponentLife}");

        if (_playerLife <= 0 || _opponentLife <= 0)
        {
            DecideGameResult();
        }
    }

    private void CompleteCombatRound()
    {
        _isCombatInProgress = false;
        if (_isGameEnded) return;

        if (_playerLife <= 0 || _opponentLife <= 0 || _currentRound >= _maxRound)
        {
            DecideGameResult();
            return;
        }

        StartNextRound();
    }

    private void StartNextRound()
    {
        _currentRound++;
        CurrentCost = Mathf.Min(_maxCost, CurrentCost + 1);
        OnRoundChanged?.Invoke(_currentRound);

        if (InGameCardManager.Instance != null)
        {
            InGameCardManager.Instance.PrepareNextCycleDeck();
            InGameCardManager.Instance.DrawCards(_cardsToDrawPerRound);
        }

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowHandForNewRound();
        }

        Debug.Log($"[GameTurnManager] Round {_currentRound} started. Cost: {CurrentCost}");
    }

    private void DecideGameResult()
    {
        if (_isGameEnded) return;

        _isGameEnded = true;
        _isCombatInProgress = false;

        if (_playerLife <= 0 && _opponentLife <= 0)
        {
            Debug.Log("[GameTurnManager] Draw. Both players reached 0 life.");
        }
        else if (_opponentLife <= 0 || _playerLife > _opponentLife)
        {
            Debug.Log("[GameTurnManager] Victory.");
        }
        else if (_playerLife <= 0 || _opponentLife > _playerLife)
        {
            Debug.Log("[GameTurnManager] Defeat.");
        }
        else
        {
            Debug.Log("[GameTurnManager] Draw. Life totals are tied after the final round.");
        }
    }
}
