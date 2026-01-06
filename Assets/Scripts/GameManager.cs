using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public static class GameConfig
{
    // 에디터에서만 변경 가능한 런타임 플래그
    public static bool ENABLE_TEST_MODE = true;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // 로비에서 인게임으로 전달할 덱 정보
    private List<short> _playerDeckToCarryOver;
    public List<short> PlayerDeckToCarryOver => _playerDeckToCarryOver;


    [Header("In-Game Start Animation UI")]
    public GameObject startSequencePanel;           // 반투명 검은색 패널 (인스펙터에서 할당)
    public TextMeshProUGUI startSequenceText;       // "제한 시간 내에..." 텍스트 (인스펙터에서 할당)
                                                    // Canvas Group을 사용하면 패널과 텍스트 알파를 한 번에 제어하기 좋음
    public CanvasGroup startSequenceCanvasGroup;    // 패널의 Canvas Group (알파 제어용)
    public float startSequenceFadeDuration = 1.0f;
    public float startSequenceDisplayDuration = 2.0f;

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
    public void SetDeckForNextGame(List<short> deckCardIds)
    {
        _playerDeckToCarryOver = new List<short>(deckCardIds); // 방어적 복사
        Debug.Log($"GameManager: Deck set for next game with {_playerDeckToCarryOver.Count} cards.");
    }

    public void MatchingSuccess()
    {
        // 로비에서 최종 덱 정보를 가져와서 설정 (예시: LobbyCardManager에서 호출)
        if (LobbyCardManager.Instance != null)
        {
            SetDeckForNextGame(LobbyCardManager.Instance.CurrentDeckCardIds);
        }
        else
        {
            Debug.LogError("LobbyCardManager instance not found when trying to set deck for game!");
        }

        UIPopup_Matching.MatchingEvent.TriggerMatchingStatusChanged("매칭 성공");
        StartCoroutine(LoadInGameSceneAndInitialize()); // 함수 이름 변경
    }

    public void MatchingReqOk()
    {
        UIPopup_Matching.MatchingEvent.TriggerMatchingStatusChanged("매칭 시작");
    }

    private IEnumerator LoadInGameSceneAndInitialize()
    {
        Debug.Log("Matching success. Moving to InGame scene in 3 seconds...");
        yield return new WaitForSeconds(3f); // 매칭 성공 UI 표시 시간 등

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("B_InGame"); // 씬 이름 확인
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        Debug.Log("InGame scene loaded.");


        // 1. 데이터 매니저(CardManager) 먼저 초기화
        if (InGameCardManager.Instance != null)
        {
            InGameCardManager.Instance.Initialize(_playerDeckToCarryOver);
        }
        else Debug.LogError("InGameCardManager instance not found!");


        // 2. UI 매니저 초기화 (이벤트 구독)
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.Initialize();
        }
        else Debug.LogError("InGameUIManager instance not found!");


        // 3. 오프닝 시퀀스 시작
        // 오프닝 시퀀스가 끝나면 그 OnComplete 콜백에서 InGameCardManager.Instance.DrawInitialHand()를 호출
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowOpeningSequence(); // ShowOpeningSequence 내부에서 DrawInitialHand를 트리거하도록 수정
        }
    }
}