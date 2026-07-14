using System.Collections.Generic;
using UnityEngine;

public static class AttackUnitRegistry
{
    private static readonly List<AttackUnit> _activeUnits = new List<AttackUnit>();

    public static IReadOnlyList<AttackUnit> ActiveUnits => _activeUnits;

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

    public static AttackUnit FindClosest(Vector3 origin, float range)
    {
        AttackUnit closest = null;
        float closestSqrDistance = range * range;

        for (int i = _activeUnits.Count - 1; i >= 0; i--)
        {
            AttackUnit unit = _activeUnits[i];
            if (unit == null || unit.IsDead)
            {
                _activeUnits.RemoveAt(i);
                continue;
            }

            float sqrDistance = (unit.transform.position - origin).sqrMagnitude;
            if (sqrDistance <= closestSqrDistance)
            {
                closest = unit;
                closestSqrDistance = sqrDistance;
            }
        }

        return closest;
    }
}
