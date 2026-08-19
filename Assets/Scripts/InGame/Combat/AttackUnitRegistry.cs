using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class AttackUnitRegistry
{
    private static readonly List<AttackUnit> _activeUnits = new List<AttackUnit>();

    public static IReadOnlyList<AttackUnit> ActiveUnits => _activeUnits;

    public static int ActiveCount
    {
        get
        {
            RemoveInactiveUnits();
            return _activeUnits.Count;
        }
    }

    public static void Register(AttackUnit unit)
    {
        if (unit == null || _activeUnits.Contains(unit)) return;
        _activeUnits.Add(unit);
    }

    public static void Unregister(AttackUnit unit)
    {
        if (unit == null) return;
        _activeUnits.Remove(unit);
    }

    public static AttackUnit FindClosest(
        Vector3 origin,
        float range,
        AttackUnitOwner targetOwner)
    {
        RemoveInactiveUnits();
        AttackUnit closest = null;
        float closestEdgeDistance = range;

        for (int i = _activeUnits.Count - 1; i >= 0; i--)
        {
            AttackUnit unit = _activeUnits[i];
            if (unit == null || unit.IsDead)
            {
                _activeUnits.RemoveAt(i);
                continue;
            }

            if (unit.Owner != targetOwner) continue;

            float centerDistance = Vector3.Distance(unit.HitCenter, origin);
            float edgeDistance = Mathf.Max(0f, centerDistance - unit.HitRadius);
            if (edgeDistance <= closestEdgeDistance)
            {
                closest = unit;
                closestEdgeDistance = edgeDistance;
            }
        }

        return closest;
    }

    public static AttackUnit FindByNetworkId(int networkUnitId, AttackUnitOwner owner)
    {
        RemoveInactiveUnits();
        return _activeUnits.Find(unit =>
            unit != null &&
            !unit.IsDead &&
            unit.NetworkUnitId == networkUnitId &&
            unit.Owner == owner);
    }

    private static void RemoveInactiveUnits()
    {
        for (int i = _activeUnits.Count - 1; i >= 0; i--)
        {
            AttackUnit unit = _activeUnits[i];
            if (unit == null || unit.IsDead)
            {
                _activeUnits.RemoveAt(i);
            }
        }
    }

    public static void InActivateAttackUnits()
    {
        foreach (var unit in _activeUnits)
        {
            if (unit != null)
            {
                unit.ShouldStop = true;
            }
        }
    }
}
