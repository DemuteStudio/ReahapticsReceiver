using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


[Serializable]
public class HapticEventClip : PlayableAsset
{
    public Lofelt.NiceVibrations.HapticClip  hapticMaterial;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<HapticEventBehaviour>.Create(graph);
        HapticEventBehaviour hapticEventBehaviour = playable.GetBehaviour();
        hapticEventBehaviour.hapticMaterial = hapticMaterial;

        return playable;
    }
    

}
