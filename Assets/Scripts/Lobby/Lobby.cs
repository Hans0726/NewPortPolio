using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class Lobby : MonoBehaviour
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
    
    [SerializeField] private TMP_Dropdown dropdownDisplayMode;
    [SerializeField] private TextMeshProUGUI currentDisplayMode;

    [SerializeField] private TMP_Dropdown dropdownResolution;
    [SerializeField] private TextMeshProUGUI currentResolution;

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
    private readonly List<Resolution> _resolutionChoices = new List<Resolution>();
    private int _displayedWidth = -1;
    private int _displayedHeight = -1;
    private FullScreenMode _displayedMode;
    private bool _refreshDisplayRequested;
    private int _customResolutionIndex = -1;

    private void SetupDropdowns()
    {
        dropdownResolution.ClearOptions();
        _resolutionChoices.Clear();
        _customResolutionIndex = -1;
        foreach (Resolution resolution in Screen.resolutions.Reverse())
        {
            if (_resolutionChoices.Any(item => item.width == resolution.width && item.height == resolution.height))
                continue;

            _resolutionChoices.Add(resolution);
            dropdownResolution.options.Add(new TMP_Dropdown.OptionData(
                $"{resolution.width} x {resolution.height}"));
        }

        RefreshDisplaySettings();
        dropdownResolution.onValueChanged.AddListener(HandleResolutionSelected);
        dropdownDisplayMode.onValueChanged.AddListener(SetDisplayMode);
    }

    private void Update()
    {
        if (_refreshDisplayRequested || Screen.width != _displayedWidth ||
            Screen.height != _displayedHeight || Screen.fullScreenMode != _displayedMode)
        {
            RefreshDisplaySettings();
        }
    }

    private void RefreshDisplaySettings()
    {
        _refreshDisplayRequested = false;
        _displayedWidth = Screen.width;
        _displayedHeight = Screen.height;
        _displayedMode = Screen.fullScreenMode;
        string label = $"{_displayedWidth} x {_displayedHeight}";
        currentResolution.text = label;

        int index = _resolutionChoices.FindIndex(item =>
            item.width == _displayedWidth && item.height == _displayedHeight);
        // Resizable windows can have a size that is absent from the monitor's modes.
        if (index < 0)
        {
            if (_customResolutionIndex < 0)
            {
                _customResolutionIndex = _resolutionChoices.Count;
                _resolutionChoices.Add(default);
                dropdownResolution.options.Add(new TMP_Dropdown.OptionData());
            }
            index = _customResolutionIndex;
            _resolutionChoices[index] = new Resolution { width = _displayedWidth, height = _displayedHeight };
            dropdownResolution.options[index].text = label;
        }
        dropdownResolution.SetValueWithoutNotify(index);
        dropdownResolution.RefreshShownValue();

        int modeIndex = _displayedMode == FullScreenMode.ExclusiveFullScreen ? 0 :
            _displayedMode == FullScreenMode.Windowed ? 1 : 2;
        dropdownDisplayMode.SetValueWithoutNotify(modeIndex);
        dropdownDisplayMode.RefreshShownValue();
        currentDisplayMode.text = modeIndex == 0 ? "전체 화면" :
            modeIndex == 1 ? "창 모드" : "테두리 없는 창 모드";
    }

    private void HandleResolutionSelected(int index)
    {
        SetResolutionByIndex(index, _resolutionChoices);
    }

    public void SetResolutionByIndex(int index, List<Resolution> choices)
    {
        if (index < 0 || index >= choices.Count) return;

        Resolution selected = choices[index];
        Screen.SetResolution(selected.width, selected.height, Screen.fullScreenMode);
        // Read the applied size on subsequent frames, not the requested size.
        _refreshDisplayRequested = true;
    }

    public void SetDisplayMode(int index)
    {
        if (index < 0 || index > 2) return;
        FullScreenMode mode = index == 0 ? FullScreenMode.ExclusiveFullScreen :
            index == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
        Screen.SetResolution(Screen.width, Screen.height, mode);
        _refreshDisplayRequested = true;
    }

    #endregion

    #region 음량
    public void SetVolume(float volume, string type)
    {
        audioMixer.SetFloat(type, SliderValueToDecibel(volume));
    }

    public void SetMute(bool mute, string type)
    {
        float volume = type switch
        {
            "BGM" => sliderBgmVolume.value,
            "SFX" => sliderSfxVolume.value,
            _ => sliderMasterVolume.value
        };

        audioMixer.SetFloat(type, mute ? -80f : SliderValueToDecibel(volume));
    }

    private float SliderValueToDecibel(float volume)
    {
        return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
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
