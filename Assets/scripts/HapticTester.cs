using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using Lofelt.NiceVibrations;
using Interhaptics;
using Interhaptics.Core;

public class HapticTester : MonoBehaviour
{
    [Header("Reaper view")]
    public ViewManager viewManager;

    [Header("Import view")]
    public Button reaperViewButton;
    public Button deleteButton;
    public Button loadVideoButton;
    public Button loadHapticButton;
    public Button PlayHapticButton;
    public Button PlayHapticPreviewButton;
    public Button saveButton;
    public Button closeVideoButton;
    public TMP_InputField triggerTimeInput;
    public TMP_Dropdown hapticDropdown;
    public VideoPlayer videoPlayer;
    public Button loadButton;
    public TMP_Text videoFilePathText; // UI text to display the imported video file path
    public TMP_Text hapticFilePathText;


    private HapticPreviewData currentHapticData;
    private List<HapticPreviewData> hapticsList = new List<HapticPreviewData>();

    private HapticClip _hapticClip;
    private HapticMaterial _hapticMaterial;

    private string hapticDataFilePath;
    private bool useNiceVibrations = true;
    private bool hasHapticDataSaved = false;

    private void Start()
    {
        Initialize();
        SetupUIListeners();
        LoadInitialData();
    }

    private void Initialize()
    {
        currentHapticData = new HapticPreviewData();
        _hapticClip = ScriptableObject.CreateInstance<HapticClip>();
        hapticDataFilePath = Path.Combine(Application.persistentDataPath, "hapticData.json");
        viewManager.screen.SetActive(false);

        NativeFilePicker.ConvertExtensionToFileType("mp4");
        NativeFilePicker.ConvertExtensionToFileType("haptic");
    }

    private void SetupUIListeners()
    {
        reaperViewButton.onClick.AddListener(viewManager.ShowReaperView);
        closeVideoButton.onClick.AddListener(viewManager.CloseVideoScreen);

        saveButton.onClick.AddListener(SaveHaptic);
        loadButton.onClick.AddListener(LoadSelectedHaptic);
        deleteButton.onClick.AddListener(DeleteSelectedHaptic);

        loadVideoButton.onClick.AddListener(ImportVideoFile);
        loadHapticButton.onClick.AddListener(ImportHapticFile);

        PlayHapticPreviewButton.onClick.AddListener(PlayVideoWithHaptic);
        PlayHapticButton.onClick.AddListener(PlayHapticOnly);

        triggerTimeInput.onEndEdit.AddListener(OnHapticTriggerTimeChanged);
        videoPlayer.loopPointReached += VideoPlayerLoopPointReached;
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void LoadInitialData()
    {
        hapticsList = HapticFileManager.LoadHapticsDataFromFile(hapticDataFilePath);
        UpdateDropdown();
    }

    public void SetHapticsMethod(int val)
    {
        useNiceVibrations = val == 1;
    }

    #region Haptic Data Management
    private void LoadSelectedHaptic()
    {
        currentHapticData = hapticsList[hapticDropdown.value];
        videoFilePathText.text = Path.GetFileNameWithoutExtension(currentHapticData.videoPath);
        hapticFilePathText.text = Path.GetFileNameWithoutExtension(currentHapticData.hapticPath);
    }

    private void DeleteSelectedHaptic()
    {
        hapticsList.RemoveAt(hapticDropdown.value);
        HapticFileManager.SaveHapticsDataToPersistentStorage(hapticsList, hapticDataFilePath);
        UpdateDropdown();
    }

    private void SaveHaptic()
    {
        if (hasHapticDataSaved) return;

        var newHapticData = new HapticPreviewData
        {
            hapticPath = currentHapticData.hapticPath,
            videoPath = currentHapticData.videoPath,
            triggerTime = currentHapticData.triggerTime,
            name = currentHapticData.name,
            type = currentHapticData.type
        };

        hapticsList.Add(newHapticData);
        HapticFileManager.SaveHapticsDataToPersistentStorage(hapticsList, hapticDataFilePath);
        hasHapticDataSaved = true;
        UpdateDropdown();
    }

    private void UpdateDropdown()
    {
        hapticDropdown.ClearOptions();
        foreach (var haptic in hapticsList)
        {
            hapticDropdown.options.Add(new TMP_Dropdown.OptionData(haptic.name));
        }
    }
    #endregion

    #region File Import Methods
    private void ImportVideoFile()
    {
        if (NativeFilePicker.IsFilePickerBusy()) return;

        string[] fileTypes = { NativeFilePicker.ConvertExtensionToFileType("mp4") };

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                Debug.Log("Video operation cancelled");
                return;
            }

            videoFilePathText.text = Path.GetFileNameWithoutExtension(path);
            currentHapticData.videoPath = path;
        }, fileTypes);
    }

    private void ImportHapticFile()
    {
        if (NativeFilePicker.IsFilePickerBusy()) return;

        string[] fileTypes = { NativeFilePicker.ConvertExtensionToFileType("haptic") };

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                Debug.Log("Haptic operation cancelled");
                return;
            }

            string type = Path.GetExtension(path);
            currentHapticData.type = type;
            hapticFilePathText.text = Path.GetFileNameWithoutExtension(path);
            currentHapticData.hapticPath = path;
            currentHapticData.name = Path.GetFileNameWithoutExtension(path);
            Debug.Log($"Haptic file: {currentHapticData.name}, type: {type}");
        }, fileTypes);
    }
    #endregion

    #region Video Player Methods
    private void VideoPlayerLoopPointReached(VideoPlayer vp)
    {
        viewManager.CloseVideoScreen();
    }


    private void OnVideoPrepared(VideoPlayer source)
    {
        videoPlayer.Play();
        PlayHaptic();
    }

    private void PlayVideoWithHaptic()
    {
        HapticFileManager.LoadAndParseHapticFile(
            currentHapticData.hapticPath,
            currentHapticData.type,
            ref _hapticClip,
            ref _hapticMaterial);

        videoPlayer.url = currentHapticData.videoPath;
        viewManager.ShowVideoScreen();
        videoPlayer.Prepare();
    }
    #endregion

    #region Haptic Playback Methods
    private void PlayHapticOnly()
    {
        HapticFileManager.LoadAndParseHapticFile(
            currentHapticData.hapticPath,
            currentHapticData.type,
            ref _hapticClip,
            ref _hapticMaterial);

        PlayHapticDelayed();
    }

    private void PlayHaptic()
    {
        Invoke(nameof(PlayHapticDelayed), currentHapticData.triggerTime);
    }

    private void PlayHapticDelayed()
    {
        Debug.Log($"Playing haptic {_hapticClip.name} at {currentHapticData.triggerTime} seconds, of type {currentHapticData.type}");

        if (currentHapticData.type == ".haptic")
        {
            HapticController.fallbackPreset = HapticPatterns.PresetType.Success;
            HapticController.Play(_hapticClip);
            Debug.Log(_hapticClip.json);
        }
        else if (currentHapticData.type == ".haps")
        {
            HAR.PlayHapticEffect(_hapticMaterial);
            Debug.Log(_hapticMaterial.text);
        }
    }
    #endregion

    private void OnHapticTriggerTimeChanged(string value)
    {
        if (float.TryParse(value, out float time))
        {
            currentHapticData.triggerTime = time;
        }
    }
}
