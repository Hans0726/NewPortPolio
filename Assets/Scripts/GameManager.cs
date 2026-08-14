using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameConfig
{
    // 에디터에서만 변경 가능한 런타임 플래그
    public static bool ENABLE_TEST_MODE = true;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 로비에서 인게임으로 전달할 덱 정보
    public IReadOnlyList<short> SelectedDeckIds { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }



    // 로비에서 덱 편집 완료 후 호출
    public void SetDeckForNextMatch(IEnumerable<short> deckCardIds)
    {
        SelectedDeckIds = new List<short>(deckCardIds); // 방어적 복사
        Debug.Log($"GameManager: Deck set for next game with {SelectedDeckIds.Count} cards.");
    }

    public void LoadInGameScene()
    {
        StartCoroutine(LoadInGameSceneAsync());
    }

    public void LoadLobbyScene(float waitingTime = 0f)
    {
        waitingTime = Math.Max(0f, waitingTime);
        StartCoroutine(LoadLobbySceneAsync(waitingTime));
    }

    private IEnumerator LoadInGameSceneAsync()
    {
        Debug.Log("Matching success. Moving to InGame scene in 3 seconds...");
        yield return new WaitForSeconds(3f); // 매칭 성공 UI 표시 시간 등

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("B_InGame"); // 씬 이름 확인
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        Debug.Log("InGame scene loaded.");
    }

    private IEnumerator LoadLobbySceneAsync(float waitingTime = 0f)
    {
        yield return new WaitForSeconds(waitingTime);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("A_Lobby"); // 씬 이름 확인
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        Debug.Log("Lobby scene loaded.");
    } 
}
