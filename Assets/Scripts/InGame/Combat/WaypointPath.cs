using System.Collections.Generic;
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [SerializeField] private List<Transform> _waypoints = new List<Transform>();
    [SerializeField] private bool _useChildrenAsWaypoints = true;

    public int Count
    {
        get
        {
            RefreshChildWaypointsIfNeeded();
            return _waypoints.Count;
        }
    }

    public bool IsArrivedAtEntrance(int index)
    {
        if (_waypoints[index].gameObject.name.Contains("Entrance")) return true;
        else return false;
    }

    public bool IsArrivedAtExit(int index)
    {
        if (_waypoints[index].gameObject.name.Contains("Exit")) return true;
        else return false;
    }

    public Vector3 GetWaypointPosition(int index)
    {
        RefreshChildWaypointsIfNeeded();
        return _waypoints[Mathf.Clamp(index, 0, _waypoints.Count - 1)].position;
    }

    private void OnValidate()
    {
        RefreshChildWaypointsIfNeeded();
    }

    private void RefreshChildWaypointsIfNeeded()
    {
        if (!_useChildrenAsWaypoints) return;

        _waypoints.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            _waypoints.Add(transform.GetChild(i));
        }
    }

    private void OnDrawGizmos()
    {
        RefreshChildWaypointsIfNeeded();
        if (_waypoints.Count == 0) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < _waypoints.Count; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint == null) continue;

            Gizmos.DrawSphere(waypoint.position, 0.25f);
            if (i + 1 < _waypoints.Count && _waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoint.position, _waypoints[i + 1].position);
            }
        }
    }
}
