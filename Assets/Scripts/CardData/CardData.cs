using System;
using UnityEngine;


[Serializable]
public class Card
{
    public short cardId;
    public bool isInDeck;
    public string cardName;
    public float moveSpeed;
    public int cost;
    public string specialEffect;
}

[Serializable]
public class AttackCard : Card
{
    public int health;
    public int defense;
}

[Serializable]
public class DefenseCard : Card
{
    public int attack;
    public float attackSpeed;
}


[CreateAssetMenu(fileName = "New CardData", menuName = "Card Data", order = 1)]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public short cardId;                // 카드 고유 ID (서버와 연동 시 중요)
    public string cardName = "카드 이름";
    public float moveSpeed = 1.0f;      // 이동 속도 (공격/방어 공통 가능)
    public int cost = 1;

    public Sprite cardIllustration; // 카드에 표시할 원화
    public Sprite fieldSprite;      // 전투 필드용 도트
    public CardType cardType;

    [Header("Field Visual")]
    public float fieldSpriteScale = 1f;
    public float fieldHitRadius = 1f;

    [Header("공격 카드 스탯")]
    public int health = 10;             // 기본 체력 (공격 카드용)
    public int defense = 0;             // 기본 방어력 (공격 카드용)

    [Header("방어 카드 스탯")]
    public int attack = 5;              // 기본 공격력 (방어 카드용)
    public float attackSpeed = 1.0f;    // 기본 공격 속도 (방어 카드용)
    public bool isFixedDefenseUnit = false;

    [Header("특수 효과")]
    public string specialEffect = ""; // 특수 효과 (문자열 또는 enum 등)

    private void OnEnable()
    {
        if (cardType == CardType.Attack)
        {
            attack = 0;
            attackSpeed = 0;
        }
        else
        {
            health = 0;
            defense = 0;
        }
    }
}

public enum CardType { UnDefined, Attack, Defense }
