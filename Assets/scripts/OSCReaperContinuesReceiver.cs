using System;
using System.Text;
using extOSC;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Interhaptics;
using Interhaptics.Core;
using UnityEngine.InputSystem;
using RichTap.Source;
using RichTap.Common;
using RichTap.Core;

public enum HapticMethod
{
    NiceVibrations = 0,
    InterHaptics = 1,
    RichTap = 2
}
/// <summary>
/// Receives OSC messages from Reaper and plays haptic feedback using either Nice Vibrations or InterHaptics
/// </summary>
public class OSCReaperContinuesReceiver  : MonoBehaviour
{
    #region Constants and Configuration
    
    [Header("OSC Configuration")]
    [SerializeField] private string hapticAddress = "/HapticJson";
    [SerializeField] private string instantHapticAddress = "/InstantHapticJson";
    [SerializeField] private string timeAddress = "/CursorPos";
    [SerializeField] private string startStopAddress = "/StartStop";
    [SerializeField] private int port = 7401;
    
    [Header("UI References")]
    [SerializeField] private ViewManager viewManager;
    [SerializeField] private Button playHapticButton;
    [SerializeField] private Button toggleConnectButton;
    [SerializeField] private Button loadHapticButton;
    [SerializeField] private Button importViewButton;
    [SerializeField] private GameObject connectedLight;
    [SerializeField] private GameObject connectedLightGreen;
    [SerializeField] private TextMeshProUGUI hapticNameText;
    [SerializeField] private TextMeshProUGUI ipText;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private RichtapClipEffect _richtapClipEffect;
    
    [Header("Haptic Settings")]
    [SerializeField] private HapticPatterns.PresetType fallbackPreset = HapticPatterns.PresetType.Success;
    private HapticMethod _hapticMethod = HapticMethod.NiceVibrations;

    [Header("Visualizer")]
    [SerializeField] private HapticVisualizerIntegration visualizerIntegration;
    #endregion
    
    #region Private Fields
    
    private OSCReceiver _receiver;
    private HapticClip _continuousHapticClip;
    private HapticClip _instantHapticClip;
    [SerializeField]
    private HapticMaterial _continuousHapticMaterial;
    [SerializeField]
    private HapticMaterial _instantHapticMaterial;
    private RichtapClip _richtapClip;

    // State management
    private bool _isListening = false;
    private bool _isCursorMoving = false;
    private float _currentTime = 0f;
    private float _scheduledHapticTime = 0f;
    private bool _hasScheduledHaptic = false;
    private float _visualizerHapticStartTime = 0f; // When the visualizer's current haptic started playing
    
    // Error handling
    private int _consecutiveErrors = 0;
    private const int MAX_CONSECUTIVE_ERRORS = 5;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        Application.runInBackground = true;
        
