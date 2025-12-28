using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("시작 메뉴")]
    [SerializeField] private Button btnGameStart;
    [SerializeField] private Button btnDeck;
    [SerializeField] private Button btnOption;
    [SerializeField] private Button btnQuit;

    [Space(5), Header("옵션")]
    [SerializeField] private UIPopup_Matching gameStart;
    [SerializeField] private UIPopup option;
    [SerializeField] private UIPopup_Deck deck;
    
    [SerializeField]
    private Dropdown dropdownDisplayMode;

    [SerializeField] private Dropdown dropdownResolution;
    private Resolution[] resolutions;
    private List<string> resolutionOptions = new List<string>();

    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Slider sliderMasterVolume;
    [SerializeField] private Toggle toggleMasterMute;

    [SerializeField] private Slider sliderBgmVolume;
    [SerializeField] private Toggle toggleBgmMute;

    [SerializeField] private Slider sliderSfxVolume;
    [SerializeField] private Toggle toggleSfxMute;

    private void Start()
    {
        btnGameStart.onClick.AddListener(gameStart.OpenPopup);
        btnDeck.onClick.AddListener(deck.OpenPopup);
        btnOption.onClick.AddListener(option.OpenPopup);
        btnQuit.onClick.AddListener(Application.Quit);

        SetupDropdowns();
        LoadVolumeSettings();

        sliderMasterVolume.onValueChanged.AddListener((float val) => { SetVolume(val, "Master"); });
        toggleMasterMute.onValueChanged.AddListener((bool isOn) => { SetMute(isOn, "Master"); });
        sliderBgmVolume.onValueChanged.AddListener((float val) => { SetVolume(val, "BGM"); });
        toggleBgmMute.onValueChanged.AddListener((bool isOn) => { SetMute(isOn, "BGM"); });
        sliderSfxVolume.onValueChanged.AddListener((float val) => { SetVolume(val, "SFX"); });
        toggleSfxMute.onValueChanged.AddListener((bool isOn) => { SetMute(isOn, "SFX"); });
    }

    #region 화면모드, 해상도
    private void SetupDropdowns()
    {
        dropdownDisplayMode.onValueChanged.AddListener(SetDisplayMode);

        // resolutions는 여기서 한 번만 가져와서 사용 (멤버 변수에 저장해두고 재활용)
        resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0)
        {
            Debug.LogError("Screen.resolutions returned no resolutions!");
            return;
        }

        dropdownResolution.options.Clear(); // 기존 옵션 모두 제거

        List<string> uniqueResolutionStrings = new List<string>();
        List<Resolution> uniqueResolutionsForSelection = new List<Resolution>(); // 실제 선택에 사용할 Resolution 객체 리스트

        // 해상도를 역순으로 순회하면서 고유한 "너비 x 높이" 문자열만 추가
        // Screen.resolutions는 보통 낮은 해상도부터 높은 해상도 순으로 정렬되어 있음
        // 뒤에서부터 순회하면 같은 해상도 중 가장 높은 주사율을 가진 것을 먼저 만나게 될 가능성이 높음 (항상 보장되진 않음)
        for (int i = resolutions.Length - 1; i >= 0; i--)
        {
            Resolution currentRes = resolutions[i];
            string option = currentRes.width + " x " + currentRes.height;

            // 이미 추가된 "너비 x 높이" 문자열인지 확인
            if (!uniqueResolutionStrings.Contains(option))
            {
                uniqueResolutionStrings.Add(option);
                uniqueResolutionsForSelection.Add(currentRes); // 해당 Resolution 객체도 저장
            }
        }

        // 유니티 에디터에서는 resolutions 순서가 빌드와 다를 수 있으므로,
        // 문자열 리스트를 다시 정렬하거나, uniqueResolutionsForSelection를 너비/높이 기준으로 정렬할 수 있음.
        // 여기서는 일단 추가된 순서대로 사용 (보통 높은 해상도가 먼저 추가됨)
        // 필요하다면 uniqueResolutionStrings.Sort() 또는 Reverse() 등을 사용

        dropdownResolution.AddOptions(uniqueResolutionStrings); // 드롭다운에 고유한 해상도 문자열 추가
        dropdownResolution.onValueChanged.RemoveAllListeners(); // 기존 리스너 제거 (중복 방지)
        dropdownResolution.onValueChanged.AddListener(index => SetResolutionByIndex(index, uniqueResolutionsForSelection)); // 수정된 리스너 연결

        // 현재 해상도에 맞는 드롭다운 값 설정 (선택 사항)
        int currentResolutionIndex = -1;
        string currentScreenOption = Screen.currentResolution.width + " x " + Screen.currentResolution.height;
        for (int i = 0; i < uniqueResolutionStrings.Count; ++i)
        {
            if (uniqueResolutionStrings[i] == currentScreenOption)
            {
                currentResolutionIndex = i;
                break;
            }
        }
        if (currentResolutionIndex != -1)
        {
            dropdownResolution.value = currentResolutionIndex;
            dropdownResolution.RefreshShownValue(); // 현재 선택된 값으로 UI 업데이트
        }
    }

    // SetResolution 함수를 인덱스와 함께 고유 해상도 리스트를 받도록 수정
    public void SetResolutionByIndex(int uniqueResolutionListIndex, List<Resolution> uniqueResolutions)
    {
        if (uniqueResolutionListIndex < 0 || uniqueResolutionListIndex >= uniqueResolutions.Count)
        {
            Debug.LogError($"Invalid resolution index: {uniqueResolutionListIndex}");
            return;
        }

        Resolution selectedRes = uniqueResolutions[uniqueResolutionListIndex];

        // 현재 해상도와 다를 경우에만 변경 (불필요한 변경 방지)
        // 주사율은 selectedRes에 포함된 값을 사용하거나, Screen.currentResolution.refreshRate를 유지할 수 있음
        // 여기서는 selectedRes에 포함된 주사율 사용
        if (Screen.width != selectedRes.width || Screen.height != selectedRes.height || Screen.currentResolution.refreshRateRatio.value != selectedRes.refreshRateRatio.value)
        {
            Debug.Log($"Setting resolution to: {selectedRes.width}x{selectedRes.height} @ {selectedRes.refreshRateRatio}Hz");
            Screen.SetResolution(selectedRes.width, selectedRes.height, Screen.fullScreenMode, selectedRes.refreshRateRatio);
        }
    }



    public void SetDisplayMode(int displayModeIndex)
    {
        switch (displayModeIndex)
        {
            case 0: // 전체 화면
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1: // 창 모드
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 2: // 테두리 없는 창 모드
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
        }
    }

    #endregion

    #region 음량
    public void SetVolume(float volume, string type)
    {
        audioMixer.SetFloat(type, Mathf.Log10(volume) * 20);
    }

    public void SetMute(bool mute, string type)
    {
        float volume = mute ? -80f : sliderMasterVolume.value;
        audioMixer.SetFloat(type, Mathf.Log10(volume) * 20);
    }

    private void LoadVolumeSettings()
    {
        // 저장된 설정 값 로드
        float masterVolume, bgmVolume, sfxVolume;
        audioMixer.GetFloat("Master", out masterVolume);
        audioMixer.GetFloat("BGM", out bgmVolume);
        audioMixer.GetFloat("SFX", out sfxVolume);

        sliderMasterVolume.value = Mathf.Pow(10f, masterVolume / 20f);
        sliderBgmVolume.value = Mathf.Pow(10f, bgmVolume / 20f);
        sliderSfxVolume.value = Mathf.Pow(10f, sfxVolume / 20f);
        toggleMasterMute.isOn = toggleBgmMute.isOn = toggleSfxMute.isOn = false;
    }
    #endregion
}
