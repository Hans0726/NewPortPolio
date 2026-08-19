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

    // 한쪽 경로 위치를 같은 경로의 반대 진행 방향 위치로 변환한다.
    public Vector3 GetOppositePathPosition(Vector3 sourcePosition)
    {
        RefreshChildWaypointsIfNeeded();
        if (_waypoints.Count < 2) return sourcePosition;

        float totalLength = 0f;
        float closestDistanceAlongPath = 0f;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            Vector3 start = _waypoints[i].position;
            Vector3 end = _waypoints[i + 1].position;
            Vector3 segment = end - start;
            float segmentLength = segment.magnitude;
            if (segmentLength <= 0.0001f) continue;

            float t = Mathf.Clamp01(Vector3.Dot(sourcePosition - start, segment) /
                                    segment.sqrMagnitude);
            Vector3 closest = start + segment * t;
            float sqrDistance = (sourcePosition - closest).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestDistanceAlongPath = totalLength + segmentLength * t;
            }

            totalLength += segmentLength;
        }

        return GetPositionAtDistance(Mathf.Max(0f, totalLength - closestDistanceAlongPath));
    }

    private Vector3 GetPositionAtDistance(float distance)
    {
        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            Vector3 start = _waypoints[i].position;
            Vector3 end = _waypoints[i + 1].position;
            float segmentLength = Vector3.Distance(start, end);
            if (distance <= segmentLength)
            {
                return Vector3.Lerp(start, end, segmentLength > 0f ? distance / segmentLength : 0f);
            }

            distance -= segmentLength;
        }

        return _waypoints[_waypoints.Count - 1].position;
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
