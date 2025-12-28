using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class UIPopup : MonoBehaviour
{
    [SerializeField]
    protected GameObject _panel;
    [SerializeField]
    protected Button _btnClose;
    [SerializeField]
    protected float _animationDuration = 0.5f;

    protected virtual void Start()
    {
        _btnClose.onClick.AddListener(ClosePopup);
    }

    public virtual void OpenPopup()
    {
        _panel.SetActive(true);
        _panel.transform.localScale = Vector3.zero;
        _panel.transform.DOScale(Vector3.one, _animationDuration);
    }

    protected virtual void ClosePopup()
    {
        _panel.transform.DOScale(Vector3.zero, _animationDuration)
            .OnComplete(() => _panel.SetActive(false));
    }
}
