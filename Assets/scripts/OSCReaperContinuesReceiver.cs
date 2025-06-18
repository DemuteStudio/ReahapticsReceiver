using System;
using System.Text;
using extOSC;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Interhaptics;
using Interhaptics.Core;

public class OSCReaperContinuesReceiver : MonoBehaviour
{
    // Constants
    [SerializeField] private TextMeshProUGUI text;

    [Header("OSC variables")]
    public string hapticAddress = "/HapticJson";
    public string instantHapticAddress = "/InstantHapticJson";
    public string timeAddress = "/CursorPos";
    public string startStopAddress = "/StartStop";
    public int port = 7401;
    private OSCReceiver _receiver;

    [Header("UI elements")]
    public ViewManager viewManager;
    public Button playHapticButton;
    public Button toggleConnectButton;
    public Button loadHapticButton;
    public GameObject connectedLight;
    public GameObject connectedLightGreen;
    public TextMeshProUGUI hapticNameText;
    public TextMeshProUGUI IPText;
    public TMP_InputField PortInput;
    public GameObject WarningPanel;
    public Button WarningPanelButton;
    public Button importViewButton;

    // Variables
    private bool _isCursorMoving = false;
    private float _timePos = 0;
    private bool _toPlayHaptic = false;
    private float _timeToPlayHaptic = 0;
    private bool _isListeneing = false;

    [SerializeField] private HapticClip _continioushapticClip;
    [SerializeField] private HapticClip _instantHapticClip;

    private HapticMaterial _continueshapticMaterial;
    private HapticMaterial _instanthapticMaterial;

    private bool useNiceVibrations = true;

    void Start()
    {
        InitializeUI();
        InitializeHapticClips();
        InitializeOSCReceiver();
        Invoke("SetIp", 1);
    }

    private void InitializeUI()
    {
        importViewButton.onClick.AddListener(viewManager.ShowImportView);
        toggleConnectButton.onClick.AddListener(ToggleListening);
        playHapticButton.onClick.AddListener(PlayInstandHaptic);
        WarningPanelButton.onClick.AddListener(CloseWarningPanel);
        PortInput.onEndEdit.AddListener(OnPortIntputChanged);
        PortInput.text = port.ToString();
    }

    private void InitializeHapticClips()
    {
        _continioushapticClip = ScriptableObject.CreateInstance<HapticClip>();
        _instantHapticClip = ScriptableObject.CreateInstance<HapticClip>();
    }

    private void InitializeOSCReceiver()
    {
        _receiver = gameObject.AddComponent<OSCReceiver>();
        _receiver.LocalPort = port;

        _receiver.Bind(startStopAddress, ReceivedMessage);
        _receiver.Bind(hapticAddress, ReceivedMessage);
        _receiver.Bind(timeAddress, ReceivedMessage);
        _receiver.Bind(instantHapticAddress, ReceivedMessage);

        Debug.Log($"Listening for OSC messages on port {port}");
    }

    void SetIp()
    {
        IPText.text = _receiver.getLocalHost();
    }

    public void SetHapticsMethod(int val)
    {
        useNiceVibrations = val == 0;
        Debug.Log($"Use {(useNiceVibrations ? "Nice Vibrations" : "InterHaptics")}");
    }

    private void CloseWarningPanel()
    {
        WarningPanel.SetActive(false);
    }

    private void OnPortIntputChanged(string value)
    {
        if (int.TryParse(value, out int port))
        {
            _receiver.LocalPort = port;
            Debug.Log($"Port changed to {port}");
        }
    }

    void ToggleListening()
    {
        _isListeneing = !_isListeneing;

        playHapticButton.gameObject.SetActive(!_isListeneing);
        loadHapticButton.gameObject.SetActive(!_isListeneing);
        connectedLight.SetActive(_isListeneing);

        HapticController.Stop();
    }

    private void Update()
    {
        if (_isCursorMoving && _isListeneing)
        {
            _timePos += Time.deltaTime;

            if (_timePos >= _timeToPlayHaptic && _toPlayHaptic)
            {
                PlayHaptic();
                _timeToPlayHaptic = 0;
                _toPlayHaptic = false;
            }
        }
    }

