using System;
using UnityEngine;

public enum AttackUnitOwner
{
    Player,
    Opponent
}

public enum CombatUnitType
{
    Attack = 0,
    Defense = 1
}

public class AttackUnit : CombatUnit
{
    [SerializeField] private float _waypointReachDistance = 0.05f;
    [SerializeField] private float _bobAmplitude = 0.05f;
    [SerializeField] private float _bobFrequency = 8f;
    [SerializeField] private Vector2 _healthBarSize = new Vector2(1.1f, 0.1f);
    [SerializeField] private float _healthBarTopPadding = 0.18f;
    [SerializeField] private int _healthBarSortingOrderOffset = 20;
    [SerializeField] private Color _opponentTint = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color _playerHealthBarColor = new Color(0.2f, 0.9f, 0.25f, 0.95f);
    [SerializeField] private Color _opponentHealthBarColor = new Color(0.95f, 0.2f, 0.2f, 0.95f);
    [SerializeField] private float _networkSendInterval = 0.1f;
    [SerializeField] private float _networkInterpolationSpeed = 18f;

    private WaypointPath _path;
    private int _nextWaypointIndex;
    private int _waypointStep = 1;
    private float _moveSpeed;
    private int _maxHealth;
    private int _currentHealth;
    private int _defense;
    private AttackUnitOwner _owner;
    private Action<AttackUnitOwner> _onReachedDestination;
    private float _unitZ;
    private float _hitRadius = 5f;
    private bool _isHiding = false;
    private WorldHealthBar _healthBar;
    private int _networkUnitId;
    private Action<int> _onOwnedUnitDestroyed;
    private Action<int, int> _onOwnedHealthChanged;
    private Action<CombatUnitType, int, Vector3, bool, bool> _onOwnedMovementChanged;
    private Action<int> _onOwnedReachedDestination;
    private Vector3 _networkTargetPosition;
    private bool _hasNetworkPosition;
    private float _nextNetworkSendTime;
    private Vector3 _networkVelocity;
    private float _lastNetworkReceiveTime;

    public bool IsDead => _currentHealth <= 0;
    public bool ShouldStop { get; set; } = false;
    public bool IsHiding => _isHiding;
    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public int Defense => _defense;
    public float HitRadius => _hitRadius;
    public Vector3 HitCenter => _renderer != null ? _renderer.bounds.center : transform.position;
    public AttackUnitOwner Owner => _owner;
    public int NetworkUnitId => _networkUnitId;

    public Vector3 GetPredictedHitCenter(float secondsAhead)
    {
        if (_owner == AttackUnitOwner.Opponent && !GameConfig.ENABLE_TEST_MODE)
        {
            return HitCenter + _networkVelocity * secondsAhead;
        }

        if (_path == null || _path.Count == 0 || secondsAhead <= 0f)
        {
            return HitCenter;
        }

        Vector3 predictedPosition = transform.position;
        float remainingDistance = _moveSpeed * secondsAhead;
        int waypointIndex = _nextWaypointIndex;

        while (remainingDistance > 0f && IsWaypointIndexInRange(waypointIndex))
        {
            Vector3 waypointPosition = _path.GetWaypointPosition(waypointIndex);
            waypointPosition.z = _unitZ;

            float distanceToWaypoint = Vector3.Distance(predictedPosition, waypointPosition);
            if (distanceToWaypoint > remainingDistance)
            {
                predictedPosition = Vector3.MoveTowards(
                    predictedPosition,
                    waypointPosition,
                    remainingDistance);
                break;
            }

            predictedPosition = waypointPosition;
            remainingDistance -= distanceToWaypoint;
            waypointIndex += _waypointStep;
        }

        return HitCenter + (predictedPosition - transform.position);
    }

