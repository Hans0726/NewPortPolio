using System.Collections.Generic;

// Connection-lifetime card data, separate from the lobby's editable deck.
public sealed class PlayerCardState
{
    private List<(short cardId, bool isInDeck)> _cards;

    public void Replace(IEnumerable<(short cardId, bool isInDeck)> cards)
    {
        _cards = new List<(short cardId, bool isInDeck)>(cards);
    }

    // A copy prevents lobby edits from changing the last received/submitted deck.
    public bool TryGetSnapshot(out List<(short cardId, bool isInDeck)> cards)
    {
        cards = _cards == null
            ? null
            : new List<(short cardId, bool isInDeck)>(_cards);
        return cards != null;
    }
}
