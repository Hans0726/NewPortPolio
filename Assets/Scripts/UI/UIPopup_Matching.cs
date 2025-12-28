using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using DG.Tweening;

public class UIPopup_Matching : UIPopup
{
    [SerializeField] Text _txtMatching;
    private Coroutine _timerCoroutine;
    private TimeSpan _currentTime;
    private bool _isMatching = true;

    protected override void Start()
    {
        base.Start();
        _timerCoroutine = StartCoroutine(UpdateTimer());
    }

    public override void OpenPopup()
    {
        base.OpenPopup();
        MatchingEvent.OnMatchingStatusChanged += UpdateStatusText;

        C_PlayerMatchingReq matchingReq = new C_PlayerMatchingReq();
        NetworkMananger.Instance.Send(matchingReq.Serialize());
    }

    protected override void ClosePopup()
    {
        base.ClosePopup();
        StopCoroutine(_timerCoroutine);
        _timerCoroutine = null;
        MatchingEvent.OnMatchingStatusChanged -= UpdateStatusText;

        C_PlayerMatchingReqCancel matchingReqCancel = new C_PlayerMatchingReqCancel();
        NetworkMananger.Instance.Send(matchingReqCancel.Serialize());
    }

    private IEnumerator UpdateTimer()
    {
        while (_isMatching)
        {
            yield return new WaitForSeconds(1f);
            _currentTime = _currentTime.Add(TimeSpan.FromSeconds(1));
            _txtMatching.text = string.Format("¸ÅÄª Áß...\n{0:D2}:{1:D2}", _currentTime.Minutes, _currentTime.Seconds);
        }

        yield return new WaitForSeconds(1f);
        ClosePopup();
    }

    public static class MatchingEvent
    {
        public delegate void MatchingStatusHandler(string txt);
        public static event MatchingStatusHandler OnMatchingStatusChanged;

        public static void TriggerMatchingStatusChanged(string txt)
        {
            OnMatchingStatusChanged?.Invoke(txt);
        }
    }

    private void UpdateStatusText(string txt)
    {
        _txtMatching.text = txt;

        if (txt.Contains("¼º°ø"))
        {
            StopCoroutine(_timerCoroutine);
            _isMatching = false;
        }
            
    }
}
