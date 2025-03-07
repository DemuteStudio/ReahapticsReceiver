using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using TMPro;
using Lofelt.NiceVibrations;
using System.Text;
using Interhaptics;
using Interhaptics.Core;
using Interhaptics.Internal;
using Interhaptics.Utils;

[System.Serializable]
public class HapticPreviewData
{
    public string name = "NoHapticSet";
    public string videoPath;
    public string hapticPath;
    public float triggerTime;
    public string type;
}

[Serializable]
public class HapticDataListWrapper
{
    public List<HapticPreviewData> hapticDataList;

    public HapticDataListWrapper(List<HapticPreviewData> hapticDataList)
    {
        this.hapticDataList = hapticDataList;
    }
}

public class HapticTester : MonoBehaviour
{
    [Header("Reaper view")]
    public Button importViewButton;
    public Button settingsButton;
    public Button closeSettingsButton;
    
    [Header("import view")]
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
    
    [Header("general")]
    public GameObject ReaperView;
    public GameObject ImportView;
    public GameObject SettingsView;
    public GameObject screen;
    public TMP_Text videoFilePathText; // UI text to display the imported video file path
    public TMP_Text hapticFilePathText;

    HapticPreviewData currentHapticData;

    private float hapticTriggerTime;
    private bool hapticTriggered = false;
    
    private bool hasHapticDataSaved = false;
    
    private List<HapticPreviewData> hapticsList = new List<HapticPreviewData>();
    
    private HapticClip _hapticClip;
    private HapticMaterial _hapticMaterial;
    
    private string hapticDataFilePath;

    private bool useNiceVibrations = true;
    private void Start()
    {
        print("Is gampad connected: " + GamepadRumbler.IsConnected());
        currentHapticData = new HapticPreviewData();
        _hapticClip = ScriptableObject.CreateInstance<HapticClip>();
        hapticDataFilePath = Path.Combine(Application.persistentDataPath, "hapticData.json");
        screen.SetActive(false);
        
        NativeFilePicker.ConvertExtensionToFileType("mp4");
        NativeFilePicker.ConvertExtensionToFileType("haptic"); 
        
        importViewButton.onClick.AddListener(OpenImportScreen);
        reaperViewButton.onClick.AddListener(OpenReaperView);
        settingsButton.onClick.AddListener(OpenSettingsView);
        closeSettingsButton.onClick.AddListener(CloseSettingsView);
        closeVideoButton.onClick.AddListener(CloseVideoScreen);
        
        saveButton.onClick.AddListener(SaveHaptic);
        loadButton.onClick.AddListener(loadSelectedHaptic);
        deleteButton.onClick.AddListener(deleteSelectedHaptic);
        
        loadVideoButton.onClick.AddListener(ImportVideoFile);
        loadHapticButton.onClick.AddListener(ImportHapticFile);
        
        PlayHapticPreviewButton.onClick.AddListener(PlayVideoWithHaptic);
        PlayHapticButton.onClick.AddListener(PlayHapticOnly);
        
        triggerTimeInput.onEndEdit.AddListener(OnHapticTriggerTimeChanged);
        videoPlayer.loopPointReached += VideoPlayerLoopPointReached;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        LoadSavedHaptics();
    }

    public void SetHapticsMethod(int val)
    {
        if (val == 1) useNiceVibrations = true;
        else useNiceVibrations = false;
    }
    private void loadSelectedHaptic()
    {
        currentHapticData = hapticsList[hapticDropdown.value];
        videoFilePathText.text = Path.GetFileNameWithoutExtension(currentHapticData.videoPath);
        hapticFilePathText.text = Path.GetFileNameWithoutExtension(currentHapticData.hapticPath);
    }
    
