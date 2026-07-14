using UnityEngine;

public class DefenseUnit : MonoBehaviour
{
    [SerializeField] private float _attackRange = 2.5f;
    [SerializeField] private float _attackDistance = 0.35f;
    [SerializeField] private float _bobAmplitude = 0.035f;
    [SerializeField] private float _bobFrequency = 8f;

    private CardData _card;
    private SpriteRenderer _renderer;
    private Vector3 _homePosition;
    private AttackUnit _target;
    private float _moveSpeed;
    private int _attack;
    private float _attackSpeed;
    private float _nextAttackTime;
    private Vector3 _visualBaseLocalPosition;

    public void Initialize(CardData card, Vector3 homePosition)
    {
        _card = card;
        _homePosition = homePosition;
        _moveSpeed = Mathf.Max(0.1f, card != null ? card.moveSpeed : 1f);
        _attack = Mathf.Max(1, card != null ? card.attack : 1);
        _attackSpeed = Mathf.Max(0.1f, card != null ? card.attackSpeed : 1f);
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _visualBaseLocalPosition = _renderer != null ? _renderer.transform.localPosition : Vector3.zero;
    }

    private void Update()
    {
        AcquireTarget();

        if (_target != null)
        {
            HandleTarget();
        }
        else
        {
            ReturnHome();
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

        float sqrHomeDistance = (target.transform.position - _homePosition).sqrMagnitude;
        return sqrHomeDistance <= _attackRange * _attackRange;
    }

    private void HandleTarget()
    {
        if (!IsValidTarget(_target))
        {
            _target = null;
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance > _attackDistance)
        {
            MoveTowards(_target.transform.position);
            return;
        }

        TryAttack();
    }

    private void TryAttack()
    {
        if (_target == null || Time.time < _nextAttackTime) return;

        _target.TakeDamage(_attack);
        _nextAttackTime = Time.time + 1f / _attackSpeed;
    }

    private void ReturnHome()
    {
        if ((transform.position - _homePosition).sqrMagnitude <= 0.001f) return;

        MoveTowards(_homePosition);
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

        Vector3 movement = transform.position - previousPosition;
        if (_renderer != null && Mathf.Abs(movement.x) > 0.001f)
        {
            _renderer.flipX = movement.x < 0f;
        }
    }

    private void AnimateVisual()
    {
        if (_renderer == null) return;

        float isMoving = ((transform.position - _homePosition).sqrMagnitude > 0.001f || _target != null) ? 1f : 0f;
        float offsetY = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude * isMoving;
        _renderer.transform.localPosition = _visualBaseLocalPosition + new Vector3(0f, offsetY, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? _homePosition : transform.position;
        Gizmos.DrawWireSphere(center, _attackRange);
    }
}