    public void Initialize(CardData card, WaypointPath path, bool reversePath, int sortingLayerId, int sortingOrder, float unitZ, float baseScale, float bottomAnchorYOffset, AttackUnitOwner owner, int networkUnitId, Action<AttackUnitOwner> onReachedDestination, Action<int> onOwnedUnitDestroyed, Action<int, int> onOwnedHealthChanged, Action<CombatUnitType, int, Vector3, bool, bool> onOwnedMovementChanged, Action<int> onOwnedReachedDestination)
    {
        _card = card;
        _path = path;
        _owner = owner;
        _networkUnitId = networkUnitId;
        _onReachedDestination = onReachedDestination;
        _onOwnedUnitDestroyed = onOwnedUnitDestroyed;
        _onOwnedHealthChanged = onOwnedHealthChanged;
        _onOwnedMovementChanged = onOwnedMovementChanged;
        _onOwnedReachedDestination = onOwnedReachedDestination;
        _unitZ = unitZ;
        _waypointStep = reversePath ? -1 : 1;
        _moveSpeed = Mathf.Max(0.1f, card != null ? card.moveSpeed : 1f);
        _maxHealth = Mathf.Max(1, card != null ? card.health : 1);
        _currentHealth = _maxHealth;
        _defense = Mathf.Max(0, card != null ? card.defense : 0);

        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(transform, false);

        _renderer = visualObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = CombatSpriteUtility.GetCardSprite(card);
        _renderer.sortingLayerID = sortingLayerId;
        _renderer.sortingOrder = sortingOrder;
        InitializeFieldVisual(card, _renderer, baseScale, bottomAnchorYOffset);
        _hitRadius = GetConfiguredHitRadius();
        if (_owner == AttackUnitOwner.Opponent)
        {
            _renderer.color = _opponentTint;
        }
        if (_path != null && _path.Count > 0)
        {
            int startWaypointIndex = reversePath ? _path.Count - 1 : 0;
            Vector3 startPosition = _path.GetWaypointPosition(startWaypointIndex);
            startPosition.z = _unitZ;
            transform.position = startPosition;
            _networkTargetPosition = startPosition;
            _nextWaypointIndex = startWaypointIndex + _waypointStep;
        }

        CreateHealthBar(sortingLayerId, sortingOrder);

        AttackUnitRegistry.Register(this);
    }

    private void OnDestroy()
    {
        AttackUnitRegistry.Unregister(this);
    }

    private void Update()
    {
#if UNITY_EDITOR
        RefreshFieldVisualTuning();
        RefreshHitRadiusTuning();
#endif
        if (IsDead || ShouldStop) return;

        if (_owner == AttackUnitOwner.Opponent && !GameConfig.ENABLE_TEST_MODE)
        {
            UpdateRemoteMovement();
            AnimateVisual();
            return;
        }

        MoveAlongPath();
        SendMovementIfNeeded();
        AnimateVisual();
    }

    public void ApplyAuthoritativeMovement(Vector3 position, bool flipX, bool isHiding)
    {
        if (_owner != AttackUnitOwner.Opponent || GameConfig.ENABLE_TEST_MODE) return;

        if (_path != null && _path.Count > 1)
        {
            position = _path.GetOppositePathPosition(position);
        }
        position.z = _unitZ;
        float elapsed = Time.time - _lastNetworkReceiveTime;
        if (_hasNetworkPosition && elapsed > 0.001f)
        {
            _networkVelocity = (position - _networkTargetPosition) / elapsed;
        }
        _lastNetworkReceiveTime = Time.time;
        _networkTargetPosition = position;
        if (!_hasNetworkPosition)
        {
            transform.position = position;
            _hasNetworkPosition = true;
        }

        if (_renderer != null)
        {
            _renderer.flipX = flipX;
        }
        if (_isHiding != isHiding)
        {
            _isHiding = isHiding;
            SetAlpha(_isHiding ? 0.3f : 1f);
        }
    }

    private void UpdateRemoteMovement()
    {
        if (!_hasNetworkPosition) return;

        float blend = 1f - Mathf.Exp(-_networkInterpolationSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, _networkTargetPosition, blend);
    }

    private void SendMovementIfNeeded()
    {
        if (_owner != AttackUnitOwner.Player || GameConfig.ENABLE_TEST_MODE ||
            Time.time < _nextNetworkSendTime)
        {
            return;
        }

        _nextNetworkSendTime = Time.time + _networkSendInterval;
        _onOwnedMovementChanged?.Invoke(
            CombatUnitType.Attack,
            _networkUnitId,
            transform.position,
            _renderer != null && _renderer.flipX,
            _isHiding);
    }

