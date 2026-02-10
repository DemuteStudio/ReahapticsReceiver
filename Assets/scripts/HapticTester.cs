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
using System.Collections;
using SimpleFileBrowser;

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
    public TMP_Text videoFilePathText; // UI text to display the imported video file path
    public TMP_Text hapticFilePathText;


    private HapticPreviewData currentHapticData;
    private List<HapticPreviewData> hapticsList = new List<HapticPreviewData>();

    private HapticClip _hapticClip;
    private HapticMaterial _hapticMaterial;

    private string hapticDataFilePath;
    private bool useNiceVibrations = true;
    private bool hasHapticDataSaved = false;

    [Header("Visualizer")]
    [SerializeField] private HapticVisualizerIntegration visualizerIntegration;

    // Visualizer tracking state
    private bool _isTrackingTime = false;
    private float _hapticStartTime = 0f;
    private float _currentPlaybackTime = 0f;

    private void Start()
    {
        Initialize();
        SetupUIListeners();
        LoadInitialData();
    }

    private void Update()
    {
        if (_isTrackingTime && visualizerIntegration != null)
        {
            _currentPlaybackTime += Time.deltaTime;
            visualizerIntegration.UpdateTime(_currentPlaybackTime);
        }
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
        hapticDropdown.onValueChanged.AddListener(LoadSelectedHaptic);

        saveButton.onClick.AddListener(SaveHaptic);
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
    /// <summary>
    /// Converts haptic file data to HapticInputData format for the visualizer.
    /// </summary>
    private string ConvertHapticToInputData(string jsonData, string fileType)
    {
        try
        {
            if (string.IsNullOrEmpty(fileType))
            {
                Debug.LogWarning("[HapticTester] File type is null or empty, returning original data");
                return jsonData;
            }

            string lowerFileType = fileType.ToLower();

            if (lowerFileType == ".haptic")
            {
                Debug.Log("[HapticTester] Converting Nice Vibrations format to HapticInputData");
                return HapticConverter.ConvertFromJsonNiceVibrations(jsonData);
            }
            else if (lowerFileType == ".haps")
            {
                Debug.Log("[HapticTester] Converting InterHaptics format to HapticInputData");
                return HapticConverter.ConvertFromJsonInterHaptics(jsonData);
            }
            else
            {
                Debug.LogWarning($"[HapticTester] Unknown file type '{fileType}', returning original data");
                return jsonData;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HapticTester] Failed to convert haptic data: {ex.Message}");
            return jsonData; // Fallback to original data
        }
    }

    private void LoadSelectedHaptic(int value)
    {
        currentHapticData = hapticsList[value];
        videoFilePathText.text = Path.GetFileNameWithoutExtension(currentHapticData.videoPath);
        hapticFilePathText.text = Path.GetFileNameWithoutExtension(currentHapticData.hapticPath);
        hasHapticDataSaved = false;

        // Load the haptic data into visualizer when selecting from dropdown
        if (visualizerIntegration != null && !string.IsNullOrEmpty(currentHapticData.hapticPath))
        {
            try
            {
                string hapticJson = File.ReadAllText(currentHapticData.hapticPath);
                string convertedJson = ConvertHapticToInputData(hapticJson, currentHapticData.type);
                visualizerIntegration.LoadHapticData(convertedJson);
                Debug.Log($"[HapticTester] Loaded selected haptic into visualizer: {currentHapticData.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HapticTester] Failed to load haptic into visualizer: {ex.Message}");
            }
        }
    }

    private void DeleteSelectedHaptic()
    {
        hapticsList.RemoveAt(hapticDropdown.value);
        HapticFileManager.SaveHapticsDataToPersistentStorage(hapticsList, hapticDataFilePath);
        hasHapticDataSaved = false;
        UpdateDropdown();
    }

    private void SaveHaptic()
    {
        var newHapticData = new HapticPreviewData
        {
            hapticPath = currentHapticData.hapticPath,
            videoPath = currentHapticData.videoPath,
            triggerTime = currentHapticData.triggerTime,
            name = currentHapticData.name,
            type = currentHapticData.type
        };
        if (IsHapticIsAlreadyInList(newHapticData)) return;
        hapticsList.Add(newHapticData);
        HapticFileManager.SaveHapticsDataToPersistentStorage(hapticsList, hapticDataFilePath);
        hasHapticDataSaved = true;
        UpdateDropdown();
    }

    private bool IsHapticIsAlreadyInList(HapticPreviewData hapticData)
    {
        foreach (var haptic in hapticsList)
        {
            if (haptic.hapticPath == hapticData.hapticPath && haptic.videoPath == hapticData.videoPath)
            {
                return true;
            }
        }
        return false;
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

    private void ImportVideoFile()
    {
#if UNITY_STANDALONE_WIN
        StartCoroutine(ShowFileBrowser("mp4", (path) =>
        {
            if (path == null)
            {
                Debug.Log("Video operation cancelled");
                return;
            }

            videoFilePathText.text = Path.GetFileNameWithoutExtension(path);
            currentHapticData.videoPath = path;
        }));
#else
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
#endif
    }

    private void ImportHapticFile()
    {
#if UNITY_STANDALONE_WIN
        StartCoroutine(ShowFileBrowser("haptic", (path) =>
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
            Debug.Log($"Haptic file: {currentHapticData.name}, type: {type}, path: {path}");

            // Load haptic data into visualizer
            if (visualizerIntegration != null)
            {
                string hapticJson = File.ReadAllText(path);
                string convertedJson = ConvertHapticToInputData(hapticJson, type);
                visualizerIntegration.LoadHapticData(convertedJson);
                Debug.Log($"[HapticTester] Loaded haptic data into visualizer");
            }
        }));
#else
        if (NativeFilePicker.IsFilePickerBusy()) return;


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

            // Load haptic data into visualizer
            if (visualizerIntegration != null)
            {
                string hapticJson = File.ReadAllText(path);
                string convertedJson = ConvertHapticToInputData(hapticJson, type);
                visualizerIntegration.LoadHapticData(convertedJson);
                Debug.Log($"[HapticTester] Loaded haptic data into visualizer");
            }
        });
