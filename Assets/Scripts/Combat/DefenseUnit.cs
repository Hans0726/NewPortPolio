using UnityEngine;

public class DefenseUnit : MonoBehaviour
{
    [SerializeField] private float _attackRange = 2.5f;
    [SerializeField] private float _holdAtRangeEdgeSeconds = 3f;
    [SerializeField] private float _rangeEdgeInset = 0.05f;
    [SerializeField] private float _movementAreaCheckRadius = 0.12f;
    [SerializeField] private float _positionReachDistance = 0.03f;
    [SerializeField] private float _bobAmplitude = 0.035f;
    [SerializeField] private float _bobFrequency = 8f;

    private enum MoveState
    {
        Guarding,
        MovingToRangeEdge,
        HoldingAtRangeEdge,
        ReturningHome
    }

    private CardData _card;
    private SpriteRenderer _renderer;
    private Vector3 _homePosition;
    private AttackUnit _target;
    private MoveState _moveState;
    private Vector3 _rangeEdgeHoldPosition;
    private float _moveSpeed;
    private int _attack;
    private float _attackSpeed;
    private float _nextAttackTime;
    private float _holdUntilTime;
    private bool _isFixedUnit;
    private bool _isMovingThisFrame;
    private Vector3 _visualBaseLocalPosition;

    public void Initialize(CardData card, Vector3 homePosition)
    {
        _card = card;
        _homePosition = homePosition;
        _moveSpeed = Mathf.Max(0.1f, card != null ? card.moveSpeed : 1f);
        _attack = Mathf.Max(1, card != null ? card.attack : 1);
        _attackSpeed = Mathf.Max(0.1f, card != null ? card.attackSpeed : 1f);
        _isFixedUnit = card != null && card.isFixedDefenseUnit;
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _visualBaseLocalPosition = _renderer != null ? _renderer.transform.localPosition : Vector3.zero;
    }

    private void Update()
    {
        _isMovingThisFrame = false;

        AttackUnit escapedTarget = IsValidTarget(_target) ? null : _target;
        AcquireTarget();

        if (_target != null)
        {
            _moveState = MoveState.Guarding;
            FacePosition(_target.HitCenter);
            TryAttack();
        }
        else
        {
            if (escapedTarget != null && !escapedTarget.IsDead && _moveState == MoveState.Guarding)
            {
                BeginRangeEdgeHold(escapedTarget.HitCenter);
            }

            HandleNoTarget();
        }

        AnimateVisual();
    }

    private void AcquireTarget()
    {
        if (IsValidTarget(_target)) return;

        _target = AttackUnitRegistry.FindClosest(_homePosition, _attackRange);
    }

    private bool IsValidTarget(AttackUnit target)
    {
        if (target == null || target.IsDead) return false;

        return GetDistanceToTargetEdge(_homePosition, target) <= _attackRange;
    }

    private void HandleTarget()
    {
        FacePosition(_target.HitCenter);
        TryAttack();
    }

    private void TryAttack()
    {
        if (_target == null || Time.time < _nextAttackTime) return;

        _target.TakeDamage(_attack);
        _nextAttackTime = Time.time + 1f / _attackSpeed;
    }

    private void HandleNoTarget()
    {
        if (_isFixedUnit) return;

        if (_moveState == MoveState.MovingToRangeEdge)
        {
            bool reached = MoveTowards(_rangeEdgeHoldPosition);
            if (reached || !_isMovingThisFrame)
            {
                _moveState = MoveState.HoldingAtRangeEdge;
                _holdUntilTime = Time.time + _holdAtRangeEdgeSeconds;
            }

            return;
        }

        if (_moveState == MoveState.HoldingAtRangeEdge)
        {
            if (Time.time >= _holdUntilTime)
            {
                _moveState = MoveState.ReturningHome;
            }

            return;
        }

        if (_moveState != MoveState.ReturningHome && !IsAtPosition(_homePosition))
        {
            _moveState = MoveState.ReturningHome;
        }

        if (_moveState == MoveState.ReturningHome)
        {
            bool reachedHome = MoveTowards(_homePosition);
            if (reachedHome)
            {
                _moveState = MoveState.Guarding;
            }
        }
    }

