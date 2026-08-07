using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using TMPro;

public class UIPopup : MonoBehaviour
{
    [SerializeField]
    protected GameObject _panel;
    [SerializeField]
    protected Button _btnClose;
    [SerializeField]
    protected TextMeshProUGUI _text;
    public TextMeshProUGUI Text { get => _text; set => _text = value; }
    [SerializeField]
    protected float _animationDuration = 0.5f;
    private GameObject _blockingPanel = null;
    public event Action OnPopupOpened;
    protected virtual void Awake()
    {
        _btnClose.onClick.AddListener(ClosePopup);
        _text.text = string.Empty;
    }

    /// <summary>
    /// 블로킹 패널을 전체 화면 크기로 설정 (On/Off 가능)
    /// </summary>
    protected void SetupBlockingPanel(bool enableBlocking)
    {
        // 이미 셋업되어있다면 리턴
        if (_blockingPanel != null && _blockingPanel.gameObject.activeSelf == true)
            return;


        string className = this.GetType().Name;

        _blockingPanel = transform.parent.Find("BlockingPanel").gameObject;

        // BlockingPanel 널 체크
        if (_blockingPanel == null)
        {
            Debug.LogWarning($"[{className}] BlockingPanel이 할당되지 않았습니다!");
            return;
        }

        _blockingPanel.SetActive(true);
    }

    public virtual void OpenPopup()
    {
        _panel.SetActive(true);

        Vector3 openedScale = new Vector3(1f, 1f, 1f);

        _panel.transform.DOKill();
        _panel.transform.localScale = Vector3.zero;
        _panel.transform.DOScale(openedScale, _animationDuration);
    }

    public virtual void OpenPopup(string message, float targetScale = 1f)
    {
        _text.text = message;
        _panel.SetActive(true);

        Vector3 openedScale = new Vector3(targetScale, targetScale, 1f);

        _panel.transform.DOKill();
        _panel.transform.localScale = Vector3.zero;
        _panel.transform.DOScale(openedScale, _animationDuration);
    }


    public virtual void OpenPopup(bool enableBlocking = false)
    {
        _panel.SetActive(true);
        SetupBlockingPanel(enableBlocking);

        _panel.transform.localScale = Vector3.zero;
        _panel.transform.DOScale(Vector3.one, _animationDuration);
        OnPopupOpened?.Invoke();
    }

    protected virtual void ClosePopup()
    {
        _panel.transform.DOScale(Vector3.zero, _animationDuration)
            .OnComplete(() =>
            {
                _panel.SetActive(false);
                _blockingPanel.SetActive(false);
            });
    }
}