        try 
        {
            InitializeComponents();
            SetupEventListeners();
            InitializeOSCReceiver();
            
            // Delay IP setting to ensure receiver is fully initialized
            // Extended to 2 seconds to allow mobile hotspot interfaces to stabilize
            Invoke(nameof(UpdateIPDisplay), 2f);
            
            LogMessage("Haptic receiver initialized successfully");
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize haptic receiver: {ex.Message}");
        }
    }
    
    void Update()
    {
        if (!_isListening || !_isCursorMoving) return;
        
        UpdateTime();
        CheckScheduledHaptics();
    }
    
    void OnDestroy()
    {
        CleanupResources();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeComponents()
    {
        // Create haptic clips
        _continuousHapticClip = ScriptableObject.CreateInstance<HapticClip>();
        _instantHapticClip = ScriptableObject.CreateInstance<HapticClip>();
        _richtapClip = ScriptableObject.CreateInstance<RichtapClip>();
        // Validate required components
        if (viewManager == null) LogError("ViewManager not assigned");
        if (hapticNameText == null) LogError("Haptic name text not assigned");
        if (ipText == null) LogError("IP text not assigned");
    }
    
    private void SetupEventListeners()
    {
        // UI Event bindings with null checks
        if (importViewButton != null) 
            importViewButton.onClick.AddListener(() => viewManager?.ShowImportView());
            
        if (toggleConnectButton != null) 
            toggleConnectButton.onClick.AddListener(ToggleListening);
            
        if (playHapticButton != null) 
            playHapticButton.onClick.AddListener(PlayInstantHaptic);
            
        if (portInput != null) 
        {
            portInput.onEndEdit.AddListener(OnPortInputChanged);
            portInput.text = port.ToString();
        }
    }
    
    private void InitializeOSCReceiver()
    {
        try
        {
            _receiver = gameObject.AddComponent<OSCReceiver>();
            _receiver.LocalPort = port;
            
            // Bind OSC addresses
            _receiver.Bind(startStopAddress, OnStartStopMessage);
            _receiver.Bind(hapticAddress, OnHapticMessage);
            _receiver.Bind(timeAddress, OnTimeMessage);
            _receiver.Bind(instantHapticAddress, OnInstantHapticMessage);
            
            LogMessage($"OSC Receiver listening on port {port}");
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize OSC receiver: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Public Interface
    
    public void SetHapticsMethod(int methodIndex)
    {
        _hapticMethod = methodIndex switch
        {
            0 => HapticMethod.NiceVibrations,
            1 => HapticMethod.InterHaptics,
            2 => HapticMethod.RichTap,
            _ => HapticMethod.NiceVibrations
        };
        string method = _hapticMethod.ToString();
        LogMessage($"Haptic method set to: {method}");
    }
    
    public void ToggleListening()
    {
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(0.5f, 0.5f); // Low freq, high freq
        }
        _isListening = !_isListening;
        UpdateUIState();
        
        if (!_isListening)
        {
            StopAllHaptics();
            ResetTimingState();
        }
        
        LogMessage($"Listening {(_isListening ? "enabled" : "disabled")}");
    }
    
    public void PlayInstantHaptic()
    {
        if (_instantHapticClip == null && _instantHapticMaterial == null)
        {
            LogError("No instant haptic data loaded");
            return;
        }
        
        PlayHaptic(_instantHapticClip, _instantHapticMaterial, _richtapClipEffect, HapticMethod.InterHaptics, "instant");
    }
    
    #endregion
    
    #region OSC Message Handlers
    
    private void OnStartStopMessage(OSCMessage message)
    {
        if (message.Values.Count == 0) return;
        
        string command = message.Values[0].StringValue;
        HandleTransportCommand(command);
    }
    
    private void OnHapticMessage(OSCMessage message)
    {
        if (!_isListening || message.Values.Count == 0) return;
        
        try
        {
            ProcessContinuousHapticData(message.Values[0].StringValue);
        }
        catch (Exception ex)
        {
            HandleError($"Failed to process haptic message: {ex.Message}");
        }
    }
    
    private void OnTimeMessage(OSCMessage message)
    {
        if (!_isListening || message.Values.Count == 0) return;

        _currentTime = message.Values[0].FloatValue;

        // Update visualizer cursor position with relative time within the haptic clip
        if (visualizerIntegration != null && _visualizerHapticStartTime > 0)
        {
            float relativeTime = _currentTime - _visualizerHapticStartTime;

            // Only update if relative time is positive and reasonable
            if (relativeTime >= 0)
            {
                visualizerIntegration.UpdateTime(relativeTime);
            }
        }
    }
    
    private void OnInstantHapticMessage(OSCMessage message)
    {
        if (message.Values.Count == 0) return;
        
        try
        {
            ProcessInstantHapticData(message.Values[0].StringValue);
        }
        catch (Exception ex)
        {
            HandleError($"Failed to process instant haptic: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Haptic Processing
    
    private void ProcessContinuousHapticData(string input)
    {
        var (sendTime, jsonData) = ParseHapticInput(input, "SendTime: ");

        float newScheduledTime = float.Parse(sendTime);

        // Only update visualizer if this is a new haptic (not already tracking one)
        bool isNewHaptic = !_hasScheduledHaptic || Mathf.Abs(newScheduledTime - _scheduledHapticTime) > 0.1f;

        _scheduledHapticTime = newScheduledTime;
        _hasScheduledHaptic = true;

        CreateHapticClips(jsonData, ref _continuousHapticClip, ref _continuousHapticMaterial, "ContinuousHaptic");

        // Only reload visualizer data if this is a genuinely new haptic event
        if (visualizerIntegration != null && isNewHaptic)
        {
            _visualizerHapticStartTime = newScheduledTime;
            Debug.Log($"[OSCReaperContinuesReceiver] NEW haptic scheduled at timeline position: {_scheduledHapticTime:F3}s");
            visualizerIntegration.LoadHapticData(jsonData);
        }
        else if (!isNewHaptic)
        {
            Debug.Log($"[OSCReaperContinuesReceiver] Ignoring duplicate haptic message at position: {_scheduledHapticTime:F3}s");
        }
    }
    
    private void ProcessInstantHapticData(string input)
    {
        var (name, jsonData) = ParseHapticInput(input, "name: ");

        CreateHapticClips(jsonData, ref _instantHapticClip, ref _instantHapticMaterial, name);

        if (hapticNameText != null)
            hapticNameText.text = name;

        // Instant haptics start at current time
        _scheduledHapticTime = _currentTime;
        _visualizerHapticStartTime = _currentTime;

        // Update visualizer with new data
        if (visualizerIntegration != null)
        {
            visualizerIntegration.LoadHapticData(jsonData);
        }

        Debug.Log($"[OSCReaperContinuesReceiver] Instant haptic started at timeline position: {_scheduledHapticTime:F3}s");
    }
    
    private (string prefix, string jsonData) ParseHapticInput(string input, string prefixIdentifier)
    {
        int newlineIndex = input.IndexOf('\n');
        if (newlineIndex == -1)
        {
            throw new ArgumentException("Invalid haptic data format");
        }
        
        string prefixPart = input.Substring(0, newlineIndex).Replace(prefixIdentifier, "").Trim();
        string jsonPart = input.Substring(newlineIndex + 1);
        
        return (prefixPart, jsonPart);
    }
    
    private void CreateHapticClips(string jsonData, ref HapticClip clip, ref HapticMaterial material, string name)
    {
        // Create Nice Vibrations clip
        string niceVibrationsJson = HapticConverter.ConvertToJsonNiceVibrations(jsonData);
        
#if UNITY_STANDALONE_WIN
        clip = NiceVibrationsNative.JsonToHapticClip(Encoding.UTF8.GetBytes(niceVibrationsJson));
#else
        if (clip == null) clip = ScriptableObject.CreateInstance<HapticClip>();
        clip.json = Encoding.UTF8.GetBytes(niceVibrationsJson);
#endif
        
        // Create InterHaptics material
        string interHapticsJson = HapticConverter.ConvertToJsonInterHaptics(jsonData);
        _continuousHapticMaterial = HapticMaterial.CreateInstanceFromString(interHapticsJson);
        _continuousHapticMaterial.name = name;
        
        // Create Richtap effect
        string richTabJson = HapticConverter.ConvertToJsonRichTap(jsonData);
        _richtapClip.SetContent(richTabJson);
        _richtapClipEffect.clip = _richtapClip;
        
        LogMessage($"Created haptic clips for: {_continuousHapticMaterial.text}");
    }
    
    #endregion
    
    #region Haptic Playback
    
    private void PlayHaptic(HapticClip clip, HapticMaterial material, RichtapClipEffect richtapClipEffect, HapticMethod method, string type)
    {
        try{
            switch (method){
                case HapticMethod.NiceVibrations:
                    if (clip != null) {
                        HapticController.fallbackPreset = fallbackPreset;
                        HapticController.Play(clip);
                        LogMessage($"Playing {type} haptic with Nice Vibrations at: {_currentTime:F3}s");
                    }
                    else {
                        LogError($"No Nice Vibrations clip available for {type} playback");
                    }
                    break;
                case HapticMethod.InterHaptics:
                    if (material != null) {
                        HAR.PlayHapticEffect(_continuousHapticMaterial);
                        LogMessage($"Playing {type} haptic with InterHaptics at: {_currentTime:F3}s");
                    }
                    else {
                        LogError($"No InterHaptics material available for {type} playback");
                    }
                    break;
                case HapticMethod.RichTap:
                    if (richtapClipEffect != null) {
                        REM.Instance.PlayEffect(richtapClipEffect);
                        LogMessage($"Playing {type} haptic with RichTap at: {_currentTime:F3}s");
                    }
                    else {
                        LogError($"No RichTap clip available for {type} playback");
                    }
                    break;
                default:
                    LogError($"Unknown haptic method: {method}");
                    break;
            }
            _consecutiveErrors = 0;
        }
        catch (Exception ex) {
            HandleError($"Failed to play {type} haptic: {ex.Message}");
        }
    }
    
    private void StopAllHaptics()
    {
        try
        {
            HapticController.Stop();
            LogMessage("All haptics stopped");
        }
        catch (Exception ex)
        {
            LogError($"Error stopping haptics: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Transport Control
    
    private void HandleTransportCommand(string command)
    {
        switch (command.ToLower())
        {
            case "started":
                _isCursorMoving = true;
                SetConnectionStatus(true);
                if (visualizerIntegration != null)
                    visualizerIntegration.StartTracking();
                LogMessage("Transport started");
                break;

            case "stopped":
                _isCursorMoving = false;
                _hasScheduledHaptic = false;
                StopAllHaptics();
                SetConnectionStatus(false);
                if (visualizerIntegration != null)
                    visualizerIntegration.StopTracking();
                LogMessage("Transport stopped");
                break;

            case "moved":
                _hasScheduledHaptic = false;
                StopAllHaptics();
                if (visualizerIntegration != null)
                {
                    Debug.LogWarning("[OSCReaperContinuesReceiver] Transport MOVED - stopping visualizer tracking");
                    visualizerIntegration.StopTracking();
                }
                LogMessage("Transport position moved");
                break;

            default:
                LogMessage($"Unknown transport command: {command}");
                break;
        }
    }
    
    #endregion
    
    #region Timing and Updates
    
    private void UpdateTime()
    {
        _currentTime += Time.deltaTime;

        // Update visualizer time
        if (visualizerIntegration != null && _isCursorMoving)
        {
            visualizerIntegration.UpdateTime(_currentTime);
        }
    }
    
    private void CheckScheduledHaptics()
    {
        if (_hasScheduledHaptic && _currentTime >= _scheduledHapticTime)
        {
            PlayHaptic(_continuousHapticClip, _continuousHapticMaterial, _richtapClipEffect, _hapticMethod, "continuous");
            _hasScheduledHaptic = false;
            _scheduledHapticTime = 0f;
        }
    }
    
    private void ResetTimingState()
    {
        _currentTime = 0f;
        _scheduledHapticTime = 0f;
        _hasScheduledHaptic = false;
        _isCursorMoving = false;
        _visualizerHapticStartTime = 0f;
    }
    
    #endregion
    
    #region UI Management
    
    private void OnPortInputChanged(string value)
    {
        if (int.TryParse(value, out int newPort) && newPort > 0 && newPort <= 65535)
        {
            port = newPort;
            if (_receiver != null)
            {
                _receiver.LocalPort = newPort;
                LogMessage($"Port changed to: {newPort}");
                UpdateIPDisplay();
            }
        }
        else
        {
            LogError("Invalid port number. Please enter a value between 1 and 65535.");
            if (portInput != null) portInput.text = port.ToString();
        }
    }
    
    private void UpdateUIState()
    {
        if (playHapticButton != null) playHapticButton.interactable = !_isListening;
        if (loadHapticButton != null) loadHapticButton.interactable = !_isListening;
        if (connectedLight != null) connectedLight.SetActive(_isListening);
        ToggleButton(playHapticButton.gameObject);
        ToggleButton(loadHapticButton.gameObject);
    }

    private void ToggleButton(GameObject gm)
    {
        Color disabledTint = new Color(1f, 1f, 1f, 0.4f);
        Color enabledTint = Color.white;
        // Tint TextMeshPro
        foreach (var tmp in gm.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            tmp.color = !_isListening ? enabledTint : disabledTint;
        }
        // Tint all Images
        foreach (var img in gm.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))
        {
            img.color = !_isListening ? enabledTint : disabledTint;
        }
    }
    
    private void SetConnectionStatus(bool isActive)
    {
        if (connectedLightGreen != null) 
            connectedLightGreen.SetActive(isActive);
    }
    
    public void UpdateIPDisplay()
    {
        try
        {
            if (_receiver != null && ipText != null)
            {
                ipText.text = _receiver.getLocalHost();
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to get local IP: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Error Handling and Logging
    
    private void HandleError(string message)
    {
        _consecutiveErrors++;
        LogError(message);
        
        if (_consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
        {
            LogError("Too many consecutive errors. Consider restarting the receiver.");
            _isListening = false;
            UpdateUIState();
        }
    }
    
    private void LogMessage(string message)
    {
        Debug.Log($"[HapticReceiver] {message}");
        UpdateDebugText(message);
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[HapticReceiver] ERROR: {message}");
        UpdateDebugText($"ERROR: {message}");
    }
    
    private void UpdateDebugText(string message)
    {
        if (debugText != null)
        {
            debugText.text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }
    }
    
    #endregion
    
    #region Cleanup
    
    private void CleanupResources()
    {
        try
        {
            StopAllHaptics();
            
            if (_continuousHapticClip != null) DestroyImmediate(_continuousHapticClip);
            if (_instantHapticClip != null) DestroyImmediate(_instantHapticClip);
            
            LogMessage("Resources cleaned up");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during cleanup: {ex.Message}");
        }
    }
    
    #endregion
}