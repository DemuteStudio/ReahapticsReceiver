using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC; // Use the library of your choice
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine.UI;
using System.Text;

public class OSCReaperInstantReceiver : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI Text;

    public string address = "/HapticJson";
    public int port = 7401; // Match this to the port Reaper sends to

    private OSCReceiver _receiver;
    
    private HapticClip hapticMaterial;
    
    public Button playHapticButton;
    
    void Start()
    {
        playHapticButton.onClick.AddListener(PlayHaptic);
        hapticMaterial = ScriptableObject.CreateInstance<HapticClip>();
        
        if (DeviceCapabilities.meetsAdvancedRequirements == true)
        {
            Debug.Log("Haptics supported");
        }
        else
        {
            Debug.Log("Haptics not supported");
        }
        
        _receiver = gameObject.AddComponent<OSCReceiver>();
        _receiver.LocalPort = port;

        // Bind handler for received messages
        _receiver.Bind(address, message => ReceivedMessage(message));
        
        Debug.Log($"Listening for OSC messages on port {port}");
    }

    private void PlayHaptic()
    {
        HapticController.Play(hapticMaterial);
    }

    private void ReceivedMessage(OSCMessage message)
    {
        //Debug.Log(message.Values[0].StringValue);
        Text.text = message.Values[0].StringValue;
        hapticMaterial.name = "ReaperHaptic";
        hapticMaterial.json = Encoding.UTF8.GetBytes(message.Values[0].StringValue);
        
        HapticController.Play(hapticMaterial);
    }
}
