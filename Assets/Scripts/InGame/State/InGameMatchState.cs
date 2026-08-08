using System;
using UnityEngine;

public enum InGamePhase
{
    Opening,
    Preparation,
    Combat,
    Result
}

public enum InGameResult
{
    None,
    Victory,
    Defeat,
    Draw
}

public sealed class InGameMatchState
{
    public event Action<int> CostChanged;
    public event Action<int> RoundChanged;
    public event Action<int, int> LifeChanged;
    public event Action<InGamePhase> PhaseChanged;
    public event Action<InGameResult> ResultChanged;

    private readonly int _startingCost;
    private readonly int _maxCost;
    private readonly int _maxRound;
    private readonly int _lifeDamagePerAttackUnit;

    public int CurrentRound { get; private set; }
    public int CurrentCost { get; private set; }
    public int PlayerLife { get; private set; }
    public int OpponentLife { get; private set; }
    public int PlayerMaxLife { get; }
    public int OpponentMaxLife { get; }
    public int MaxRound => _maxRound;
    public InGamePhase Phase { get; private set; } = InGamePhase.Opening;
    public InGameResult Result { get; private set; } = InGameResult.None;
    public bool IsGameEnded => Result != InGameResult.None;

    public InGameMatchState(
        int startingRound,
        int maxRound,
        int startingCost,
        int maxCost,
        int playerLife,
        int opponentLife,
        int lifeDamagePerAttackUnit)
    {
        _maxRound = Mathf.Max(1, maxRound);
        _startingCost = Mathf.Max(0, startingCost);
        _maxCost = Mathf.Max(_startingCost, maxCost);
        _lifeDamagePerAttackUnit = Mathf.Max(1, lifeDamagePerAttackUnit);

        CurrentRound = Mathf.Clamp(startingRound, 1, _maxRound);
        CurrentCost = Mathf.Min(_maxCost, _startingCost + CurrentRound - 1);
        PlayerLife = Mathf.Max(1, playerLife);
        OpponentLife = Mathf.Max(1, opponentLife);
        PlayerMaxLife = PlayerLife;
        OpponentMaxLife = OpponentLife;
    }

    public bool ApplyRound(int round)
    {
        int nextRound = Mathf.Clamp(round, 1, _maxRound);
        bool changed = nextRound != CurrentRound;
        CurrentRound = nextRound;
        CurrentCost = Mathf.Min(_maxCost, _startingCost + CurrentRound - 1);

        if (changed)
        {
            RoundChanged?.Invoke(CurrentRound);
        }

        CostChanged?.Invoke(CurrentCost);
        return changed;
    }

    public bool TrySpendCost(int amount)
    {
        if (amount < 0 || CurrentCost < amount) return false;

        CurrentCost -= amount;
        CostChanged?.Invoke(CurrentCost);
        return true;
    }

    public void SetPhase(InGamePhase phase)
    {
        if (Phase == phase) return;

        Phase = phase;
        PhaseChanged?.Invoke(Phase);
    }

    public void ApplyDestinationDamage(AttackUnitOwner attackOwner)
    {
        if (IsGameEnded) return;

        if (attackOwner == AttackUnitOwner.Player)
        {
            OpponentLife = Mathf.Max(0, OpponentLife - _lifeDamagePerAttackUnit);
        }
        else
        {
            PlayerLife = Mathf.Max(0, PlayerLife - _lifeDamagePerAttackUnit);
        }

        LifeChanged?.Invoke(PlayerLife, OpponentLife);
    }

    public InGameResult DecideResult()
    {
        if (Result != InGameResult.None) return Result;

        if (PlayerLife <= 0 && OpponentLife <= 0)
        {
            Result = InGameResult.Draw;
        }
        else if (OpponentLife <= 0 || PlayerLife > OpponentLife)
        {
            Result = InGameResult.Victory;
        }
        else if (PlayerLife <= 0 || OpponentLife > PlayerLife)
        {
            Result = InGameResult.Defeat;
        }
        else
        {
            Result = InGameResult.Draw;
        }

        SetPhase(InGamePhase.Result);
        ResultChanged?.Invoke(Result);
        return Result;
    }
}
