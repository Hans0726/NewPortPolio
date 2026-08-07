using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using DG.Tweening;

public class UIPopup_Matching : UIPopup
{
    public event Action MatchingRequest;
    public event Action MatchingCancelRequest;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OpenPopup()
    {
        base.OpenPopup(true);
        MatchingRequest?.Invoke();
    }

    protected override void ClosePopup()
    {
        base.ClosePopup();
        MatchingCancelRequest?.Invoke();
    }

    public void SetElapsedTime(TimeSpan elapsed)
    {
        _text.text =
            $"매칭 중...\n{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    public void UpdateMatchingStatusText(string txt)
    {
        _text.text = txt;
    }

    public void SetCancelInteractable()
    {
        _btnClose.interactable = false;
    }
}
