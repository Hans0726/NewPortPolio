using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    [SerializeField] private float _waypointReachDistance = 0.05f;
    [SerializeField] private float _bobAmplitude = 0.05f;
    [SerializeField] private float _bobFrequency = 8f;

    private CardData _card;
    private WaypointPath _path;
    private SpriteRenderer _renderer;
    private int _nextWaypointIndex;
    private float _moveSpeed;
    private int _maxHealth;
    private int _currentHealth;
    private int _defense;
    private float _unitZ;
    private Vector3 _visualBaseLocalPosition;

    public bool IsDead => _currentHealth <= 0;
    public int Defense => _defense;

    public void Initialize(CardData card, WaypointPath path, int sortingLayerId, int sortingOrder, float unitZ)
    {
        _card = card;
        _path = path;
        _unitZ = unitZ;
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
        _visualBaseLocalPosition = Vector3.zero;

        if (_path != null && _path.Count > 0)
        {
            Vector3 startPosition = _path.GetWaypointPosition(0);
            startPosition.z = _unitZ;
            transform.position = startPosition;
            _nextWaypointIndex = 1;
        }

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
            _renderer.flipX = movement.x < 0f;
        }

        if ((transform.position - target).sqrMagnitude <= _waypointReachDistance * _waypointReachDistance)
        {
            if (_path.IsArrivedAtEntrance(_nextWaypointIndex - 0))
                SetAlpha(0.3f);
            if (_path.IsArrivedAtExit(_nextWaypointIndex - 0))
                SetAlpha(1f);

            _nextWaypointIndex++;
        }
    }

    private void AnimateVisual()
    {
        if (_renderer == null) return;

        float offsetY = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
        _renderer.transform.localPosition = _visualBaseLocalPosition + new Vector3(0f, offsetY, 0f);
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
        Destroy(gameObject);
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
