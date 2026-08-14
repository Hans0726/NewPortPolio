using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CardPlayController : IDisposable
{
    private readonly struct PendingDefensePlacement
    {
        public PendingDefensePlacement(CardData card, Vector3 groundPosition)
        {
            Card = card;
            GroundPosition = groundPosition;
        }

        public CardData Card { get; }
        public Vector3 GroundPosition { get; }
    }

    private readonly List<CardData> _opponentAttackCards = new List<CardData>();
    private readonly List<PendingDefensePlacement> _pendingOpponentDefensePlacements =
        new List<PendingDefensePlacement>();

    private InGameMatchState _matchState;
    private InGameCardState _cardState;
    private InGameHandUI _handView;
    private InGameHUDView _hudView;
    private DefensePlacementManager _placementService;
    private NetworkGateway _gateway;
    private bool _preparationActive;
    private bool _combatActive;
    private bool _initialized;

    public event Action PlacementFinished;

    public bool IsPlacementActive =>
        _placementService != null && _placementService.IsPlacing;
    public IReadOnlyList<CardData> OpponentAttackCards => _opponentAttackCards;

    public void Initialize(
        InGameMatchState matchState,
        InGameCardState cardState,
        InGameHandUI handView,
        InGameHUDView hudView,
        DefensePlacementManager placementService,
        NetworkGateway gateway)
    {
        if (_initialized) return;

        _matchState = matchState;
        _cardState = cardState;
        _handView = handView;
        _hudView = hudView;
        _placementService = placementService;
        _gateway = gateway;

        _handView.CardUseRequested += HandleCardUseRequested;
        if (_gateway != null)
        {
            _gateway.OpponentCardSelectionReceived += HandleOpponentCardSelection;
            _gateway.OpponentDefensePlacementReceived += HandleOpponentDefensePlacement;
        }

        _initialized = true;
    }

    public void SetPreparationActive(bool active)
    {
        _preparationActive = active;
    }

    public void EnterCombat()
    {
        _combatActive = true;

        for (int i = 0; i < _pendingOpponentDefensePlacements.Count; i++)
        {
            PendingDefensePlacement placement = _pendingOpponentDefensePlacements[i];
            _placementService?.PlaceRemoteDefenseUnit(
                placement.Card,
                placement.GroundPosition);
        }

        _pendingOpponentDefensePlacements.Clear();
    }

    public void ExitCombat()
    {
        _combatActive = false;
    }

    public void Dispose()
    {
        if (!_initialized) return;

        _handView.CardUseRequested -= HandleCardUseRequested;
        if (_gateway != null)
        {
            _gateway.OpponentCardSelectionReceived -= HandleOpponentCardSelection;
            _gateway.OpponentDefensePlacementReceived -= HandleOpponentDefensePlacement;
        }

        _pendingOpponentDefensePlacements.Clear();
        _combatActive = false;
        _initialized = false;
    }

    private void HandleCardUseRequested(CardData card, GameObject cardRoot)
    {
        if (!TryUseCard(card))
        {
            _handView.RejectCardUse(cardRoot);
            return;
        }

        _handView.CommitCardUse(cardRoot);
    }

    private bool TryUseCard(CardData card)
    {
        if (!_preparationActive || card == null) return false;
        if (!_cardState.ContainsInHand(card)) return false;
        if (_matchState.CurrentCost < card.cost) return false;

        if (card.cardType == CardType.Attack)
        {
            if (!_cardState.RemoveCardFromHand(card)) return false;
            if (!_matchState.TrySpendCost(card.cost)) return false;

            _cardState.AddSelectedAttackCard(card);
            _hudView.AddUsedAttackCard(card);
            if (!GameConfig.ENABLE_TEST_MODE)
            {
                _gateway?.SendSelectedAttackCard(card.cardId);
            }
            return true;
        }

        if (card.cardType != CardType.Defense || _placementService == null)
        {
            return false;
        }

        bool placementStarted = _placementService.BeginPlacement(
            card,
            CompleteDefensePlacement,
            HandlePlacementEnded);
        if (!placementStarted) return false;
        if (!_cardState.RemoveCardFromHand(card)) return false;
        if (!_matchState.TrySpendCost(card.cost)) return false;

        _handView.SetInteractionLocked(true);
        return true;
    }

    private void CompleteDefensePlacement(CardData card, Vector3 groundPosition)
    {
        _cardState.AddSelectedDefenseCard(card);
        _hudView.AddUsedDefenseCard(card);
        if (!GameConfig.ENABLE_TEST_MODE)
        {
            _gateway?.SendDefensePlacement(card.cardId, groundPosition.x, groundPosition.y);
        }
    }

    private void HandlePlacementEnded()
    {
        PlacementFinished?.Invoke();
    }

    private void HandleOpponentCardSelection(S_CardSelectResult packet)
    {
        if (packet == null) return;

        foreach (S_CardSelectResult.SelectedCardIds selectedCard in packet.selectedCardIdss)
        {
            CardData card = _cardState.GetCardDataById(selectedCard.cardId);
            if (card != null && card.cardType == CardType.Attack)
            {
                _opponentAttackCards.Add(card);
            }
        }
    }

    private void HandleOpponentDefensePlacement(S_UnitPlacementResult packet)
    {
        if (packet == null || !packet.isSuccess || _placementService == null) return;

        CardData card = _cardState.GetCardDataById(packet.cardId);
        if (card == null || card.cardType != CardType.Defense) return;

        Vector3 localGroundPosition = new Vector3(-packet.x, -packet.y, 0f);
        if (_combatActive)
        {
            _placementService.PlaceRemoteDefenseUnit(card, localGroundPosition);
            return;
        }

        _pendingOpponentDefensePlacements.Add(
            new PendingDefensePlacement(card, localGroundPosition));
    }
}