    public void TakeDamage(int rawDamage)
    {
        // 네트워크 게임에서 상대 유닛의 HP는 그 유닛 소유 클라이언트가 보낸 값만 적용한다.
        if (_owner == AttackUnitOwner.Opponent && !GameConfig.ENABLE_TEST_MODE)
        {
            return;
        }

        int actualDamage = Mathf.Max(1, rawDamage - _defense);
        _currentHealth -= actualDamage;
        _healthBar?.SetValue(_currentHealth, _maxHealth);

        if (_owner == AttackUnitOwner.Player && !GameConfig.ENABLE_TEST_MODE)
        {
            _onOwnedHealthChanged?.Invoke(_networkUnitId, Mathf.Max(0, _currentHealth));
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyAuthoritativeHealth(int currentHealth)
    {
        if (_owner != AttackUnitOwner.Opponent || GameConfig.ENABLE_TEST_MODE) return;

        _currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
        _healthBar?.SetValue(_currentHealth, _maxHealth);
        if (_currentHealth <= 0)
        {
            ApplyAuthoritativeDestroy();
        }
    }

    private void MoveAlongPath()
    {
        if (_path == null || _path.Count == 0) return;

        if (!IsWaypointIndexInRange(_nextWaypointIndex))
        {
            ReachDestination();
            return;
        }

        Vector3 target = _path.GetWaypointPosition(_nextWaypointIndex);
        target.z = _unitZ;

        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target, _moveSpeed * Time.deltaTime);

        Vector3 movement = transform.position - previousPosition;

        if (_renderer != null && Mathf.Abs(movement.x) > 0.001f)
        {
            _renderer.flipX = movement.x > 0f;
        }

        if ((transform.position - target).sqrMagnitude <= _waypointReachDistance * _waypointReachDistance)
        {
            UpdateTunnelVisibility(_nextWaypointIndex);
            _nextWaypointIndex += _waypointStep;
        }
    }

    private bool IsWaypointIndexInRange(int waypointIndex)
    {
        return waypointIndex >= 0 && waypointIndex < _path.Count;
    }

    private void UpdateTunnelVisibility(int waypointIndex)
    {
        bool enteredTunnel = _waypointStep > 0
            ? _path.IsArrivedAtEntrance(waypointIndex)
            : _path.IsArrivedAtExit(waypointIndex);
        bool exitedTunnel = _waypointStep > 0
            ? _path.IsArrivedAtExit(waypointIndex)
            : _path.IsArrivedAtEntrance(waypointIndex);

        if (enteredTunnel)
        {
            _isHiding = true;
            SetAlpha(0.3f);
        }

        if (exitedTunnel)
        {
            _isHiding = false;
            SetAlpha(1f);
        }
    }

    private void AnimateVisual()
    {
        if (_renderer == null) return;

        float offsetY = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
        SetVisualBobOffset(offsetY);
    }

    private void CreateHealthBar(int sortingLayerId, int sortingOrder)
    {
        Vector3 offset = new Vector3(0f, GetHealthBarHeightOffset(), 0f);
        Color fillColor = _owner == AttackUnitOwner.Opponent
            ? _opponentHealthBarColor
            : _playerHealthBarColor;
        _healthBar = WorldHealthBar.Create(
            transform,
            offset,
            _healthBarSize,
            fillColor,
            sortingLayerId,
            sortingOrder + _healthBarSortingOrderOffset,
            keepInsideCamera: true);
        _healthBar.SetValue(_currentHealth, _maxHealth);
    }

    protected override void OnFieldVisualScaleChanged()
    {
        _hitRadius = GetConfiguredHitRadius();
        _healthBar?.SetLayout(
            new Vector3(0f, GetHealthBarHeightOffset(), 0f),
            _healthBarSize);
    }

    private float GetConfiguredHitRadius()
    {
        float radius = _card != null ? _card.fieldHitRadius : 0.25f;
        return Mathf.Max(0f, radius * _fieldSpriteScale);
    }

#if UNITY_EDITOR
    private void RefreshHitRadiusTuning()
    {
        float nextHitRadius = GetConfiguredHitRadius();
        if (Mathf.Approximately(nextHitRadius, _hitRadius)) return;

        _hitRadius = nextHitRadius;
    }
#endif

    private float GetHealthBarHeightOffset()
    {
        if (_renderer == null || _renderer.sprite == null)
        {
            return 1f + _healthBarTopPadding;
        }

        return _renderer.sprite.bounds.size.y * _fieldSpriteScale + _bottomAnchorYOffset + _healthBarTopPadding;
    }

    private void SetAlpha(float alpha)
    {
        if (_renderer == null) return;

        Color color = _renderer.color;
        color.a = alpha;
        _renderer.color = color;
    }

    private void ReachDestination()
    {
        Debug.Log($"[AttackUnit] Reached destination: {(_card != null ? _card.cardName : name)}");
        _onReachedDestination?.Invoke(_owner);
        if (_owner == AttackUnitOwner.Player && !GameConfig.ENABLE_TEST_MODE)
        {
            _onOwnedReachedDestination?.Invoke(_networkUnitId);
        }
        Destroy(gameObject);
    }

    private void Die()
    {
        if (_owner == AttackUnitOwner.Player)
        {
            _onOwnedUnitDestroyed?.Invoke(_networkUnitId);
        }

        Destroy(gameObject);
    }

    public void ApplyAuthoritativeDestroy()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(HitCenter, _hitRadius);
    }
}
