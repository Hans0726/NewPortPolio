using System.Collections;
using UnityEngine;

public abstract class CombatUnit : MonoBehaviour
{
    private CombatRoundManager _combatClock;
    protected float CombatTime => _combatClock != null ? _combatClock.CombatTime : Time.time;
    protected float CombatDeltaTime => _combatClock != null ? _combatClock.CombatDeltaTime : Time.deltaTime;

    public void SetCombatClock(CombatRoundManager combatClock)
    {
        _combatClock = combatClock;
    }

    protected CardData _card;
    protected SpriteRenderer _renderer;
    protected float _baseFieldSpriteScale = 1f;
    protected float _fieldSpriteScale = 1f;
    protected float _fieldSpriteYOffset;
    protected float _bottomAnchorYOffset;
    protected Vector3 _visualBaseLocalPosition;
    private Coroutine _visualFeedbackRoutine;
    private Color _visualFeedbackOriginalColor;
    private bool _hasVisualFeedback;

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
        float nextYOffset = GetConfiguredFieldYOffset();
        if (Mathf.Approximately(nextScale, _fieldSpriteScale) &&
            Mathf.Approximately(nextYOffset, _fieldSpriteYOffset)) return;

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

    protected void PlayAttackFeedback()
    {
        PlayVisualFeedback(new Color(1f, 0.75f, 0.25f, 1f), 1.08f, 0.09f);
    }

    protected void PlayHitFeedback()
    {
        PlayVisualFeedback(new Color(1f, 0.2f, 0.2f, 1f), 0.94f, 0.11f);
    }

    protected void CancelVisualFeedback()
    {
        if (_visualFeedbackRoutine != null)
        {
            StopCoroutine(_visualFeedbackRoutine);
            _visualFeedbackRoutine = null;
        }

        if (_hasVisualFeedback)
        {
            RestoreVisualFeedback();
        }
    }

    private void ApplyFieldVisualScale()
    {
        _fieldSpriteScale = GetConfiguredFieldScale();
        _fieldSpriteYOffset = GetConfiguredFieldYOffset();
        _renderer.transform.localScale = Vector3.one * _fieldSpriteScale;
        _visualBaseLocalPosition = GetBottomAnchoredVisualPosition(_renderer.sprite);
        _renderer.transform.localPosition = _visualBaseLocalPosition;
    }

    private float GetConfiguredFieldScale()
    {
        float cardScale = _card != null ? _card.fieldSpriteScale : 1f;
        return Mathf.Max(0.01f, _baseFieldSpriteScale * cardScale);
    }

    private float GetConfiguredFieldYOffset()
    {
        return _card != null ? _card.fieldSpriteYOffset : 0f;
    }

    private Vector3 GetBottomAnchoredVisualPosition(Sprite sprite)
    {
        if (sprite == null)
        {
            return new Vector3(0f, _bottomAnchorYOffset + _fieldSpriteYOffset, 0f);
        }

        float bottomToOrigin = -sprite.bounds.min.y * _fieldSpriteScale;
        return new Vector3(
            0f,
            bottomToOrigin + _bottomAnchorYOffset + _fieldSpriteYOffset,
            0f);
    }

    private void PlayVisualFeedback(Color feedbackColor, float scaleMultiplier, float duration)
    {
        if (_renderer == null) return;

        CancelVisualFeedback();
        _visualFeedbackOriginalColor = _renderer.color;
        _hasVisualFeedback = true;

        _visualFeedbackRoutine = StartCoroutine(
            RunVisualFeedback(feedbackColor, scaleMultiplier, duration));
    }

    private IEnumerator RunVisualFeedback(Color feedbackColor, float scaleMultiplier, float duration)
    {
        Vector3 originalScale = Vector3.one * _fieldSpriteScale;
        feedbackColor.a = _visualFeedbackOriginalColor.a;

        _renderer.color = Color.Lerp(_visualFeedbackOriginalColor, feedbackColor, 0.7f);
        _renderer.transform.localScale = originalScale * scaleMultiplier;
        float remaining = duration;
        do
        {
            yield return null;
            remaining -= CombatDeltaTime;
        } while (remaining > 0f);

        RestoreVisualFeedback();
        _visualFeedbackRoutine = null;
    }

    private void RestoreVisualFeedback()
    {
        if (_renderer == null) return;

        _renderer.color = _visualFeedbackOriginalColor;
        _renderer.transform.localScale = Vector3.one * _fieldSpriteScale;
        _hasVisualFeedback = false;
    }
}