    private void deleteSelectedHaptic()
    {
        hapticsList.RemoveAt(hapticDropdown.value);
        SaveHapticsDataToPersistentStorage(hapticsList);
        UpdateDropdown();
    }
    private void ImportVideoFile()
    {
        if (NativeFilePicker.IsFilePickerBusy())
            return;

        string[] fileTypes = { NativeFilePicker.ConvertExtensionToFileType("mp4") };

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                Debug.Log("Video operation cancelled");
            }
            else
            {
                Debug.Log("video file: " + Path.GetFileNameWithoutExtension(path));
                videoFilePathText.text = Path.GetFileNameWithoutExtension(path);
                currentHapticData.videoPath = path;
            }
        }, fileTypes);
    }
    
    private void ImportHapticFile()
    {
        if (NativeFilePicker.IsFilePickerBusy())
            return;

        string[] fileTypes = {};

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                Debug.Log("Haptic operation cancelled");
            }
            else
            {
                Debug.Log("haptic file: " + Path.GetFileNameWithoutExtension(path));
                string type = Path.GetExtension(path);
                currentHapticData.type = type;
                Debug.Log("haptic type: " + type);

                hapticFilePathText.text = Path.GetFileNameWithoutExtension(path);
                currentHapticData.hapticPath = path;
                currentHapticData.name = Path.GetFileNameWithoutExtension(path);
            }
        }, fileTypes);
    }
    private void VideoPlayerLoopPointReached(VideoPlayer vp)
    {
        CloseVideoScreen();
    }
    private void CloseVideoScreen()
    {
        screen.SetActive(false);
        ImportView.SetActive(true);
        CancelInvoke();
    }
    private void OpenImportScreen()
    {
        hasHapticDataSaved = false;
        ReaperView.SetActive(false);
        ImportView.SetActive(true);
    }

    private void OpenReaperView()
    {
        ImportView.SetActive(false);
        ReaperView.SetActive(true);
    }
    
    private void CloseSettingsView()
    {
        ImportView.SetActive(false);
        SettingsView.SetActive(false);
        ReaperView.SetActive(true);
        settingsButton.gameObject.SetActive(true);
    }
    
    private void OpenSettingsView()
    {
        ReaperView.SetActive(false);
        ImportView.SetActive(false);
        SettingsView.SetActive(true);
        settingsButton.gameObject.SetActive(false);

    }

    private void SaveHaptic()
    {
        if (!hasHapticDataSaved)
        {
            HapticPreviewData newHapticData = new HapticPreviewData();
            newHapticData.hapticPath = currentHapticData.hapticPath;
            newHapticData.videoPath = currentHapticData.videoPath;
            newHapticData.triggerTime = currentHapticData.triggerTime;
            newHapticData.name = currentHapticData.name;
            newHapticData.type = currentHapticData.type;
            hapticsList.Add(newHapticData);
            SaveHapticsDataToPersistentStorage(hapticsList);
            hasHapticDataSaved = true;
            UpdateDropdown();
        }
    }
    
    private void LoadSavedHaptics()
    {
        if (File.Exists(hapticDataFilePath))
        {
            string json = File.ReadAllText(hapticDataFilePath);
            HapticDataListWrapper wrapper = JsonUtility.FromJson<HapticDataListWrapper>(json);
            hapticsList = wrapper.hapticDataList;
        }
        else
        {
            Debug.LogWarning("No haptic data found at " + hapticDataFilePath);
        }
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
    
    private void PlayVideoWithHaptic()
    {
        LoadAndParseHapticFile(currentHapticData.hapticPath);
        videoPlayer.url = currentHapticData.videoPath;

        ReaperView.SetActive(false);
        ImportView.SetActive(false);
        screen.SetActive(true);
        videoPlayer.Prepare();
    }
    
    private void OnVideoPrepared(VideoPlayer source)
    {
        videoPlayer.Play();
        PlayHaptic();
    }
    
    private void PlayHapticOnly()
    {
        LoadAndParseHapticFile(currentHapticData.hapticPath);
        PlayHapticDelayed();
    }
    private void PlayHaptic()
    {
        Invoke("PlayHapticDelayed", currentHapticData.triggerTime);
    }
    private void PlayHapticDelayed()
    {
        print("Playing haptic" + _hapticClip.name + " at " + currentHapticData.triggerTime + " seconds, of type " + currentHapticData.type);
        if (currentHapticData.type == ".haptic")
        {
            HapticController.fallbackPreset = HapticPatterns.PresetType.Success;
            HapticController.Play(_hapticClip);
            print(_hapticClip.json);
        }
        if (currentHapticData.type == ".haps")
        {
            HAR.PlayHapticEffect(_hapticMaterial);
            print(_hapticMaterial.text);
        }
    }
    public void SaveHapticsDataToPersistentStorage(List<HapticPreviewData> hapticDataList)
    {
        try
        {
            // Serialize the list to JSON
            string json = JsonUtility.ToJson(new HapticDataListWrapper(hapticDataList));

            // Write the JSON to a file in persistent storage
            File.WriteAllText(hapticDataFilePath, json);

            Debug.Log("Haptic data saved successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save haptic data: " + e.Message);
        }
    }
    
    private void OnHapticTriggerTimeChanged(string value)
    {
        currentHapticData.triggerTime = float.Parse(value);
    }
    
    private void LoadAndParseHapticFile(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path); // Read JSON from file
            if (currentHapticData.type == ".haptic")
            {
                _hapticClip.name = Path.GetFileNameWithoutExtension(path);
                _hapticClip.json = Encoding.UTF8.GetBytes(json); // Parse JSON into HapticData object
                Debug.Log("Haptic data loaded: " + _hapticClip.json);
            }
            if (currentHapticData.type == ".haps")
            {
                _hapticMaterial = HapticMaterial.CreateInstanceFromString(json);
            }
        }
        else
        {
            Debug.LogError("Haptic file not found: " + path);
        }
    }
    
}
