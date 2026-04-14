using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIPopup_CardSelect : UIPopup
{
    [SerializeField] private Transform _cardDisplayContainer; // 팝업 내부 카드 표시 영역
    [SerializeField] private Button _btnAddToDeck;
    [SerializeField] private Button _btnDraw;

    private GameObject _displayedCardInstance; // 팝업에 표시 중인 카드 복제본
    private Action<CardData, bool> _onChoice; // card, isDraw(true) or addToDeck(false)

    protected override void Start()
    {
        base.Start(); // UIPopup의 Start 호출 (_btnClose 설정)

        _btnAddToDeck.onClick.AddListener(OnAddToDeckClicked);
        _btnDraw.onClick.AddListener(OnDrawClicked);
    }

    public void OpenPopup(CardUI cardUI, Action<CardData, bool> onChoice)
    {
        _onChoice = onChoice;
        cardUI.RootGameObject.SetActive(false); // 원본 카드 숨김

        // ★ 기존 복제본 정리
        if (_displayedCardInstance != null)
            Destroy(_displayedCardInstance);

        // ★ 부모 없이 생성한 후 수동으로 배치
        _displayedCardInstance = Instantiate(cardUI.RootGameObject);
        _displayedCardInstance.name = "DisplayedCard_Copy";
        _displayedCardInstance.SetActive(true);

        // ★ RectTransform 초기화 및 Stretch 설정
        RectTransform displayRect = _displayedCardInstance.GetComponent<RectTransform>();

        // ★ 부모로 설정
        displayRect.SetParent(_cardDisplayContainer, false);

        // 1. localPosition 초기화
        displayRect.localPosition = Vector3.zero;
        displayRect.localScale =    Vector3.one; // 스케일 초기화
        _displayedCardInstance.transform.GetChild(0).localScale = Vector3.one; // 자식 카드의 스케일도 초기화

        // ★ 상호작용 비활성화
        CanvasGroup displayCanvasGroup = _displayedCardInstance.GetComponent<CanvasGroup>();
        if (displayCanvasGroup != null)
            displayCanvasGroup.interactable = false;

        base.OpenPopup();
    }

    private void OnAddToDeckClicked()
    {
        _onChoice?.Invoke(_displayedCardInstance.GetComponent<CardUI>().CurrentCardData, false);
        base.ClosePopup();
    }

    private void OnDrawClicked()
    {
        _onChoice?.Invoke(_displayedCardInstance.GetComponent<CardUI>().CurrentCardData, true);
        base.ClosePopup();
    }

    protected override void ClosePopup()
    {
        base.ClosePopup();
        
        // 팝업 닫을 때 복제본 정리
        if (_displayedCardInstance != null)
            Destroy(_displayedCardInstance);
    }
}