    private void PlayHaptic()
    {
        if (useNiceVibrations)
        {
            Debug.Log("play haptic with NiceVibrations at: " + _timePos);
            HapticController.fallbackPreset = HapticPatterns.PresetType.Success;
            HapticController.Play(_instantHapticClip);
            Debug.Log(_instantHapticClip.json.ToString());
        }
        else
        {
            Debug.Log("play haptic with InterHaptics at: " + _timePos);
            HAR.PlayHapticEffect(_continueshapticMaterial);
        }
    }

    private void PlayInstandHaptic()
    {
        if (useNiceVibrations)
        {
            Debug.Log("play haptic with NiceVibrations at: " + _timePos);
            HapticController.fallbackPreset = HapticPatterns.PresetType.Success;
            HapticController.Play(_instantHapticClip);
            Debug.Log(_instantHapticClip.json.ToString());
        }
        else
        {
            Debug.Log("play haptic with InterHaptics at: " + _timePos);
            HAR.PlayHapticEffect(_instanthapticMaterial);
        }
    }

    private void StopHaptic()
    {
        HapticController.Stop();
    }

    private void ReceivedMessage(OSCMessage message)
    {
        if (!_isListeneing && message.Address == instantHapticAddress)
        {
            ProcessInstantHapticMessage(message);
            return;
        }

        if (_isListeneing == false) return;

        switch (message.Address)
        {
            case var _ when message.Address == hapticAddress:
                ProcessHapticMessage(message);
                break;

            case var _ when message.Address == timeAddress:
                _timePos = message.Values[0].FloatValue;
                break;

            case var _ when message.Address == startStopAddress:
                ProcessStartStopMessage(message);
                break;
        }
    }

    private void ProcessInstantHapticMessage(OSCMessage message)
    {
        string input = message.Values[0].StringValue;
        int newlineIndex = input.IndexOf('\n');

        string namePart = input.Substring(0, newlineIndex).Replace("name: ", "").Trim();
        string jsonPart = input.Substring(newlineIndex + 1);

        Debug.Log(namePart);
        Debug.Log(jsonPart);

        // Nice Vibrations
        var parsedJson = HapticConverter.ConvertToJsonNiceVibrations(jsonPart);
        _instantHapticClip = HapticImporter.JsonToHapticClip(Encoding.UTF8.GetBytes(parsedJson));
        hapticNameText.text = namePart;

        // InterHaptics
        var parsedJsonInteHaptics = HapticConverter.ConvertToJsonInterHaptics(jsonPart);
        Debug.Log(parsedJsonInteHaptics);
        _instanthapticMaterial = HapticMaterial.CreateInstanceFromString(parsedJsonInteHaptics);
        _instanthapticMaterial.name = namePart;
        Debug.Log(_instanthapticMaterial.text);
    }

    private void ProcessHapticMessage(OSCMessage message)
    {
        string input = message.Values[0].StringValue;
        int newlineIndex = input.IndexOf('\n');

        string floatPart = input.Substring(0, newlineIndex).Replace("SendTime: ", "").Trim();
        float.TryParse(floatPart, out float sendTime);
        _timeToPlayHaptic = sendTime;
        _toPlayHaptic = true;

        string jsonPart = input.Substring(newlineIndex + 1);

        // Nice Vibrations
        var parsedJson = HapticConverter.ConvertToJsonNiceVibrations(jsonPart);
        Debug.Log(parsedJson);
        _continioushapticClip = HapticImporter.JsonToHapticClip(Encoding.UTF8.GetBytes(parsedJson));

        // InterHaptics
        var parsedJsonInteHaptics = HapticConverter.ConvertToJsonInterHaptics(jsonPart);
        Debug.Log(parsedJsonInteHaptics);
        _continueshapticMaterial = HapticMaterial.CreateInstanceFromString(parsedJsonInteHaptics);
        _continueshapticMaterial.name = "ContinuesHaptic";
        Debug.Log(_continueshapticMaterial.text);
    }

    private void ProcessStartStopMessage(OSCMessage message)
    {
        string s = message.Values[0].StringValue;

        switch (s)
        {
            case "started":
                _isCursorMoving = true;
                Debug.Log(s);
                connectedLightGreen.SetActive(true);
                break;

            case "stopped":
                _toPlayHaptic = false;
                _isCursorMoving = false;
                StopHaptic();
                Debug.Log(s);
                connectedLightGreen.SetActive(false);
                break;

            case "moved":
                _toPlayHaptic = false;
                StopHaptic();
                Debug.Log(s);
                break;
        }
    }
}