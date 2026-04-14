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

        //// ★ 기존 복제본 정리
        //if (_displayedCardInstance != null)
        //    Destroy(_displayedCardInstance);

        //// ★ 부모 없이 생성한 후 수동으로 배치
        //_displayedCardInstance = Instantiate(cardUI.RootGameObject);
        //_displayedCardInstance.name = "DisplayedCard_Copy";

        //// ★ RectTransform 초기화
        //RectTransform displayRect = _displayedCardInstance.GetComponent<RectTransform>();

        //// ★ 부모로 설정 (DontDestroyOnLoad 문제 회피)
        //displayRect.SetParent(_cardDisplayContainer, false);
        //displayRect.anchoredPosition = Vector2.zero; // 컨테이너 중앙
        //displayRect.localScale = new Vector3(0.5f, 0.5f, 1f); // 반으로 축소

        //// ★ 상호작용 비활성화
        //CanvasGroup displayCanvasGroup = _displayedCardInstance.GetComponent<CanvasGroup>();
        //if (displayCanvasGroup != null)
        //    displayCanvasGroup.interactable = false;

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