using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HapticRecieverSettings : MonoBehaviour
{
    [Header("Reaper view")]
    public ViewManager viewManager;
    public TMP_Dropdown hapticDropdown;
    public HapticTester myhapticTester;
    public OSCReaperContinuesReceiver  myOSCReaperContinuesReceiver;
    public Button closeSettingsButton;
    public Button DocsButton;
    void Start()
    {
        closeSettingsButton.onClick.AddListener(viewManager.HideSettingsView);
        hapticDropdown.onValueChanged.AddListener(SetHapticPlaybackMethod);
        DocsButton.onClick.AddListener(GoToDocs);
        hapticDropdown.ClearOptions();
        hapticDropdown.options.Add(new TMP_Dropdown.OptionData("Nice Vibrations"));
        hapticDropdown.options.Add(new TMP_Dropdown.OptionData("InterHaptics"));
        //hapticDropdown.options.Add(new TMP_Dropdown.OptionData("RichTab"));
    }

    private void OnEnable()
    {
        myOSCReaperContinuesReceiver.UpdateIPDisplay();
    }

    private void SetHapticPlaybackMethod(int val)
    {
        //myhapticTester.SetHapticsMethod(val);
        myOSCReaperContinuesReceiver.SetHapticsMethod(val);
    }
    
    private void GoToDocs()
    {
        Application.OpenURL("https://github.com/DemuteStudio/ReaHaptic");
    }
}
