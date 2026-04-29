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
    private CardUI _originalCardUI; // 원본 카드 UI 참조
    private Transform _originalParent; // ★ 원본 카드의 부모 (핸드 컨테이너)
    private int _originalSiblingIndex; // ★ 원본 카드의 sibling index (n번째 위치)
    private GameObject _draggedCardRoot; // ★ 드래그된 카드의 루트 오브젝트 (복원 시 사용)
    private Action<CardData, bool> _onChoice; // card, isDraw(true) or addToDeck(false)

    protected override void Start()
    {
        base.Start(); // UIPopup의 Start 호출 (_btnClose 설정)

        _btnAddToDeck.onClick.AddListener(OnAddToDeckClicked);
        _btnDraw.onClick.AddListener(OnDrawClicked);
    }

    /// <summary>
    /// ★ 팝업을 열기 전에 카드의 원본 부모 정보를 미리 설정합니다.
    /// (드래그 전의 정확한 위치 정보를 보존하기 위함)
    /// </summary>
    public void SetOriginalCardInfo(Transform originalParent, int siblingIndex, CardUI draggedCardUI)
    {
        _originalParent = originalParent;
        _originalSiblingIndex = siblingIndex;
        _draggedCardRoot = draggedCardUI.RootGameObject; // ★ 카드 루트 객체 저장
    }

    public void OpenPopup(CardUI cardUI, Action<CardData, bool> onChoice)
    {
        _onChoice = onChoice;
        _originalCardUI = cardUI; // ★ 원본 카드 UI 저장
        
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
        float scaleFactor = _cardDisplayContainer.GetComponent<RectTransform>().rect.height / displayRect.rect.height;
        displayRect.localPosition = Vector3.zero;
        displayRect.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        _displayedCardInstance.transform.GetChild(0).localScale = Vector3.one; // 자식 카드의 스케일도 초기화
        
        // ★ CardUI 컴포넌트의 이벤트 핸들러를 비활성화하여 호버 이벤트 방지
        CardUI displayedCardUI = _displayedCardInstance.GetComponentInChildren<CardUI>();
        if (displayedCardUI != null)
        {
            displayedCardUI.enabled = false; // CardUI 스크립트 비활성화
        }

        base.OpenPopup(true);
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
        
        // ★ 원본 카드를 원래 위치로 복원 (취소한 경우)
        if (_originalCardUI != null && _originalCardUI.RootGameObject != null && _originalParent != null)
        {
            // 원본 카드를 원래 부모로 돌려보냄
            _originalCardUI.RootGameObject.transform.SetParent(_originalParent, false);
            // 원래 sibling index로 복원 (n번째 위치)
            _originalCardUI.RootGameObject.transform.SetSiblingIndex(_originalSiblingIndex);
            // 원본 카드 다시 활성화
            _originalCardUI.RootGameObject.SetActive(true);
            
            // ★ InGameUIManager의 _activeHandCardRoots에 다시 추가
            InGameUIManager.Instance.RestoreCardToHand(_draggedCardRoot, _originalSiblingIndex);
         }
        
        // 팝업 닫을 때 복제본 정리
        if (_displayedCardInstance != null)
            Destroy(_displayedCardInstance);
    }
}