    private void BeginRangeEdgeHold(Vector3 escapedTargetPosition)
    {
        if (_isFixedUnit) return;

        Vector3 direction = escapedTargetPosition - _homePosition;
        direction.z = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            _rangeEdgeHoldPosition = _homePosition;
        }
        else
        {
            float rangeEdgeDistance = Mathf.Max(0f, _attackRange - _rangeEdgeInset);
            _rangeEdgeHoldPosition = _homePosition + direction.normalized * rangeEdgeDistance;
            _rangeEdgeHoldPosition.z = transform.position.z;
        }

        _moveState = MoveState.MovingToRangeEdge;
        _holdUntilTime = 0f;
    }

    private bool MoveTowards(Vector3 targetPosition)
    {
        Vector3 previousPosition = transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);
        nextPosition = ClampPositionInsideAttackRange(nextPosition);

        if (CanMoveToPosition(nextPosition))
        {
            transform.position = nextPosition;
        }

        Vector3 movement = transform.position - previousPosition;
        _isMovingThisFrame = movement.sqrMagnitude > 0.000001f;

        if (_renderer != null && Mathf.Abs(movement.x) > 0.001f)
        {
            _renderer.flipX = movement.x < 0f;
        }

        return IsAtPosition(targetPosition);
    }

    private void AnimateVisual()
    {
        if (_renderer == null) return;

        float isMoving = _isMovingThisFrame ? 1f : 0f;
        float offsetY = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude * isMoving;
        _renderer.transform.localPosition = _visualBaseLocalPosition + new Vector3(0f, offsetY, 0f);
    }

    private float GetDistanceToTargetEdge(Vector3 origin, AttackUnit target)
    {
        if (target == null) return float.MaxValue;

        float centerDistance = Vector3.Distance(origin, target.HitCenter);
        return Mathf.Max(0f, centerDistance - target.HitRadius);
    }

    private Vector3 ClampPositionInsideAttackRange(Vector3 position)
    {
        Vector3 homePosition = Application.isPlaying ? _homePosition : transform.position;
        Vector3 homeToPosition = position - homePosition;
        homeToPosition.z = 0f;

        if (homeToPosition.sqrMagnitude <= _attackRange * _attackRange) return position;

        Vector3 clampedPosition = homePosition + homeToPosition.normalized * _attackRange;
        clampedPosition.z = position.z;
        return clampedPosition;
    }

    private bool CanMoveToPosition(Vector3 position)
    {
        DefensePlacementManager placementManager = DefensePlacementManager.Instance;
        if (placementManager == null) return true;

        Vector3 groundPosition = GetGroundPosition(position);
        if (!placementManager.IsWorldPositionPlaceable(groundPosition)) return false;
        if (_movementAreaCheckRadius <= 0f) return true;

        return placementManager.IsWorldPositionPlaceable(groundPosition + Vector3.left * _movementAreaCheckRadius)
            && placementManager.IsWorldPositionPlaceable(groundPosition + Vector3.right * _movementAreaCheckRadius)
            && placementManager.IsWorldPositionPlaceable(groundPosition + Vector3.up * _movementAreaCheckRadius)
            && placementManager.IsWorldPositionPlaceable(groundPosition + Vector3.down * _movementAreaCheckRadius);
    }

    private Vector3 GetGroundPosition(Vector3 unitPosition)
    {
        if (_renderer == null || _renderer.sprite == null) return unitPosition;

        Vector3 groundPosition = unitPosition;
        float visualOffsetY = _visualBaseLocalPosition.y * transform.lossyScale.y;
        float spriteBottomOffsetY = _renderer.sprite.bounds.min.y * _renderer.transform.lossyScale.y;
        groundPosition.y += visualOffsetY + spriteBottomOffsetY;
        return groundPosition;
    }

    private bool IsAtPosition(Vector3 position)
    {
        Vector3 offset = transform.position - position;
        offset.z = 0f;
        return offset.sqrMagnitude <= _positionReachDistance * _positionReachDistance;
    }

    private void FacePosition(Vector3 position)
    {
        if (_renderer == null) return;

        float deltaX = position.x - transform.position.x;
        if (Mathf.Abs(deltaX) > 0.001f)
        {
            _renderer.flipX = deltaX < 0f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? _homePosition : transform.position;
        Gizmos.DrawWireSphere(center, _attackRange);

        if (!Application.isPlaying) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawLine(_homePosition, transform.position);
    }
}
