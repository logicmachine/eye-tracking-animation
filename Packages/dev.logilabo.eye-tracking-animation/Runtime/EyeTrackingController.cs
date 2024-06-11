using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

// ReSharper disable once CheckNamespace
namespace dev.logilabo.eye_tracking_animation.runtime
{
    [Serializable]
    public struct GazeMotionSet
    {
        public Motion neutral;
        public Motion inDown;
        public Motion inUp;
        public Motion outDown;
        public Motion outUp;
    }

    [Serializable]
    public struct GazeMotionSetEntry
    {
        public VRCAvatarDescriptor.AnimLayerType layerType;
        public GazeMotionSet left;
        public GazeMotionSet right;
    }
    
    public class EyeTrackingController : MonoBehaviour, IEditorOnly
    {
        public float closeTransitionTime = 0.03f;
        public float openTransitionTime = 0.05f;
        public float localSmoothness = 0.1f;
        public float remoteSmoothness = 0.7f;
        public AnimationClip enableClip = null;
        public AnimationClip disableClip = null;
        public Motion leftCloseClip = null;
        public Motion rightCloseClip = null;
        public List<GazeMotionSetEntry> gazeMotions = new List<GazeMotionSetEntry>();
    }
}
