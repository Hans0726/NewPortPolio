using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DefensePlacementManager : MonoBehaviour
{
    public static DefensePlacementManager Instance { get; private set; }

    [Header("Placement Area")]
    [SerializeField] private Collider2D[] _placeableAreas;

    [Header("Placement Preview")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _placedUnitRoot;
    [SerializeField] private float _previewZ = -1f;
    [SerializeField] private float _previewScale = 1f;
    [SerializeField] private float _bottomAnchorYOffset = 0.1f;
    [SerializeField] private string _previewSortingLayerName = "Layer 1";
    [SerializeField] private int _previewSortingOrder = 100;
    [SerializeField] private int _overlaySortingOrder = 2;

    [Header("Overlay Colors")]
    [SerializeField] private Color _placeableColor = new Color(0.1f, 1f, 0.1f, 0.35f);

    private CardData _pendingCard;
    private Action<CardData, Vector3> _onPlaced;
    private Action _onPlacementEnded;
    private GameObject _previewObject;
    private SpriteRenderer _previewRenderer;
    private bool _isPlacing;

    public bool IsPlacing => _isPlacing;

    public bool IsWorldPositionPlaceable(Vector3 worldPosition)
    {
        Vector2 point = worldPosition;
        foreach (Collider2D area in _placeableAreas)
        {
            if (area != null && area.OverlapPoint(point))
                return true;
        }

        return false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!_isPlacing) return;

        UpdatePreviewPosition();

        if (Input.GetMouseButtonUp(0))
        {
            TryPlaceAtMousePosition();
        }

    }

    public bool BeginPlacement(CardData card, Action<CardData, Vector3> onPlaced, Action onPlacementEnded = null)
    {
        if (_isPlacing)
        {
            Debug.LogWarning("[DefensePlacementManager] Placement is already in progress.");
            return false;
        }

        if (card == null)
        {
            Debug.LogWarning("[DefensePlacementManager] Cannot begin placement with null card.");
            return false;
        }

        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
            if (_worldCamera == null)
            {
                Debug.LogError("[DefensePlacementManager] World Camera is not assigned and MainCamera was not found.");
                return false;
            }
        }

        _pendingCard = card;
        _onPlaced = onPlaced;
        _onPlacementEnded = onPlacementEnded;
        _isPlacing = true;

        CreatePreview(card);
        UpdatePreviewPosition();
        return true;
    }

    //private void ShowPlacementOverlay()
    //{
    //    _placementOverlayTilemap.ClearAllTiles();
    //    ApplyOverlayRendererSettings();

    //    _overlayTile = ScriptableObject.CreateInstance<Tile>();
    //    _overlayTile.sprite = CreateOverlaySprite();
    //    _overlayTile.colliderType = Tile.ColliderType.None;

    //    foreach (Vector3Int cell in _placeableCells)
    //    {
    //        SetOverlayCell(cell, _placeableColor);
    //    }
    //}

    //private Sprite CreateOverlaySprite()
    //{
    //    const int textureSize = 32;
    //    Texture2D texture = new Texture2D(textureSize, textureSize);
    //    texture.filterMode = FilterMode.Point;
    //    texture.wrapMode = TextureWrapMode.Clamp;

    //    Color[] pixels = new Color[textureSize * textureSize];
    //    for (int i = 0; i < pixels.Length; i++)
    //    {
    //        pixels[i] = Color.white;
    //    }

    //    texture.SetPixels(pixels);
    //    texture.Apply();
    //    texture.hideFlags = HideFlags.HideAndDontSave;

    //    return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    //}

    //private void ApplyOverlayRendererSettings()
    //{
    //    if (_placementOverlayTilemap == null) return;
    //    if (_placementOverlayTilemap == _placeableTilemap) return;

    //    TilemapRenderer overlayRenderer = _placementOverlayTilemap.GetComponent<TilemapRenderer>();
    //    if (overlayRenderer == null) return;

    //    overlayRenderer.sortingLayerID = ResolveSortingLayerID();
    //    overlayRenderer.sortingOrder = _overlaySortingOrder;
    //}

    private void CreatePreview(CardData card)
    {
        _previewObject = new GameObject($"DefensePlacementPreview_{card.cardName}");
        _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
        _previewRenderer.sprite = GetCardSprite(card);
        _previewRenderer.color = new Color(1f, 1f, 1f, 0.75f);
        _previewRenderer.sortingLayerID = ResolveSortingLayerID();
        _previewRenderer.sortingOrder = _previewSortingOrder;
        _previewObject.transform.localScale = Vector3.one * _previewScale;

        Vector3 centerBottom = _worldCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.2f, Mathf.Abs(_worldCamera.transform.position.z)));
        centerBottom.z = _previewZ;
        _previewObject.transform.position = centerBottom;
    }

    private Sprite GetCardSprite(CardData card)
    {
        if (card.cardImage != null)
        {
            return card.cardImage;
        }

        Sprite resourceSprite = Resources.Load<Sprite>("CardImage/" + card.cardName);
        return resourceSprite;
    }

    private void UpdatePreviewPosition()
    {
        if (_previewObject == null || _worldCamera == null) return;

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        Sprite previewSprite = _previewRenderer != null ? _previewRenderer.sprite : null;
        _previewObject.transform.position = GetSpritePositionFromBottomAnchor(mouseWorldPosition, previewSprite);

        if (_previewRenderer != null)
        {
            _previewRenderer.color = IsWorldPositionPlaceable(mouseWorldPosition)
                ? new Color(1f, 1f, 1f, 0.85f)
                : new Color(1f, 0.4f, 0.4f, 0.65f);
        }
    }

    private void TryPlaceAtMousePosition()
    {
        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        if (!IsWorldPositionPlaceable(mouseWorldPosition))
        {
            Debug.Log("[DefensePlacementManager] Cannot place defense unit outside the placeable area.");
            return;
        }

        Vector3 groundPosition = mouseWorldPosition;
        Vector3 placePosition = groundPosition;
        Sprite unitSprite = GetCardSprite(_pendingCard);
        placePosition = GetSpritePositionFromBottomAnchor(placePosition, unitSprite);
        PlaceUnit(_pendingCard, placePosition, groundPosition);
        _onPlaced?.Invoke(_pendingCard, placePosition);
        EndPlacement();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(_worldCamera.transform.position.z);
        return _worldCamera.ScreenToWorldPoint(mousePosition);
    }

    private void PlaceUnit(CardData card, Vector3 position, Vector3 groundPosition)
    {
        GameObject unitObject = new GameObject($"DefenseUnit_{card.cardName}");
        if (_placedUnitRoot != null)
        {
            unitObject.transform.SetParent(_placedUnitRoot, true);
        }

        unitObject.transform.position = position;
        unitObject.transform.localScale = Vector3.one * _previewScale;

        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(unitObject.transform, false);

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetCardSprite(card);
        renderer.sortingLayerID = ResolveSortingLayerID();
        renderer.sortingOrder = _previewSortingOrder - 1;

        DefenseUnit defenseUnit = unitObject.AddComponent<DefenseUnit>();
        defenseUnit.Initialize(card, position, groundPosition);
    }

    private Vector3 GetSpritePositionFromBottomAnchor(Vector3 bottomAnchorPosition, Sprite sprite)
    {
        Vector3 position = bottomAnchorPosition;
        position.z = _previewZ;

        if (sprite != null)
        {
            position.y -= sprite.bounds.min.y * _previewScale;
        }

        position.y += _bottomAnchorYOffset;
        return position;
    }

    private int ResolveSortingLayerID()
    {
        int sortingLayerId = SortingLayer.NameToID(_previewSortingLayerName);
        if (sortingLayerId == 0 && _previewSortingLayerName != "Default")
        {
            Debug.LogWarning($"[DefensePlacementManager] Sorting Layer '{_previewSortingLayerName}' was not found. Check Project Settings > Tags and Layers > Sorting Layers.");
        }

        return sortingLayerId;
    }

    private void EndPlacement()
    {
        if (_previewObject != null)
        {
            Destroy(_previewObject);
            _previewObject = null;
            _previewRenderer = null;
        }

        _onPlaced = null;
        _onPlacementEnded?.Invoke();
        _onPlacementEnded = null;
        _pendingCard = null;
        _isPlacing = false;
    }
}
