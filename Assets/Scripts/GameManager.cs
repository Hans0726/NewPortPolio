using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameConfig
{
    // 에디터에서만 변경 가능한 런타임 플래그
    public static bool ENABLE_TEST_MODE = true;
}

public enum GameSfx
{
    Button,
    CardDraw,
    CardUse,
    Placement,
    Attack,
    HeavyAttack,
    MagicAttack,
    Hit,
    Death,
    RoundStart,
    Victory,
    Defeat
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 로비에서 인게임으로 전달할 덱 정보
    public IReadOnlyList<short> SelectedDeckIds { get; private set; }

    private readonly Dictionary<GameSfx, AudioClip> _sfxClips = new Dictionary<GameSfx, AudioClip>();
    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private string _configuredScenePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ConfigureAudioSources();
        LoadAudioClips();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    public void PlaySfx(GameSfx sound, float volumeScale = 1f)
    {
        if (_sfxSource == null || !_sfxClips.TryGetValue(sound, out AudioClip clip) || clip == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayBgm(string resourceName)
    {
        if (_bgmSource == null || string.IsNullOrWhiteSpace(resourceName)) return;

        AudioClip clip = Resources.Load<AudioClip>($"Audio/BGM/{resourceName}");
        if (clip == null || (_bgmSource.clip == clip && _bgmSource.isPlaying)) return;

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    private void ConfigureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        foreach (AudioSource source in sources)
        {
            string groupName = source.outputAudioMixerGroup != null
                ? source.outputAudioMixerGroup.name
                : string.Empty;

            if (groupName == "BGM") _bgmSource = source;
            if (groupName == "SFX") _sfxSource = source;
        }

        _bgmSource ??= gameObject.AddComponent<AudioSource>();
        _sfxSource ??= gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _sfxSource.playOnAwake = false;
    }

    private void LoadAudioClips()
    {
        foreach (GameSfx sound in Enum.GetValues(typeof(GameSfx)))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{sound}");
            if (clip != null) _sfxClips[sound] = clip;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_configuredScenePath == scene.path) return;
        _configuredScenePath = scene.path;

        PlayBgm(scene.name == "B_InGame" ? "Battle" : "Lobby");

        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            button.onClick.AddListener(() => PlaySfx(GameSfx.Button, 0.65f));
        }
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

    public void ShowWarningPopup(string message, Action action = null)
    {
        GameObject mainCanvas = GameObject.Find("Canvas").gameObject;
        GameObject warningPopup = mainCanvas.transform.Find("UIPopup_Warning")?.gameObject;
        if (warningPopup == null)
        {
            warningPopup = Instantiate(Resources.Load<GameObject>("Prefabs/UIPopup"), mainCanvas.transform);
            warningPopup.name = "UIPopup_Warning";
        }

        UIPopup warning = warningPopup.GetComponent<UIPopup>();
        warningPopup.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0f);
        warning.OpenPopup(
            message,
            0.5f);
        warning.ChangeClosedEvent(action);
    }
}
