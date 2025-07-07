using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class HapticEventBehaviour : PlayableBehaviour
{
    public Lofelt.NiceVibrations.HapticClip hapticMaterial;
    public int priority = 2;
    private bool _firstFrameHappened = false;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!Application.isPlaying || _firstFrameHappened) return;

        bool shouldPlayHaptic = Lofelt.NiceVibrations.HapticController.IsPlaying(); 

        if (shouldPlayHaptic)
        {
            Lofelt.NiceVibrations.HapticController.Stop( );
            Lofelt.NiceVibrations.HapticController.Play(hapticMaterial);
            _firstFrameHappened = true;
            Debug.Log("played haptic " + hapticMaterial.name);
        }
        else
        {
            Debug.Log("Haptic not played: " + hapticMaterial.name + " priority too low");
        }
    }
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        _firstFrameHappened = false;
    }
}
