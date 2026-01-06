using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{

    // CardData를 직접 참조하여 UI를 설정
    private CardData _currentCardData;
    public CardData CurrentCardData => _currentCardData;
    public GameObject RootGameObject { get; private set; } // 자신의 루트 오브젝트 참조
    public RectTransform RectTransform { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }

    [Header("CardUI")]
    [SerializeField] private GameObject _cardDisplayObject; // 카드 전체 모습 (덱에 없을 때)
    [SerializeField] private GameObject _cardInDeckDisplayObject; // 덱에 있을 때 미니 카드 모습

    [Header("CardUI References")]
    [SerializeField] private Image _imgCard;
    [SerializeField] private TextMeshProUGUI _textName;
    [SerializeField] private TextMeshProUGUI _textEffect;
    [SerializeField] private TextMeshProUGUI _textCost;
    [SerializeField] private TextMeshProUGUI _textMoveSpeed;
    [SerializeField] private TextMeshProUGUI _textHealthOrAttack;
    [SerializeField] private TextMeshProUGUI _textDefenseOrAttackSpeed;

    [SerializeField] private Button _btnAddToDeckAction; // 카드 전체 목록에서 덱으로 추가하는 버튼
    [SerializeField] private Button _btnRemoveFromDeckAction; // 덱 목록에서 덱에서 제거하는 버튼

    [Space(5), Header("AttackOff Specifics")]
    [SerializeField] private GameObject _imgAttack;
    [SerializeField] private GameObject _imgAttackSpeed;

    [Space(5), Header("DefenseOff Specifics")]
    [SerializeField] private GameObject _imgHealth;
    [SerializeField] private GameObject _imgDefense;

    [Space(5), Header("CardInDeck Mini Display")]
    [SerializeField] private Image _imgCardMinimize;
    [SerializeField] private TextMeshProUGUI _textCostInDeck;
    [SerializeField] private Text _textNameInDeck;

    [Header("Effects")]
    [SerializeField] private GameObject _playableEffect;

    // 클릭 시 호출될 액션 (UIPopup_Deck에서 설정)
    public Action<CardUI> OnOwnedCardClicked;   // 로비 덱 카드 UI 용
    public Action<CardUI> OnDeckCardClicked;    // 로비 덱 카드 UI 용

    private void Awake()
    {
        if (transform.parent != null) // 안전장치
        {
            RootGameObject = transform.parent.gameObject;
        }

        if (_btnAddToDeckAction != null)
        {
            _btnAddToDeckAction.onClick.AddListener(() => OnOwnedCardClicked?.Invoke(this));
        }
        if (_btnRemoveFromDeckAction != null)
        {
            _btnRemoveFromDeckAction.onClick.AddListener(() => OnDeckCardClicked?.Invoke(this));
        }
        if (_playableEffect != null) _playableEffect.SetActive(false);


        RectTransform = GetComponentInParent<RectTransform>();
        CanvasGroup = GetComponentInParent<CanvasGroup>();
    }

    public void InitializeDisplay(CardData cardData, InGameUIManager uiManager = null)
    {
        _currentCardData = cardData;

        if (_currentCardData == null)
        {
            if (RootGameObject) RootGameObject.SetActive(false);
            return;
        }

        // --- 이미지 로드 및 텍스트 설정 ---
        try {
            Sprite cardSprite = Resources.Load<Sprite>("CardImage/" + _currentCardData.cardName);
            _imgCard.sprite = cardSprite;
            _imgCardMinimize.sprite = cardSprite;
        }
        catch (Exception e) { Debug.LogError($"Error loading image for {_currentCardData.cardName}: {e.Message}"); }

        _textName.text = cardData.cardName;
        _textNameInDeck.text = cardData.cardName;
        _textEffect.text = cardData.specialEffect; // SO에 description 필드 사용 권장
        _textCost.text = cardData.cost.ToString();
        _textCostInDeck.text = cardData.cost.ToString();
        _textMoveSpeed.text = cardData.moveSpeed.ToString();

        bool isAttackCard = cardData.cardType == CardType.Attack; // CardData에 cardType enum 사용

        // 스탯 UI 업데이트
        _textHealthOrAttack.text = isAttackCard ? cardData.health.ToString() : cardData.attack.ToString();
        _textDefenseOrAttackSpeed.text = isAttackCard ? cardData.defense.ToString() : cardData.attackSpeed.ToString();

        // 타입에 따른 UI 요소 활성화/비활성화
        if (_imgHealth) _imgHealth.SetActive(isAttackCard);
        if (_imgDefense) _imgDefense.SetActive(isAttackCard);
        if (_imgAttack) _imgAttack.SetActive(!isAttackCard);
        if (_imgAttackSpeed) _imgAttackSpeed.SetActive(!isAttackCard);
    }


    /// <summary>
    /// 카드가 현재 덱에 포함되어 있는지 여부에 따라 UI 표시 상태를 업데이트
    /// </summary>
    public void UpdateView(bool isInDeckView)
    {
        if (_currentCardData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _cardDisplayObject.SetActive(!isInDeckView);
        _cardInDeckDisplayObject.SetActive(isInDeckView);
    }

    public void SetPlayableState(bool isInteractable)
    {
        if (CanvasGroup == null) return;
        CanvasGroup.interactable = isInteractable; // Canvas Group으로 드래그 등 모든 상호작용 제어
        _playableEffect.SetActive(isInteractable);
    }

    public void SetPlayableState(int currentCost)
    {
        if (CanvasGroup == null) return;
        bool isInteractable = currentCost >= Convert.ToInt32(_textCost.text);
        CanvasGroup.interactable = isInteractable;
        _playableEffect.SetActive(isInteractable);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ★ 드래그 중이면 호버 이벤트 무시
        if (InGameUIManager.Instance != null && !InGameUIManager.Instance.IsDragging)
        {
            InGameUIManager.Instance.SetHoveredCard(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ★ 드래그 중이면 호버 해제 이벤트 무시
        if (InGameUIManager.Instance != null && !InGameUIManager.Instance.IsDragging)
        {
            InGameUIManager.Instance.ClearHoveredCard(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanvasGroup.interactable) return;
        _playableEffect.SetActive(false);
        InGameUIManager.Instance.OnCardBeginDrag(this);
    }
    public void OnDrag(PointerEventData eventData) => InGameUIManager.Instance.OnCardDrag(eventData);
    public void OnEndDrag(PointerEventData eventData) => InGameUIManager.Instance.OnCardEndDrag(this, eventData);
}
