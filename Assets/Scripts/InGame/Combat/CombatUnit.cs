using UnityEngine;

public abstract class CombatUnit : MonoBehaviour
{
    protected CardData _card;
    protected SpriteRenderer _renderer;
    protected float _baseFieldSpriteScale = 1f;
    protected float _fieldSpriteScale = 1f;
    protected float _bottomAnchorYOffset;
    protected Vector3 _visualBaseLocalPosition;

    protected void InitializeFieldVisual(
        CardData card,
        SpriteRenderer spriteRenderer,
        float baseScale,
        float bottomAnchorOffset)
    {
        _card = card;
        _renderer = spriteRenderer;
        _baseFieldSpriteScale = Mathf.Max(0.01f, baseScale);
        _bottomAnchorYOffset = bottomAnchorOffset;
        if (_renderer == null) return;

        ApplyFieldVisualScale();
    }

    protected void RefreshFieldVisualTuning()
    {
        if (_card == null || _renderer == null) return;

        float nextScale = GetConfiguredFieldScale();
        if (Mathf.Approximately(nextScale, _fieldSpriteScale)) return;

        ApplyFieldVisualScale();
        OnFieldVisualScaleChanged();
    }

    protected virtual void OnFieldVisualScaleChanged()
    {
    }

    protected void SetVisualBobOffset(float offsetY)
    {
        if (_renderer == null) return;

        _renderer.transform.localPosition =
            _visualBaseLocalPosition + new Vector3(0f, offsetY, 0f);
    }

    protected void ResetVisualPosition()
    {
        if (_renderer != null)
        {
            _renderer.transform.localPosition = _visualBaseLocalPosition;
        }
    }

    protected Vector3 GetVisualCenter()
    {
        return _renderer != null ? _renderer.bounds.center : transform.position;
    }

    private void ApplyFieldVisualScale()
    {
        _fieldSpriteScale = GetConfiguredFieldScale();
        _renderer.transform.localScale = Vector3.one * _fieldSpriteScale;
        _visualBaseLocalPosition = GetBottomAnchoredVisualPosition(_renderer.sprite);
        _renderer.transform.localPosition = _visualBaseLocalPosition;
    }

    private float GetConfiguredFieldScale()
    {
        float cardScale = _card != null ? _card.fieldSpriteScale : 1f;
        return Mathf.Max(0.01f, _baseFieldSpriteScale * cardScale);
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
}
