using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class InGameCardState : MonoBehaviour
{
    [Header("Card Database")]
    [SerializeField] private CardDatabase _cardDatabase;
    [FormerlySerializedAs("InitialHandSize")]
    [SerializeField] private int _initialHandSize = 3;

    private readonly List<CardData> _playerDeck = new List<CardData>();
    private readonly List<CardData> _playerHand = new List<CardData>();
    private readonly List<CardData> _playerDiscardPile = new List<CardData>();
    private readonly List<CardData> _selectedAttackCards = new List<CardData>();
    private readonly List<CardData> _selectedDefenseCards = new List<CardData>();

    public IReadOnlyList<CardData> PlayerDeck => _playerDeck;
    public IReadOnlyList<CardData> PlayerHand => _playerHand;
    public IReadOnlyList<CardData> PlayerDiscardPile => _playerDiscardPile;
    public IReadOnlyList<CardData> SelectedAttackCards => _selectedAttackCards;
    public IReadOnlyList<CardData> SelectedDefenseCards => _selectedDefenseCards;

    private void Awake()
    {
        _cardDatabase?.Initialize();
    }

    public void Initialize(IReadOnlyList<short> deckCardIds)
    {
        ClearAllCards();
        _cardDatabase?.Initialize();

        if (_cardDatabase == null)
        {
            Debug.LogError("[InGameCardState] CardDatabase is missing.");
            return;
        }

        if (deckCardIds == null || deckCardIds.Count == 0)
        {
            Debug.LogError("[InGameCardState] Deck card ids are empty.");
            return;
        }

        foreach (short cardId in deckCardIds)
        {
            CardData card = _cardDatabase.GetCardDataById(cardId);
            if (card != null)
            {
                _playerDeck.Add(card);
            }
        }

        ShuffleDeck();
    }

    public void InitializeForTest(int deckSize = 10)
    {
        ClearAllCards();
        _cardDatabase?.Initialize();

        if (_cardDatabase == null)
        {
            Debug.LogError("[InGameCardState] CardDatabase is missing.");
            return;
        }

        int attempts = 0;
        while (_playerDeck.Count < deckSize && attempts++ < deckSize * 20)
        {
            short cardId = UnityEngine.Random.value < 0.5f
                ? (short)UnityEngine.Random.Range(0, 8)
                : (short)UnityEngine.Random.Range(100, 102);
            CardData card = _cardDatabase.GetCardDataById(cardId);
            if (card == null) continue;

            if (card.cardType == CardType.Defense && _playerDeck.Contains(card))
            {
                continue;
            }

            _playerDeck.Add(card);
        }

        ShuffleDeck();
    }

    public CardData GetCardDataById(short cardId)
    {
        return _cardDatabase != null ? _cardDatabase.GetCardDataById(cardId) : null;
    }

    public void DrawInitialHand()
    {
        DrawCards(_initialHandSize);
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            RefillDeckIfNeeded();
            if (_playerDeck.Count == 0) break;

            CardData card = _playerDeck[0];
            _playerDeck.RemoveAt(0);
            _playerHand.Add(card);
        }

    }

    public bool ContainsInHand(CardData card)
    {
        return card != null && _playerHand.Contains(card);
    }

    public bool RemoveCardFromHand(CardData card)
    {
        if (card == null || !_playerHand.Remove(card)) return false;

        _playerDiscardPile.Add(card);
        return true;
    }

    public void AddSelectedAttackCard(CardData card)
    {
        if (card != null) _selectedAttackCards.Add(card);
    }

    public void AddSelectedDefenseCard(CardData card)
    {
        if (card != null) _selectedDefenseCards.Add(card);
    }

    public void PrepareNextRound()
    {
        // Used cards remain in the discard pile until the draw deck is exhausted.
    }

    private void RefillDeckIfNeeded()
    {
        if (_playerDeck.Count > 0 || _playerDiscardPile.Count == 0) return;

        _playerDeck.AddRange(_playerDiscardPile);
        _playerDiscardPile.Clear();
        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        for (int i = _playerDeck.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (_playerDeck[i], _playerDeck[swapIndex]) = (_playerDeck[swapIndex], _playerDeck[i]);
        }
    }

    private void ClearAllCards()
    {
        _playerDeck.Clear();
        _playerHand.Clear();
        _playerDiscardPile.Clear();
        _selectedAttackCards.Clear();
        _selectedDefenseCards.Clear();
    }
}
