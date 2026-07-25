using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattlePreparationController : MonoBehaviour
{
    public static BattlePreparationController Instance { get; private set; }

    [SerializeField] private int _testPreparationSeconds = 30;

    private InGameHandUI _handUI;
    private InGameUIManager _hud;
    private Button _turnEndButton;
    private Coroutine _timerRoutine;
    private bool _isPreparing;
    private bool _readyRequested;
    private bool _readyPendingAfterPlacement;
    private readonly List<CardData> _opponentAttackCards = new List<CardData>();

    public bool IsPreparing => _isPreparing;
    public IReadOnlyList<CardData> OpponentAttackCards => _opponentAttackCards;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (_handUI != null)
        {
            _handUI.CardUseRequested -= HandleCardUseRequested;
        }

        if (_turnEndButton != null)
        {
            _turnEndButton.onClick.RemoveListener(RequestTurnEnd);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Configure(InGameHandUI handUI, InGameUIManager hud, Button turnEndButton)
    {
        if (_handUI != null)
        {
            _handUI.CardUseRequested -= HandleCardUseRequested;
        }

        if (_turnEndButton != null)
        {
            _turnEndButton.onClick.RemoveListener(RequestTurnEnd);
        }

        _handUI = handUI;
        _hud = hud;
        _turnEndButton = turnEndButton;

        _handUI.CardUseRequested += HandleCardUseRequested;
        _turnEndButton.onClick.AddListener(RequestTurnEnd);
        SetTurnEndButtonInteractable(false);
    }

    public void NotifyOpeningSequenceFinished()
    {
        if (GameConfig.ENABLE_TEST_MODE)
        {
            BeginPreparation(
                GameTurnManager.Instance != null ? GameTurnManager.Instance.CurrentRound : 1,
                _testPreparationSeconds);
            return;
        }

        SendTurnStartReady();
    }

    public void BeginPreparation(int roundNumber, int preparationSeconds)
    {
        _isPreparing = true;
        _readyRequested = false;
        _readyPendingAfterPlacement = false;
        _handUI.SetInteractionLocked(false);
        SetTurnEndButtonInteractable(true);

        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
        }

        int duration = preparationSeconds > 0
            ? preparationSeconds
            : _testPreparationSeconds;
        _timerRoutine = StartCoroutine(RunPreparationTimer(duration));
    }

    public void RequestTurnEnd()
    {
        if (!_isPreparing || _readyRequested) return;

        if (DefensePlacementManager.Instance != null && DefensePlacementManager.Instance.IsPlacing)
        {
            _readyPendingAfterPlacement = true;
            _handUI.SetInteractionLocked(true);
            SetTurnEndButtonInteractable(false);
            return;
        }

        CompletePreparationRequest();
    }

    public void NotifyCombatRoundFinished()
    {
        if (GameConfig.ENABLE_TEST_MODE)
        {
            GameTurnManager.Instance.StartNextRoundForTest();
            return;
        }

        SendTurnStartReady();
    }

    public void ReceiveOpponentCardSelection(S_CardSelectResult packet)
    {
        if (packet == null || InGameCardManager.Instance == null) return;

        foreach (S_CardSelectResult.SelectedCardIds selectedCard in packet.selectedCardIdss)
        {
            CardData card = InGameCardManager.Instance.GetCardDataById(selectedCard.cardId);
            if (card != null && card.cardType == CardType.Attack)
            {
                _opponentAttackCards.Add(card);
            }
        }
    }

    public void ReceiveOpponentDefensePlacement(S_UnitPlacementResult packet)
    {
        if (packet == null || !packet.isSuccess || InGameCardManager.Instance == null) return;

        CardData card = InGameCardManager.Instance.GetCardDataById(packet.cardId);
        if (card == null || card.cardType != CardType.Defense) return;

        DefensePlacementManager.Instance?.PlaceRemoteDefenseUnit(
            card,
            new Vector3(packet.x, packet.y, 0f));
    }

    private IEnumerator RunPreparationTimer(int duration)
    {
        float remainingTime = Mathf.Max(0, duration);
        while (_isPreparing && !_readyRequested && remainingTime > 0f)
        {
            _hud.SetPreparationTime(Mathf.CeilToInt(remainingTime));
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        _hud.SetPreparationTime(0);
        _timerRoutine = null;

        if (_isPreparing && !_readyRequested)
        {
            RequestTurnEnd();
        }
    }

    private void CompletePreparationRequest()
    {
        _readyRequested = true;
        _isPreparing = false;
        _handUI.SetInteractionLocked(true);
        SetTurnEndButtonInteractable(false);

        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        if (GameConfig.ENABLE_TEST_MODE)
        {
            GameTurnManager.Instance.BeginCombat();
            return;
        }

        if (NetworkMananger.Instance == null)
        {
            Debug.LogError("[BattlePreparationController] NetworkMananger is missing.");
            return;
        }

        NetworkMananger.Instance.Send(new C_TurnEnd().Serialize());
    }

    private void HandleCardUseRequested(CardData card, GameObject cardRoot)
    {
        if (!TryUseCard(card))
        {
            _handUI.RejectCardUse(cardRoot);
            return;
        }

        _handUI.CommitCardUse(cardRoot);
    }

    private bool TryUseCard(CardData card)
    {
        if (!_isPreparing || _readyRequested || card == null) return false;
        if (GameTurnManager.Instance == null || InGameCardManager.Instance == null) return false;
        if (GameTurnManager.Instance.CurrentCost < card.cost) return false;
        if (!InGameCardManager.Instance.PlayerHand.Contains(card)) return false;

        if (card.cardType == CardType.Attack)
        {
            if (!InGameCardManager.Instance.RemoveCardFromHand(card, true)) return false;

            GameTurnManager.Instance.DeductCost(card.cost);
            InGameCardManager.Instance.AddSelectedAttackCard(card);
            _hud.AddUsedAttackCard(card);
            SendSelectedAttackCard(card);
            return true;
        }

        if (card.cardType != CardType.Defense || DefensePlacementManager.Instance == null)
        {
            return false;
        }

        bool placementStarted = DefensePlacementManager.Instance.BeginPlacement(
            card,
            CompleteDefenseCardPlacement,
            HandleDefensePlacementEnded);
        if (!placementStarted) return false;
        if (!InGameCardManager.Instance.RemoveCardFromHand(card, true)) return false;

        GameTurnManager.Instance.DeductCost(card.cost);
        _handUI.SetInteractionLocked(true);
        return true;
    }

    private void CompleteDefenseCardPlacement(CardData card, Vector3 placedPosition)
    {
        InGameCardManager.Instance.AddSelectedDefenseCard(card);
        _hud.AddUsedDefenseCard(card);
        SendDefensePlacement(card, placedPosition);
    }

    private void HandleDefensePlacementEnded()
    {
        if (_readyPendingAfterPlacement)
        {
            _readyPendingAfterPlacement = false;
            CompletePreparationRequest();
            return;
        }

        if (_isPreparing && !_readyRequested)
        {
            _handUI.SetInteractionLocked(false);
        }
    }

    private void SendSelectedAttackCard(CardData card)
    {
        if (GameConfig.ENABLE_TEST_MODE || NetworkMananger.Instance == null) return;

        C_CardSelect packet = new C_CardSelect();
        packet.selectedCardIdss.Add(new C_CardSelect.SelectedCardIds
        {
            cardId = card.cardId
        });
        NetworkMananger.Instance.Send(packet.Serialize());
    }

    private void SendDefensePlacement(CardData card, Vector3 position)
    {
        if (GameConfig.ENABLE_TEST_MODE || NetworkMananger.Instance == null) return;

        C_UnitPlacement packet = new C_UnitPlacement
        {
            cardId = card.cardId,
            x = position.x,
            y = position.y
        };
        NetworkMananger.Instance.Send(packet.Serialize());
    }

    private void SendTurnStartReady()
    {
        if (NetworkMananger.Instance == null)
        {
            Debug.LogError("[BattlePreparationController] NetworkMananger is missing.");
            return;
        }

        C_TurnStartReady packet = new C_TurnStartReady
        {
            ready = true
        };
        NetworkMananger.Instance.Send(packet.Serialize());
    }

    private void SetTurnEndButtonInteractable(bool interactable)
    {
        if (_turnEndButton != null)
        {
            _turnEndButton.interactable = interactable;
        }
    }
}
