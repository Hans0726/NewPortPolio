using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class InGameSceneInstaller : MonoBehaviour
{
    [Header("Runtime Mode")]
    [SerializeField] private bool _autoDetectTestMode = true;

    [Header("Existing Scene Roots")]
    [FormerlySerializedAs("_inGameUIManager")]
    [SerializeField] private GameObject _inGameHUDView;
    [FormerlySerializedAs("_inGameCardManager")]
    [SerializeField] private GameObject _inGameCardState;

    [Header("Scene Components")]
    [SerializeField] private InGameFlowController _flowController;
    [SerializeField] private InGameHUDView _hudView;
    [SerializeField] private InGameCardState _cardState;
    [SerializeField] private BattlePreparationController _preparationController;
    [SerializeField] private DefensePlacementManager _placementService;
    [SerializeField] private CombatRoundManager _combatService;

    private InGameMatchState _matchState;
    private CardPlayController _cardPlayController;
    private bool _installed;

    private void Start()
    {
        Install();
    }

    private void OnDestroy()
    {
        if (!_installed) return;

        _flowController?.Dispose();
        _preparationController?.Dispose();
        _cardPlayController?.Dispose();
        _hudView?.Dispose();
        _installed = false;
    }

    private void Install()
    {
        ConfigureRuntimeMode();
        ResolveSceneComponents();
        if (!ValidateSceneComponents()) return;

        NetworkGateway gateway = NetworkMananger.Instance != null
            ? NetworkMananger.Instance.Gateway
            : null;
        if (!GameConfig.ENABLE_TEST_MODE && gateway == null)
        {
            Debug.LogError("[InGameSceneInstaller] NetworkGateway is missing.");
            return;
        }

        _matchState = _flowController.CreateMatchState();
        _cardPlayController = new CardPlayController();
        _combatService.Configure(_placementService);

        InitializeDeck();
        _hudView.Initialize(_matchState, _cardState);
        _cardPlayController.Initialize(
            _matchState,
            _cardState,
            _hudView.HandUI,
            _hudView,
            _placementService,
            gateway);
        _preparationController.Initialize(
            _matchState,
            _hudView,
            _hudView.HandUI,
            _cardPlayController,
            gateway);
        _flowController.Initialize(
            _matchState,
            _cardState,
            _hudView,
            _preparationController,
            _cardPlayController,
            _combatService,
            gateway);

        foreach (ObjectiveHealthBar healthBar in FindObjectsByType<ObjectiveHealthBar>())
        {
            healthBar.Initialize(_matchState);
        }

        _installed = true;
        _flowController.StartGame();
        Debug.Log("[InGameSceneInstaller] In-game composition installed.");
    }

    private void InitializeDeck()
    {
        IReadOnlyList<short> selectedDeck = GameManager.Instance != null
            ? GameManager.Instance.SelectedDeckIds
            : null;

        if (GameConfig.ENABLE_TEST_MODE && (selectedDeck == null || selectedDeck.Count == 0))
        {
            Debug.LogWarning("[InGameSceneInstaller] Running with a generated test deck.");
            _cardState.InitializeForTest();
            return;
        }

        _cardState.Initialize(selectedDeck);
    }

    private void ConfigureRuntimeMode()
    {
        if (!_autoDetectTestMode) return;

        IReadOnlyList<short> selectedDeck = GameManager.Instance != null
            ? GameManager.Instance.SelectedDeckIds : null;
        GameConfig.ENABLE_TEST_MODE = selectedDeck == null || selectedDeck.Count == 0;
    }

    private void ResolveSceneComponents()
    {
        if (_hudView == null && _inGameHUDView != null)
        {
            _hudView = _inGameHUDView.GetComponent<InGameHUDView>();
        }
        if (_cardState == null && _inGameCardState != null)
        {
            _cardState = _inGameCardState.GetComponent<InGameCardState>();
        }

        _flowController ??= FindAnyObjectByType<InGameFlowController>();
        _hudView ??= FindAnyObjectByType<InGameHUDView>();
        _cardState ??= FindAnyObjectByType<InGameCardState>();
        _placementService ??= FindAnyObjectByType<DefensePlacementManager>();
        _combatService ??= FindAnyObjectByType<CombatRoundManager>();

        if (_preparationController == null && _hudView != null)
        {
            _preparationController = _hudView.GetComponent<BattlePreparationController>();
            if (_preparationController == null)
            {
                _preparationController = _hudView.gameObject.AddComponent<BattlePreparationController>();
            }
        }
    }

    private bool ValidateSceneComponents()
    {
        bool valid = true;
        valid &= Require(_flowController, nameof(_flowController));
        valid &= Require(_hudView, nameof(_hudView));
        valid &= Require(_cardState, nameof(_cardState));
        valid &= Require(_preparationController, nameof(_preparationController));
        valid &= Require(_placementService, nameof(_placementService));
        valid &= Require(_combatService, nameof(_combatService));
        return valid;
    }

    private static bool Require(Object value, string fieldName)
    {
        if (value != null) return true;

        Debug.LogError($"[InGameSceneInstaller] Missing scene reference: {fieldName}");
        return false;
    }
}
