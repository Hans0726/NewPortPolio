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


// 이 속성을 추가해야 유니티 에디터에서 에셋 파일 생성이 가능해집니다!
[CreateAssetMenu(fileName = "New CardData", menuName = "Card Data", order = 1)]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public short cardId;                // 카드 고유 ID (서버와 연동 시 중요)
    public string cardName = "카드 이름";
    public float moveSpeed = 1.0f;      // 이동 속도 (공격/방어 공통 가능)
    public int cost = 1;

    public Sprite cardImage;            // 카드 이미지
    public CardType cardType;

    [Header("공격 카드 스탯")]
    public int health = 10;             // 기본 체력 (공격 카드용)
    public int defense = 0;             // 기본 방어력 (공격 카드용)

    [Header("방어 카드 스탯")]
    public int attack = 5;              // 기본 공격력 (방어 카드용)
    public float attackSpeed = 1.0f;    // 기본 공격 속도 (방어 카드용)

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
