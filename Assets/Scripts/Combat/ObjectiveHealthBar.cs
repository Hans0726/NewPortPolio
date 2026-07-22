using UnityEngine;

public class ObjectiveHealthBar : MonoBehaviour
{
    [SerializeField] private AttackUnitOwner _objectiveOwner = AttackUnitOwner.Opponent;
    [SerializeField] private Vector3 _barOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private Vector2 _barSize = new Vector2(1.4f, 0.12f);
    [SerializeField] private Color _fillColor = new Color(0.2f, 0.9f, 0.25f, 0.95f);
    [SerializeField] private string _sortingLayerName = "Layer 1";
    [SerializeField] private int _sortingOrder = 120;

    private WorldHealthBar _healthBar;

    private void Start()
    {
        _healthBar = WorldHealthBar.Create(transform, _barOffset, _barSize, _fillColor, ResolveSortingLayerID(), _sortingOrder);

        if (GameTurnManager.Instance != null)
        {
            GameTurnManager.Instance.OnLifeChanged += HandleLifeChanged;
            HandleLifeChanged(GameTurnManager.Instance.PlayerLife, GameTurnManager.Instance.OpponentLife);
        }
    }

    private void OnDestroy()
    {
        if (GameTurnManager.Instance != null)
        {
            GameTurnManager.Instance.OnLifeChanged -= HandleLifeChanged;
        }
    }

    private void HandleLifeChanged(int playerLife, int opponentLife)
    {
        if (_healthBar == null || GameTurnManager.Instance == null) return;

        int currentLife = _objectiveOwner == AttackUnitOwner.Player ? playerLife : opponentLife;
        int maxLife = _objectiveOwner == AttackUnitOwner.Player
            ? GameTurnManager.Instance.PlayerMaxLife
            : GameTurnManager.Instance.OpponentMaxLife;

        _healthBar.SetValue(currentLife, maxLife);
    }

    private int ResolveSortingLayerID()
    {
        int sortingLayerId = SortingLayer.NameToID(_sortingLayerName);
        if (sortingLayerId == 0 && _sortingLayerName != "Default")
        {
            Debug.LogWarning($"[ObjectiveHealthBar] Sorting Layer '{_sortingLayerName}' was not found.");
        }

        return sortingLayerId;
    }
}
