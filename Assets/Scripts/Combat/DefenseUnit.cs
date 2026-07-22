using UnityEngine;

public class DefenseUnit : MonoBehaviour
{
    [SerializeField] private float _attackRange = 2.5f;
    [SerializeField] private float _holdAtRangeEdgeSeconds = 3f;
    [SerializeField] private float _movementAreaCheckRadius = 0.2f;
    [SerializeField] private float _positionReachDistance = 0.03f;
    [SerializeField] private float _bobAmplitude = 0.035f;
    [SerializeField] private float _bobFrequency = 8f;


    private enum MoveState
    {
        Guarding,
        ChasingTarget,
        HoldingLastKnownPosition,
        ReturningHome
    }

    private CardData _card;
    private SpriteRenderer _renderer;
    private Vector3 _homePosition;
    private AttackUnit _target;
    [SerializeField] private MoveState _moveState;
    private Vector3 _lastKnownTargetPosition;
    private float _moveSpeed;
    private int _attack;
    private float _attackSpeed;
    private float _nextAttackTime;
    private float _holdUntilTime;
    private float _blockedChaseStartTime;
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

        if (_target == null || _target.IsDead)
        {
            if (_target != null)
            {
                BeginLastKnownPositionHold();
                _target = null;
            }

            AcquireTarget();
        }

        if (_target != null && !_target.IsDead)
        {
            HandleTrackedTarget();
        }
        else
        {
            HandleNoTarget();
        }

        AnimateVisual();
    }

    private void AcquireTarget()
    {
        if (_target != null && !_target.IsDead) return;

        _target = AttackUnitRegistry.FindClosest(transform.position, _attackRange);
        if (_target == null) return;

        _moveState = MoveState.Guarding;
        _lastKnownTargetPosition = _target.HitCenter;
        _blockedChaseStartTime = 0f;
    }

    private bool CanAttackTarget(AttackUnit target)
    {
        if (target == null || target.IsDead) return false;

        return GetDistanceToTargetEdge(transform.position, target) <= _attackRange;
    }

    private void HandleTrackedTarget()
    {
        _lastKnownTargetPosition = _target.HitCenter;
        FacePosition(_lastKnownTargetPosition);

        if (CanAttackTarget(_target))
        {
            _moveState = MoveState.Guarding;
            _blockedChaseStartTime = 0f;
            TryAttack();
            return;
        }

        if (_isFixedUnit)
        {
            _target = null;
            return;
        }

        _moveState = MoveState.ChasingTarget;
        MoveTowards(_lastKnownTargetPosition);

        if (_isMovingThisFrame)
        {
            _blockedChaseStartTime = 0f;
        }
        else if (_blockedChaseStartTime <= 0f)
        {
            _blockedChaseStartTime = Time.time;
        }
        else if (Time.time >= _blockedChaseStartTime + _holdAtRangeEdgeSeconds)
        {
            _target = null;
            BeginLastKnownPositionHold();
        }
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

        if (_moveState == MoveState.HoldingLastKnownPosition)
        {
            if (Time.time >= _holdUntilTime)
            {
                _moveState = MoveState.ReturningHome;
            }

            return;
        }

        if (_moveState == MoveState.ChasingTarget)
        {
            bool reached = MoveTowards(_lastKnownTargetPosition);
            if (reached || !_isMovingThisFrame)
            {
                _moveState = MoveState.HoldingLastKnownPosition;
                _holdUntilTime = Time.time + _holdAtRangeEdgeSeconds;
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

    private void BeginLastKnownPositionHold()
    {
        if (_isFixedUnit) return;

        _lastKnownTargetPosition.z = transform.position.z;
        _moveState = MoveState.ChasingTarget;
        _holdUntilTime = 0f;
        _blockedChaseStartTime = 0f;
    }

    private bool MoveTowards(Vector3 targetPosition)
    {
        Vector3 previousPosition = transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

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
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        if (!Application.isPlaying) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawLine(_homePosition, transform.position);
    }
}
