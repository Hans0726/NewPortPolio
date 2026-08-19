using UnityEngine;

public class WorldHealthBar : MonoBehaviour
{
    private static Sprite _barSprite;

    private Transform _target;
    private Vector3 _offset;
    private Vector2 _size = new Vector2(1.2f, 0.12f);
    private SpriteRenderer _backgroundRenderer;
    private SpriteRenderer _fillRenderer;
    private float _fillRatio = 1f;
    private bool _keepInsideCamera;
    private Camera _worldCamera;
    private const float ViewportTopPadding = 0.025f;

    public static WorldHealthBar Create(
        Transform target,
        Vector3 offset,
        Vector2 size,
        Color fillColor,
        int sortingLayerId,
        int sortingOrder,
        bool keepInsideCamera = false)
    {
        GameObject barObject = new GameObject("WorldHealthBar");
        barObject.transform.SetParent(target, false);

        WorldHealthBar bar = barObject.AddComponent<WorldHealthBar>();
        bar.Initialize(
            target,
            offset,
            size,
            fillColor,
            sortingLayerId,
            sortingOrder,
            keepInsideCamera);
        return bar;
    }

    public void SetValue(int currentValue, int maxValue)
    {
        float ratio = maxValue <= 0 ? 0f : currentValue / (float)maxValue;
        SetFillRatio(ratio);
    }

    private void Initialize(
        Transform target,
        Vector3 offset,
        Vector2 size,
        Color fillColor,
        int sortingLayerId,
        int sortingOrder,
        bool keepInsideCamera)
    {
        _target = target;
        _offset = offset;
        _size = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
        _keepInsideCamera = keepInsideCamera;
        _worldCamera = keepInsideCamera ? Camera.main : null;

        _backgroundRenderer = CreatePart("Background", new Color(0.08f, 0.08f, 0.08f, 0.85f), sortingLayerId, sortingOrder);
        _fillRenderer = CreatePart("Fill", fillColor, sortingLayerId, sortingOrder + 1);

        UpdatePosition();
        ApplyVisuals();
    }

    private SpriteRenderer CreatePart(string partName, Color color, int sortingLayerId, int sortingOrder)
    {
        GameObject partObject = new GameObject(partName);
        partObject.transform.SetParent(transform, false);

        SpriteRenderer renderer = partObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetBarSprite();
        renderer.color = color;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void SetFillRatio(float ratio)
    {
        _fillRatio = Mathf.Clamp01(ratio);
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (_backgroundRenderer != null)
        {
            _backgroundRenderer.transform.localPosition = Vector3.zero;
            _backgroundRenderer.transform.localScale = new Vector3(_size.x, _size.y, 1f);
        }

        if (_fillRenderer != null)
        {
            float fillWidth = _size.x * _fillRatio;
            _fillRenderer.transform.localScale = new Vector3(fillWidth, _size.y, 1f);
            _fillRenderer.transform.localPosition = new Vector3(-(_size.x - fillWidth) * 0.5f, 0f, -0.01f);
        }
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (_target == null) return;

        Vector3 desiredWorldPosition = _target.TransformPoint(_offset);
        if (!_keepInsideCamera || _worldCamera == null)
        {
            transform.position = desiredWorldPosition;
            return;
        }

        Vector3 viewportPosition = _worldCamera.WorldToViewportPoint(desiredWorldPosition);
        if (viewportPosition.z <= 0f)
        {
            transform.position = desiredWorldPosition;
            return;
        }

        float halfBarHeight = _worldCamera.orthographic
            ? (_size.y * 0.5f) / (_worldCamera.orthographicSize * 2f)
            : 0f;
        viewportPosition.y = Mathf.Min(
            viewportPosition.y,
            1f - ViewportTopPadding - halfBarHeight);
        transform.position = _worldCamera.ViewportToWorldPoint(viewportPosition);
    }

    private static Sprite GetBarSprite()
    {
        if (_barSprite != null)
        {
            return _barSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;

        _barSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _barSprite.hideFlags = HideFlags.HideAndDontSave;
        return _barSprite;
    }
}
