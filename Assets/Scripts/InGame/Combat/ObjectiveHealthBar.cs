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
    private InGameMatchState _matchState;

    public void Initialize(InGameMatchState matchState)
    {
        if (_matchState != null)
        {
            _matchState.LifeChanged -= HandleLifeChanged;
        }

        _matchState = matchState;
        if (_matchState != null)
        {
            _matchState.LifeChanged += HandleLifeChanged;
            RenderCurrentLife();
        }
    }

    private void Start()
    {
        _healthBar = WorldHealthBar.Create(transform, _barOffset, _barSize, _fillColor, ResolveSortingLayerID(), _sortingOrder);

        RenderCurrentLife();
    }

    private void OnDestroy()
    {
        if (_matchState != null)
        {
            _matchState.LifeChanged -= HandleLifeChanged;
        }
    }

    private void HandleLifeChanged(int playerLife, int opponentLife)
    {
        if (_healthBar == null || _matchState == null) return;

        int currentLife = _objectiveOwner == AttackUnitOwner.Player ? playerLife : opponentLife;
        int maxLife = _objectiveOwner == AttackUnitOwner.Player
            ? _matchState.PlayerMaxLife
            : _matchState.OpponentMaxLife;

        _healthBar.SetValue(currentLife, maxLife);
    }

    private void RenderCurrentLife()
    {
        if (_matchState == null) return;
        HandleLifeChanged(_matchState.PlayerLife, _matchState.OpponentLife);
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
