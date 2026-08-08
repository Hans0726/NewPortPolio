using UnityEngine;

public class DefenseUnit : MonoBehaviour
{
    [SerializeField] private float _attackRange = 2.5f;
    [SerializeField, Min(0f)] private float _maxChaseDuration = 2f;
    [SerializeField] private float _movementAreaCheckRadius = 0.2f;
    [SerializeField] private float _positionReachDistance = 0.03f;
    [SerializeField] private float _bobAmplitude = 0.035f;
    [SerializeField] private float _bobFrequency = 8f;
    [SerializeField, Min(0f)] private float _pursuitPredictionMultiplier = 1.5f;
    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogs = true;

    private enum MoveState
    {
        Guarding,
        ChasingTarget,
        ReturningHome
    }

    private CardData _card;
    private SpriteRenderer _renderer;
    private Vector3 _homePosition;
    private Vector3 _groundOffsetFromTransform;
    private AttackUnit _target;
    [SerializeField] private MoveState _moveState;
    private Vector3 _lastKnownTargetPosition;
    private float _moveSpeed;
    private int _attack;
    private float _attackSpeed;
    private float _nextAttackTime;
    private float _chaseElapsedTime;
    private bool _isFixedUnit;
    private bool _isMovingThisFrame;
    private Vector3 _visualBaseLocalPosition;
    private AttackUnitOwner _targetOwner = AttackUnitOwner.Opponent;
    private DefensePlacementManager _placementService;
    private bool _combatActive;

    public void Initialize(
        CardData card,
        Vector3 homePosition,
        Vector3 homeGroundPosition,
        AttackUnitOwner targetOwner = AttackUnitOwner.Opponent,
        DefensePlacementManager placementService = null)
    {
        _card = card;
        _homePosition = homePosition;
        _groundOffsetFromTransform = homeGroundPosition - homePosition;
        _targetOwner = targetOwner;
        _placementService = placementService;
        _moveSpeed = Mathf.Max(0.1f, card != null ? card.moveSpeed : 1f);
        _attack = Mathf.Max(1, card != null ? card.attack : 1);
        _attackSpeed = Mathf.Max(0.1f, card != null ? card.attackSpeed : 1f);
        _isFixedUnit = card != null && card.isFixedDefenseUnit;
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _visualBaseLocalPosition = _renderer != null ? _renderer.transform.localPosition : Vector3.zero;

        LogDebug($"Initialized | State={_moveState}");
    }