#endif

    }

#if UNITY_STANDALONE_WIN
    private IEnumerator ShowFileBrowser(string extension, System.Action<string> callback)
    {
        FileBrowser.SetFilters(false, new FileBrowser.Filter(extension.ToUpper() + " files", extension));
        FileBrowser.SetDefaultFilter(extension);
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Select File", "Select");

        if (FileBrowser.Success)
            callback(FileBrowser.Result[0]);
        else
            callback(null);
    }
#endif


#region Video Player Methods
    private void VideoPlayerLoopPointReached(VideoPlayer vp)
    {
        StopTracking();
        viewManager.CloseVideoScreen();
    }


    private void OnVideoPrepared(VideoPlayer source)
    {
        videoPlayer.Play();
        StartTracking();
        PlayHaptic();
    }

    private void PlayVideoWithHaptic()
    {
        HapticFileManager.LoadAndParseHapticFile(
            currentHapticData.hapticPath,
            currentHapticData.type,
            ref _hapticClip,
            ref _hapticMaterial);
        videoPlayer.Stop();
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

        StartTracking();
        PlayHapticDelayed();

        // Schedule stop tracking based on haptic duration (estimate ~3 seconds if unknown)
        float hapticDuration = 3f; // You could parse this from the haptic file if available
        Invoke(nameof(StopTracking), hapticDuration);
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

    #region Visualizer Tracking Methods
    private void StartTracking()
    {
        if (visualizerIntegration == null) return;

        _isTrackingTime = true;
        _currentPlaybackTime = 0f;
        _hapticStartTime = Time.time;
        visualizerIntegration.StartTracking();
        Debug.Log("[HapticTester] Started visualizer tracking");
    }

    private void StopTracking()
    {
        if (visualizerIntegration == null) return;

        _isTrackingTime = false;
        _currentPlaybackTime = 0f;
        visualizerIntegration.StopTracking();
        Debug.Log("[HapticTester] Stopped visualizer tracking");
    }
    #endregion
}
