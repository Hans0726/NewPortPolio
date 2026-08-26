using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LobbyDeckState
{
    // 플레이어가 소유한 모든 카드의 CardData SO 참조 리스트
    private List<CardData> _ownedPlayerCards = new List<CardData>();
    public IReadOnlyList<CardData> OwnedPlayerCards => _ownedPlayerCards;

    // 플레이어의 현재 덱에 포함된 카드 ID 리스트 (InGameCardState로 전달될 정보)
    private List<short> _currentDeckCardIds = new List<short>();
    public IReadOnlyList<short> CurrentDeckCardIds => _currentDeckCardIds;
    public int NumCardInDeck => _currentDeckCardIds.Count;
    private int _maxDeckSize;
    public int MaxDeckSize => _maxDeckSize;
    public bool IsDeckComplete =>
    _currentDeckCardIds.Count == _maxDeckSize;

    // 덱 구성 변경 시 UIPopup_Deck에 알릴 이벤트
    public event Action Changed;
    public LobbyDeckState(int maxDeckSize)
    {
        _maxDeckSize = maxDeckSize;
    }

    public void Initialize(
    IEnumerable<CardData> ownedCards,
    IEnumerable<short> deckCardIds)
    {
        _ownedPlayerCards.Clear();
        _currentDeckCardIds.Clear();

        _ownedPlayerCards.AddRange(ownedCards);
        _currentDeckCardIds.AddRange(deckCardIds.Take(_maxDeckSize)); // 최대 덱 크기 제한

        Changed?.Invoke();
    }

    public LobbyDeckViewModel CreateViewModel() {return new LobbyDeckViewModel(_ownedPlayerCards, _currentDeckCardIds, _maxDeckSize); }

    public bool TryAddCardToDeck(short cardId)
    {
        if (_currentDeckCardIds.Count >= _maxDeckSize)
        {
            Debug.Log("Deck is full.");
            return false;
        }
        if (!_currentDeckCardIds.Contains(cardId))
        {
            _currentDeckCardIds.Add(cardId);
            Changed?.Invoke();
            return true;
        }
        return false;
    }

    public bool TryRemoveCardFromDeck(short cardId)
    {
        if (_currentDeckCardIds.Contains(cardId))
        {
            _currentDeckCardIds.Remove(cardId);
            Changed?.Invoke(); // UI 갱신 알림
            return true;
        }
        return false; // 덱에 없는 카드
    }


}
public sealed class LobbyDeckViewModel
{
    public IReadOnlyList<CardData> OwnedCards { get; }
    public IReadOnlyList<short> DeckCardIds { get; }
    public int MaxDeckSize { get; }

    public LobbyDeckViewModel(
        IReadOnlyList<CardData> ownedCards,
        IReadOnlyList<short> deckCardIds,
        int maxDeckSize)
    {
        OwnedCards = ownedCards;
        DeckCardIds = deckCardIds;
        MaxDeckSize = maxDeckSize;
    }
}
