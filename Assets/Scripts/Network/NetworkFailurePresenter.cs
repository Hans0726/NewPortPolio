using System.Collections;
using UnityEngine;

public sealed class NetworkFailurePresenter : MonoBehaviour
{
    private const string FailureMessage =
        "서버에 연결할 수 없거나 연결이 끊어졌습니다.\n게임을 종료합니다.";

    private NetworkMananger _networkManager;
    private bool _isFailureBeingShown;

    public void Initialize(NetworkMananger networkManager)
    {
        if (_networkManager == networkManager)
            return;

        Unsubscribe();
        _networkManager = networkManager;
        _networkManager.ConnectionFailed += HandleConnectionFailed;
    }

    private void HandleConnectionFailed()
    {
        if (_isFailureBeingShown)
            return;

        _isFailureBeingShown = true;
        StartCoroutine(ShowFailurePopupWhenReady());
    }

    private IEnumerator ShowFailurePopupWhenReady()
    {
        while (GameManager.Instance == null || GameObject.Find("Canvas") == null)
            yield return null;

        GameManager.Instance.ShowWarningPopup(FailureMessage, QuitApplication);
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (_networkManager != null)
            _networkManager.ConnectionFailed -= HandleConnectionFailed;

        _networkManager = null;
    }
}
