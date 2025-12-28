using System.Collections.Generic;

public class CardManager
{
    private List<CardData> _deck = new List<CardData>(); // 실제 게임에 사용할 덱
    private List<CardData> _hand = new List<CardData>(); // 현재 손에 들고 있는 카드
    public List<CardData> Hand { get { return _hand; } } // 다른 스크립트(UI 등)에서 핸드 정보를 읽기 위한 프로퍼티
    private List<CardData> _discardPile = new List<CardData>();


    /// <summary>
    /// 게임 시작 시 서버로부터 받은 덱 카드 ID 리스트를 기반으로 _deck 리스트를 채우는 함수
    /// </summary>
    /// <param name="deckCardIds"></param>
    public void InitializeDeck(List<short> deckCardIds)
    {

    }

    /// <summary>
    /// _deck 리스트를 랜덤하게 섞습니다.System.Random이나 UnityEngine.Random을 사용하여 구현(Fisher-Yates 알고리즘 등)
    /// </summary>
    public void ShuffleDeck()
    {

    }

    /// <summary>
    /// _deck에서 지정된 수만큼 카드를 뽑아 _hand 리스트에 추가하고, _deck에서는 제거합니다.덱이 비면 _discardPile을 섞어 _deck으로 가져오는 로직도 추가할 수 있습니다.
    /// </summary>
    public void DrawCards(int count)
    {

    }

    /// <summary>
    ///  핸드에서 카드를 사용하는 로직(핸드에서 제거, 필요시 _discardPile에 추가 등)
    /// </summary>
    public void PlayCard(CardData card)
    {

    }
}