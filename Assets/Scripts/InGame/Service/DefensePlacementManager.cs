using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DefensePlacementManager : MonoBehaviour
{
    [Header("Placement Area")]
    [SerializeField] private Collider2D[] _placeableAreas;
    [SerializeField] private SpriteRenderer[] _placeableAreaRenderers;
    [SerializeField] private Color _placeableColor = new Color(0.1f, 1f, 0.1f, 0.35f);

    [Header("Placement Preview")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _placedUnitRoot;
    [SerializeField] private float _previewZ = -1f;
    [SerializeField] private float _previewScale = 1f;
    [SerializeField] private float _bottomAnchorYOffset = 0.1f;
    [SerializeField] private string _previewSortingLayerName = "Layer 1";
    [SerializeField] private int _previewSortingOrder = 100;

   
    private CardData _pendingCard;
    private Action<CardData, Vector3, int> _onPlaced;
    private Action _onPlacementEnded;
    private GameObject _previewObject;
    private SpriteRenderer _previewRenderer;
    private bool _isPlacing;
    private readonly List<DefenseUnit> _placedUnits = new List<DefenseUnit>();
    private readonly Dictionary<int, DefenseUnit> _opponentUnitsById = new Dictionary<int, DefenseUnit>();
    private readonly Dictionary<int, int> _pendingOpponentTargets = new Dictionary<int, int>();
    private readonly Dictionary<int, (Vector3 Position, bool FlipX)> _pendingOpponentMovements =
        new Dictionary<int, (Vector3, bool)>();
    private int _nextLocalUnitId = 1;

    public event Action<int, int> OwnedDefenseTargetChanged;
    public event Action<CombatUnitType, int, Vector3, bool, bool> OwnedUnitMovementChanged;
    public event Action<int, int, int> OwnedDefenseAttack;

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

    public bool IsWorldPositionPlaceable(Vector3 worldPosition, AttackUnitOwner owner)
    {
        if (owner == AttackUnitOwner.Opponent)
        {
            worldPosition = MirrorWorldPosition(worldPosition);
        }

        return IsWorldPositionPlaceable(worldPosition);
    }

    private void Awake()
    {
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

    public bool BeginPlacement(CardData card, Action<CardData, Vector3, int> onPlaced, Action onPlacementEnded = null)
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

        ShowPlacementArea(true);
        CreatePreview(card);
        UpdatePreviewPosition();
        return true;
    }

    private void ShowPlacementArea(bool isOn)
    {
        foreach (SpriteRenderer sr in _placeableAreaRenderers)
        {
            if (isOn)
                sr.color = _placeableColor;
            else
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
        }
    }

    private void CreatePreview(CardData card)
    {
        _previewObject = new GameObject($"DefensePlacementPreview_{card.cardName}");
        _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
        _previewRenderer.sprite = GetCardSprite(card);
        _previewRenderer.color = new Color(1f, 1f, 1f, 0.75f);
        _previewRenderer.sortingLayerID = ResolveSortingLayerID();
        _previewRenderer.sortingOrder = _previewSortingOrder;
        _previewObject.transform.localScale = Vector3.one * GetFieldVisualScale(card);

        Vector3 centerBottom = _worldCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.2f, Mathf.Abs(_worldCamera.transform.position.z)));
        centerBottom.z = _previewZ;
        _previewObject.transform.position = centerBottom;
    }

    private Sprite GetCardSprite(CardData card)
    {
        return CombatSpriteUtility.GetCardSprite(card);
    }

    private void UpdatePreviewPosition()
    {
        if (_previewObject == null || _worldCamera == null) return;

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        Sprite previewSprite = _previewRenderer != null ? _previewRenderer.sprite : null;
        float visualScale = GetFieldVisualScale(_pendingCard);
        _previewObject.transform.localScale = Vector3.one * visualScale;
        _previewObject.transform.position = GetSpritePositionFromBottomAnchor(
            mouseWorldPosition,
            previewSprite,
            visualScale);

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
        int unitId = _nextLocalUnitId++;
        PlaceUnit(_pendingCard, groundPosition, AttackUnitOwner.Player, unitId);
        _onPlaced?.Invoke(_pendingCard, groundPosition, unitId);
        EndPlacement();
    }

    public Vector3 MirrorWorldPosition(Vector3 worldPosition)
    {
        Vector3 center = _worldCamera.transform.position;
        return new Vector3(
            center.x * 2f - worldPosition.x,
            center.y * 2f - worldPosition.y,
            worldPosition.z);
;
    }

    public void PlaceRemoteDefenseUnit(CardData card, Vector3 groundPosition, int unitId)
    {
        if (card == null) return;

        PlaceUnit(card, groundPosition, AttackUnitOwner.Opponent, unitId);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(_worldCamera.transform.position.z);
        return _worldCamera.ScreenToWorldPoint(mousePosition);
    }

    private void PlaceUnit(
        CardData card,
        Vector3 groundPosition,
        AttackUnitOwner owner,
        int unitId)
    {
        GameObject unitObject = new GameObject($"DefenseUnit_{card.cardName}");
        if (_placedUnitRoot != null)
        {
            unitObject.transform.SetParent(_placedUnitRoot, true);
        }

        Vector3 unitPosition = groundPosition;
        unitPosition.z = _previewZ;
        unitObject.transform.position = unitPosition;

        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(unitObject.transform, false);

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetCardSprite(card);
        renderer.sortingLayerID = ResolveSortingLayerID();
        renderer.sortingOrder = _previewSortingOrder - 1;

        DefenseUnit defenseUnit = unitObject.AddComponent<DefenseUnit>();
        defenseUnit.Initialize(
            card,
            unitPosition,
            groundPosition,
            owner,
            unitId,
            this,
            HandleOwnedDefenseTargetChanged,
            HandleOwnedUnitMovementChanged,
            HandleOwnedDefenseAttack,
            _previewScale,
            _bottomAnchorYOffset);
        _placedUnits.Add(defenseUnit);
        if (owner == AttackUnitOwner.Opponent)
        {
            _opponentUnitsById[unitId] = defenseUnit;
            if (_pendingOpponentTargets.Remove(unitId, out int targetUnitId))
            {
                defenseUnit.ApplyAuthoritativeTarget(targetUnitId);
            }
            if (_pendingOpponentMovements.Remove(unitId, out var movement))
            {
                defenseUnit.ApplyAuthoritativeMovement(movement.Position, movement.FlipX);
            }
        }
    }

    public void ApplyOpponentDefenseTarget(int defenseUnitId, int targetUnitId)
    {
        if (_opponentUnitsById.TryGetValue(defenseUnitId, out DefenseUnit unit) && unit != null)
        {
            unit.ApplyAuthoritativeTarget(targetUnitId);
            return;
        }

        _pendingOpponentTargets[defenseUnitId] = targetUnitId;
    }

    private void HandleOwnedDefenseTargetChanged(int defenseUnitId, int targetUnitId)
    {
        OwnedDefenseTargetChanged?.Invoke(defenseUnitId, targetUnitId);
    }

    public void ApplyOpponentDefenseMovement(int unitId, Vector3 position, bool flipX)
    {
        if (_opponentUnitsById.TryGetValue(unitId, out DefenseUnit unit) && unit != null)
        {
            unit.ApplyAuthoritativeMovement(position, flipX);
            return;
        }

        _pendingOpponentMovements[unitId] = (position, flipX);
    }

    private void HandleOwnedUnitMovementChanged(
        CombatUnitType unitType,
        int unitId,
        Vector3 position,
        bool flipX,
        bool isHiding)
    {
        OwnedUnitMovementChanged?.Invoke(unitType, unitId, position, flipX, isHiding);
    }

    private void HandleOwnedDefenseAttack(int attackerId, int targetId, int damage)
    {
        OwnedDefenseAttack?.Invoke(attackerId, targetId, damage);
    }

    public void PlayOpponentDefenseAttackFeedback(int unitId)
    {
        if (_opponentUnitsById.TryGetValue(unitId, out DefenseUnit unit) && unit != null)
        {
            unit.PlayAuthoritativeAttackFeedback();
        }
    }

    public void SetPlacedUnitsCombatActive(bool active)
    {
        for (int i = _placedUnits.Count - 1; i >= 0; i--)
        {
            DefenseUnit unit = _placedUnits[i];
            if (unit == null)
            {
                _placedUnits.RemoveAt(i);
                continue;
            }

            unit.SetCombatActive(active);
        }
    }

    private Vector3 GetSpritePositionFromBottomAnchor(
        Vector3 bottomAnchorPosition,
        Sprite sprite,
        float visualScale)
    {
        Vector3 position = bottomAnchorPosition;
        position.z = _previewZ;

        if (sprite != null)
        {
            position.y -= sprite.bounds.min.y * visualScale;
        }

        position.y += _bottomAnchorYOffset;
        position.y += _pendingCard != null ? _pendingCard.fieldSpriteYOffset : 0f;
        return position;
    }

    private float GetFieldVisualScale(CardData card)
    {
        float cardScale = card != null ? card.fieldSpriteScale : 1f;
        return Mathf.Max(0.01f, _previewScale * cardScale);
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

        ShowPlacementArea(false);
        _onPlaced = null;
        _onPlacementEnded?.Invoke();
        _onPlacementEnded = null;
        _pendingCard = null;
        _isPlacing = false;
    }
}
