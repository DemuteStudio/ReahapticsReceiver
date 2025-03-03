using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HapticRecieverSettings : MonoBehaviour
{
    public TMP_Dropdown hapticDropdown;
    public HapticTester myhapticTester;
    public OSCReaperContinuesReceiver myOSCReaperContinuesReceiver;
    void Start()
    {
        hapticDropdown.onValueChanged.AddListener(SetHapticPlaybackMethod);
        hapticDropdown.ClearOptions();
        hapticDropdown.options.Add(new TMP_Dropdown.OptionData("Nice Vibrations"));
        hapticDropdown.options.Add(new TMP_Dropdown.OptionData("InterHaptics"));
    }

    private void SetHapticPlaybackMethod(int val)
    {
        myhapticTester.SetHapticsMethod(val);
        myOSCReaperContinuesReceiver.SetHapticsMethod(val);
    }
}
