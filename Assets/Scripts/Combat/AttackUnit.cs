using System;
using UnityEngine;

public enum AttackUnitOwner
{
    Player,
    Opponent
}

public class AttackUnit : MonoBehaviour
{
    [SerializeField] private float _waypointReachDistance = 0.05f;
    [SerializeField] private float _bobAmplitude = 0.05f;
    [SerializeField] private float _bobFrequency = 8f;
    [SerializeField] private Vector2 _healthBarSize = new Vector2(1.1f, 0.1f);
    [SerializeField] private float _healthBarTopPadding = 0.18f;
    [SerializeField] private int _healthBarSortingOrderOffset = 20;

    private CardData _card;
    private WaypointPath _path;
    private SpriteRenderer _renderer;
    private int _nextWaypointIndex;
    private float _moveSpeed;
    private int _maxHealth;
    private int _currentHealth;
    private int _defense;
    private AttackUnitOwner _owner;
    private Action<AttackUnitOwner> _onReachedDestination;
    private float _unitZ;
    private float _fieldSpriteScale = 1f;
    private float _hitRadius = 5f;
    private float _bottomAnchorYOffset;
    private bool _isHiding = false;
    private Vector3 _visualBaseLocalPosition;
    private WorldHealthBar _healthBar;

    public bool IsDead => _currentHealth <= 0;
    public bool IsHiding => _isHiding;
    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public int Defense => _defense;
    public float HitRadius => _hitRadius;
    public Vector3 HitCenter => _renderer != null ? _renderer.bounds.center : transform.position;
    public AttackUnitOwner Owner => _owner;

    public Vector3 GetPredictedHitCenter(float secondsAhead)
    {
        if (_path == null || _path.Count == 0 || secondsAhead <= 0f)
        {
            return HitCenter;
        }

        Vector3 predictedPosition = transform.position;
        float remainingDistance = _moveSpeed * secondsAhead;
        int waypointIndex = _nextWaypointIndex;
        int waypointCount = _path.Count;

        while (remainingDistance > 0f && waypointIndex < waypointCount)
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
            waypointIndex++;
        }

        return HitCenter + (predictedPosition - transform.position);
    }

    public void Initialize(CardData card, WaypointPath path, int sortingLayerId, int sortingOrder, float unitZ, float baseScale, float bottomAnchorYOffset, AttackUnitOwner owner, Action<AttackUnitOwner> onReachedDestination)
    {
        _card = card;
        _path = path;
        _owner = owner;
        _onReachedDestination = onReachedDestination;
        _unitZ = unitZ;
        _bottomAnchorYOffset = bottomAnchorYOffset;
        _fieldSpriteScale = Mathf.Max(0.01f, baseScale * (card != null ? card.fieldSpriteScale : 1f));
        _hitRadius = Mathf.Max(0f, (card != null ? card.fieldHitRadius : 0.25f) * _fieldSpriteScale);
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
        _renderer.transform.localScale = Vector3.one * _fieldSpriteScale;
        _visualBaseLocalPosition = GetBottomAnchoredVisualPosition(_renderer.sprite);

        if (_path != null && _path.Count > 0)
        {
            Vector3 startPosition = _path.GetWaypointPosition(0);
            startPosition.z = _unitZ;
            transform.position = startPosition;
            _nextWaypointIndex = 1;
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
        if (IsDead) return;

        MoveAlongPath();
        AnimateVisual();
    }

    public void TakeDamage(int rawDamage)
    {
        int actualDamage = Mathf.Max(1, rawDamage - _defense);
        _currentHealth -= actualDamage;
        _healthBar?.SetValue(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void MoveAlongPath()
    {
        if (_path == null || _path.Count == 0) return;

        if (_nextWaypointIndex >= _path.Count)
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
            if (_path.IsArrivedAtEntrance(_nextWaypointIndex - 0))
            {
                _isHiding = true;
                SetAlpha(0.3f);
            }
            if (_path.IsArrivedAtExit(_nextWaypointIndex - 0))
            {
                _isHiding = false;
                SetAlpha(1f);
            }

            _nextWaypointIndex++;
        }
    }

    private void AnimateVisual()
    {
        if (_renderer == null) return;

        float offsetY = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
        _renderer.transform.localPosition = _visualBaseLocalPosition + new Vector3(0f, offsetY, 0f);
    }

    private void CreateHealthBar(int sortingLayerId, int sortingOrder)
    {
        Vector3 offset = new Vector3(0f, GetHealthBarHeightOffset(), 0f);
        Vector2 size = _healthBarSize * Mathf.Max(0.75f, _fieldSpriteScale);
        _healthBar = WorldHealthBar.Create(transform, offset, size, new Color(0.2f, 0.9f, 0.25f, 0.95f), sortingLayerId, sortingOrder + _healthBarSortingOrderOffset);
        _healthBar.SetValue(_currentHealth, _maxHealth);
    }

    private float GetHealthBarHeightOffset()
    {
        if (_renderer == null || _renderer.sprite == null)
        {
            return 1f + _healthBarTopPadding;
        }

        return _renderer.sprite.bounds.size.y * _fieldSpriteScale + _bottomAnchorYOffset + _healthBarTopPadding;
    }

    private Vector3 GetBottomAnchoredVisualPosition(Sprite sprite)
    {
        if (sprite == null)
        {
            return new Vector3(0f, _bottomAnchorYOffset, 0f);
        }

        float bottomToOrigin = -sprite.bounds.min.y * _fieldSpriteScale;
        return new Vector3(0f, bottomToOrigin + _bottomAnchorYOffset, 0f);
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
        Destroy(gameObject);
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(HitCenter, _hitRadius);
    }
}