    private void Update()
    {
        _isMovingThisFrame = false;

        if (!IsCombatActive())
        {
            HandleOutOfCombat();
            AnimateVisual();
            return;
        }

        RefreshTarget();

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

    private bool IsCombatActive()
    {
        return _combatActive;
    }

    public void SetCombatActive(bool active)
    {
        _combatActive = active;
        if (!active)
        {
            HandleOutOfCombat();
        }
    }

    private void HandleOutOfCombat()
    {
        ClearTarget("Combat ended");
        transform.position = _homePosition;
        ChangeMoveState(MoveState.Guarding, "Combat ended");

        if (_renderer != null)
        {
            _renderer.transform.localPosition = _visualBaseLocalPosition;
        }
    }

    private void RefreshTarget()
    {
        AttackUnit attackableTarget = AttackUnitRegistry.FindClosest(
            transform.position,
            _attackRange,
            _targetOwner);

        if (attackableTarget != null && (_target == null || _target.IsDead || !CanAttackTarget(_target)))
        {
            SetTarget(attackableTarget);
            return;
        }

        if (_target == null || _target.IsDead)
        {
            ClearTarget("Target missing or dead");
        }
    }

    private void SetTarget(AttackUnit target)
    {
        if (target == null) return;

        AttackUnit previousTarget = _target;
        _target = target;

        if (previousTarget != _target)
        {
            _chaseElapsedTime = 0f;
            LogDebug($"Target acquired | {GetAttackUnitName(previousTarget)} -> {GetAttackUnitName(_target)}");
        }

        ChangeMoveState(MoveState.Guarding, "Target acquired");
        _lastKnownTargetPosition = _target.HitCenter;
    }

    private void ClearTarget(string reason)
    {
        if (_target == null)
        {
            _chaseElapsedTime = 0f;
            return;
        }

        string targetName = GetAttackUnitName(_target);
        _target = null;
        _chaseElapsedTime = 0f;
        LogDebug($"Target released | Target={targetName} | Reason={reason}");
    }

    private bool CanAttackTarget(AttackUnit target)
    {
        if (target == null || target.IsDead) return false;

        return GetDistanceToTargetEdge(transform.position, target) <= _attackRange;
    }

    private void HandleTrackedTarget()
    {
        FacePosition(_target.HitCenter);

        if (CanAttackTarget(_target))
        {
            _chaseElapsedTime = 0f;
            ChangeMoveState(MoveState.Guarding, "Target entered attack range");
            TryAttack();
            return;
        }

        if (IsRecoveringFromAttack())
        {
            ChangeMoveState(MoveState.Guarding, "Waiting for attack cooldown");
            return;
        }

        if (_isFixedUnit)
        {
            ClearTarget("Fixed unit cannot chase");
            return;
        }

        float predictionTime = GetAttackCooldown() * _pursuitPredictionMultiplier;
        _lastKnownTargetPosition = _target.GetPredictedHitCenter(predictionTime);
        ChaseTarget();
    }

    private void TryAttack()
    {
        if (_target == null || Time.time < _nextAttackTime || _target.IsHiding) return;

        string targetName = GetAttackUnitName(_target);
        _target.TakeDamage(_attack);
        _nextAttackTime = Time.time + GetAttackCooldown();
        LogDebug($"Attack | Target={targetName} | Damage={_attack} | Cooldown={GetAttackCooldown():0.00}s");
    }

    private bool IsRecoveringFromAttack()
    {
        return Time.time < _nextAttackTime;
    }

    private float GetAttackCooldown()
    {
        return 1f / _attackSpeed;
    }

    private void HandleNoTarget()
    {
        if (_isFixedUnit) return;

        if (IsRecoveringFromAttack())
        {
            ChangeMoveState(MoveState.Guarding, "Waiting for attack cooldown");
            return;
        }

        if (_moveState != MoveState.ReturningHome && !IsAtPosition(_homePosition))
        {
            ChangeMoveState(MoveState.ReturningHome, "No target");
        }

        if (_moveState == MoveState.ReturningHome)
        {
            bool reachedHome = MoveTowards(_homePosition);
            if (reachedHome)
            {
                ChangeMoveState(MoveState.Guarding, "Reached home");
            }
        }
    }

    private void ChaseTarget()
    {
        ChangeMoveState(MoveState.ChasingTarget, "Target outside attack range");
        _chaseElapsedTime += Time.deltaTime;

        MoveTowards(_lastKnownTargetPosition);

        // 이동 중 사거리 안으로 들어오면 먼저 멈추고 다음 프레임부터 공격한다.
        if (_target != null && CanAttackTarget(_target))
        {
            _chaseElapsedTime = 0f;
            ChangeMoveState(MoveState.Guarding, "Stopped inside attack range");
            FacePosition(_target.HitCenter);
            return;
        }

        if (_chaseElapsedTime >= _maxChaseDuration)
        {
            ClearTarget($"Could not enter attack range within {_maxChaseDuration:0.00}s");
        }
    }

    private void ChangeMoveState(MoveState nextState, string reason)
    {
        if (_moveState == nextState) return;

        _moveState = nextState;
    }

    private void LogDebug(string message)
    {
        if (!_enableDebugLogs) return;

        Debug.Log($"[DefenseUnit:{GetDefenseUnitName()}] {message}", this);
    }

    private string GetDefenseUnitName()
    {
        return _card != null && !string.IsNullOrEmpty(_card.cardName)
            ? _card.cardName
            : name;
    }

    private static string GetAttackUnitName(AttackUnit target)
    {
        return target != null ? target.name : "None";
    }

    private bool MoveTowards(Vector3 targetPosition)
    {
        Vector3 previousPosition = transform.position;
        float step = _moveSpeed * Time.deltaTime;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, step);

        if (!TryMoveTo(nextPosition))
        {
            Vector3 xOnlyTarget = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
            Vector3 xOnlyPosition = Vector3.MoveTowards(transform.position, xOnlyTarget, step);
            if (!TryMoveTo(xOnlyPosition))
            {
                Vector3 yOnlyTarget = new Vector3(transform.position.x, targetPosition.y, transform.position.z);
                Vector3 yOnlyPosition = Vector3.MoveTowards(transform.position, yOnlyTarget, step);
                TryMoveTo(yOnlyPosition);
            }
        }

        Vector3 movement = transform.position - previousPosition;
        _isMovingThisFrame = movement.sqrMagnitude > 0.000001f;

        if (_renderer != null && Mathf.Abs(movement.x) > 0.001f)
        {
            _renderer.flipX = movement.x > 0f;
        }

        return IsAtPosition(targetPosition);
    }

    private bool TryMoveTo(Vector3 position)
    {
        if (!CanMoveToPosition(position))
            return false;

        transform.position = position;
        return true;
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
        if (_placementService == null) return true;

        Vector3 groundPosition = GetGroundPosition(position);
        if (!_placementService.IsWorldPositionPlaceable(groundPosition)) return false;
        if (_movementAreaCheckRadius <= 0f) return true;

        return _placementService.IsWorldPositionPlaceable(groundPosition + Vector3.left * _movementAreaCheckRadius)
            && _placementService.IsWorldPositionPlaceable(groundPosition + Vector3.right * _movementAreaCheckRadius);
    }

    private Vector3 GetGroundPosition(Vector3 unitPosition)
    {
        return unitPosition + _groundOffsetFromTransform;
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
            _renderer.flipX = deltaX > 0f;
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
