using System;
using System.Collections.Generic;
using System.Linq;

using VRC.SDK3.Avatars.Components;

using nadena.dev.ndmf;

using AnimatorAsCode.V1;
using AnimatorAsCode.V1.NDMFProcessor;
using AnimatorAsCode.V1.ModularAvatar;
using AnimatorAsCode.V1.VRC;
using dev.logilabo.eye_tracking_animation.runtime;
using dev.logilabo.eye_tracking_animation.editor;

[assembly: ExportsPlugin(typeof(EyeTrackingGeneratorPlugin))]

// ReSharper disable once CheckNamespace
namespace dev.logilabo.eye_tracking_animation.editor
{
    public class EyeTrackingGeneratorPlugin : AacPlugin<EyeTrackingController>
    {
        private const string ParameterPrefix = "EyeTrackingController/v2/";

        private void CreateInitializationLayer(AacFlController controller)
        {
            var layer = controller.NewLayer();
            var enableParam = layer.BoolParameter(ParameterPrefix + "Enable");
            var trackOnDisableParam = layer.BoolParameter(ParameterPrefix + "TrackOnDisable");

            var disableMotion = my.disableClip;
            var enableMotion = my.enableClip;
            if (disableMotion == null || enableMotion == null)
            {
                var emptyMotion = aac.NewClip().Clip;
                if (disableMotion == null) { disableMotion = emptyMotion; }
                if (enableMotion == null) { enableMotion = emptyMotion; }
            }

            var disableTracks = layer.NewState("Disable Tracks (WD Off)")
                .WithAnimation(disableMotion)
                .TrackingTracks(AacAv3.Av3TrackingElement.Eyes);
            var disableAnimates = layer.NewState("Disable Animates (WD Off)")
                .WithAnimation(enableMotion)
                .TrackingAnimates(AacAv3.Av3TrackingElement.Eyes);
            var enable = layer.NewState("Enable (WD Off)")
                .WithAnimation(enableMotion)
                .TrackingAnimates(AacAv3.Av3TrackingElement.Eyes);

            layer.AnyTransitionsTo(disableTracks)
                .WithNoTransitionToSelf()
                .When(enableParam.IsFalse())
                .And(trackOnDisableParam.IsTrue());
            layer.AnyTransitionsTo(disableAnimates)
                .WithNoTransitionToSelf()
                .When(enableParam.IsFalse())
                .And(trackOnDisableParam.IsFalse());
            // TODO tracking control may ignored without transition to self?
            layer.AnyTransitionsTo(enable)
                .WithTransitionToSelf()
                .When(enableParam.IsTrue());
        }
        
        private void CreateGazeLayer(AacFlController controller, VRCAvatarDescriptor.AnimLayerType layerType)
        {
            var layer = controller.NewLayer("Gaze");
            
            var oneParam = layer.FloatParameter(ParameterPrefix + "One");
            layer.OverrideValue(oneParam, 1.0f);
            
            var enableParam = layer.BoolParameter(ParameterPrefix + "Enable");
            var leftInOutParam = layer.FloatParameter(ParameterPrefix + "LInOut/Smoothed");
            var rightInOutParam = layer.FloatParameter(ParameterPrefix + "RInOut/Smoothed");
            var pitchParam = layer.FloatParameter(ParameterPrefix + "Pitch/Smoothed");

            var disableTree = aac.NewBlendTree().Direct();
            var enableTree = aac.NewBlendTree().Direct();
            foreach (var set in my.gazeMotions.Where(set => set.layerType == layerType))
            {
                // Disabled: set to center
                disableTree.WithAnimation(set.left.neutral, oneParam);
                disableTree.WithAnimation(set.right.neutral, oneParam);
                // Enabled
                enableTree.WithAnimation(
                    aac.NewBlendTree()
                        .SimpleDirectional2D(leftInOutParam, pitchParam)
                        .WithAnimation(set.left.inDown, -1.0f, -1.0f)
                        .WithAnimation(set.left.inUp, -1.0f, 1.0f)
                        .WithAnimation(set.left.outDown, 1.0f, -1.0f)
                        .WithAnimation(set.left.outUp, 1.0f, 1.0f),
                    oneParam);
                enableTree.WithAnimation(
                    aac.NewBlendTree()
                        .SimpleDirectional2D(rightInOutParam, pitchParam)
                        .WithAnimation(set.right.inDown, -1.0f, -1.0f)
                        .WithAnimation(set.right.inUp, -1.0f, 1.0f)
                        .WithAnimation(set.right.outDown, 1.0f, -1.0f)
                        .WithAnimation(set.right.outUp, 1.0f, 1.0f),
                    oneParam);
            }
            
            var disable = layer.NewState("Disable (WD On)")
                .WithAnimation(disableTree)
                .WithWriteDefaultsSetTo(true)
                .TrackingTracks(AacAv3.Av3TrackingElement.Eyes);
            var enable = layer.NewState("Enable (WD On)")
                .WithAnimation(enableTree)
                .WithWriteDefaultsSetTo(true)
                .TrackingAnimates(AacAv3.Av3TrackingElement.Eyes);
            
            layer.AnyTransitionsTo(disable)
                .WithNoTransitionToSelf()
                .When(enableParam.IsFalse());
            layer.AnyTransitionsTo(enable)
                .WithNoTransitionToSelf()
                .When(enableParam.IsTrue());
        }

        private void CreateCloseLayer(AacFlController controller, int eye)
        {
            var prefix = eye == 0 ? "L" : "R";
            var layer = controller.NewLayer(prefix + "Close");

            var oneParam = layer.FloatParameter(ParameterPrefix + "One");
            layer.OverrideValue(oneParam, 1.0f);
            var enableParam = layer.BoolParameter(ParameterPrefix + "Enable");
            var localClosenessParam = layer.FloatParameter(ParameterPrefix + prefix + "Closeness");
            var remoteCloseParam = layer.BoolParameter(ParameterPrefix + prefix + "CloseRemote");
            var blinkableParam = layer.BoolParameter(ParameterPrefix + prefix + "Blinkable");
            var blinkCounterParams = new[]
            {
                layer.BoolParameter(ParameterPrefix + "Blink0"),
                layer.BoolParameter(ParameterPrefix + "Blink1"),
            };

            var emptyClip = aac.NewClip().Clip;
            var closeClip = eye == 0 ? my.leftCloseClip : my.rightCloseClip;

            var disable = layer.NewState("Disable (WD Off)")
                .WithAnimation(emptyClip)
                .WithWriteDefaultsSetTo(false);

            // Local
            var localClose = layer.NewState("Local Close (WD Off)")
                .WithAnimation(aac.NewBlendTree()
                    .Simple1D(localClosenessParam)
                    .WithAnimation(emptyClip, 0.0f)
                    .WithAnimation(closeClip, 1.0f))
                .WithWriteDefaultsSetTo(false);
            var localOpen = layer.NewState("Local Open (WD Off)")
                .WithAnimation(emptyClip)
                .WithWriteDefaultsSetTo(false);
            
            disable.TransitionsTo(localClose)
                .When(layer.Av3().ItIsLocal())
                .And(enableParam.IsTrue())
                .And(blinkableParam.IsTrue());
            disable.TransitionsTo(localOpen)
                .When(layer.Av3().ItIsLocal())
                .And(enableParam.IsTrue())
                .And(blinkableParam.IsFalse());

            localClose.TransitionsTo(disable)
                .When(layer.Av3().ItIsRemote())
                .Or().When(enableParam.IsFalse());
            localOpen.TransitionsTo(disable)
                .When(layer.Av3().ItIsRemote())
                .Or().When(enableParam.IsFalse());

            localClose.TransitionsTo(localOpen)
                .WithTransitionDurationSeconds(my.closeTransitionTime)
                .When(blinkableParam.IsFalse());
            localOpen.TransitionsTo(localClose)
                .WithTransitionDurationSeconds(my.openTransitionTime)
                .When(blinkableParam.IsTrue());

            // Remote
            var n = 1 << blinkCounterParams.Length;
            var opens = new List<AacFlState>();
            var closes = new List<AacFlState>();
            var blinks = new List<AacFlState>();
            for (int i = 0; i < n; ++i)
            {
                opens.Add(layer.NewState("Open " + i + " (WD Off)")
                    .WithAnimation(emptyClip)
                    .WithWriteDefaultsSetTo(false));
                closes.Add(layer.NewState("Close " + i + " (WD Off)")
                    .WithAnimation(closeClip)
                    .WithWriteDefaultsSetTo(false));
                blinks.Add(layer.NewState("Blink " + i + " (WD Off)")
                    .WithAnimation(closeClip)
                    .WithWriteDefaultsSetTo(false));
            }
            for (var i = 0; i < n; ++i)
            {
                var next = (i + 1) % n;
                var lo = (i & 1) != 0;
                var hi = i >> 1 != 0;

                disable.TransitionsTo(closes[i])
                    .WithTransitionDurationSeconds(my.closeTransitionTime)
                    .When(layer.Av3().IsLocal.IsFalse())
                    .And(blinkableParam.IsTrue())
                    .And(enableParam.IsTrue())
                    .And(remoteCloseParam.IsTrue())
                    .And(blinkCounterParams[0].IsEqualTo(lo))
                    .And(blinkCounterParams[1].IsEqualTo(hi));
                disable.TransitionsTo(opens[i])
                    .WithTransitionDurationSeconds(my.openTransitionTime)
                    .When(layer.Av3().IsLocal.IsFalse())
                    .And(blinkableParam.IsTrue())
                    .And(enableParam.IsTrue())
                    .And(remoteCloseParam.IsFalse())
                    .And(blinkCounterParams[0].IsEqualTo(lo))
                    .And(blinkCounterParams[1].IsEqualTo(hi));

                closes[i].TransitionsTo(disable)
                    .WithTransitionDurationSeconds(my.openTransitionTime)
                    .When(layer.Av3().IsLocal.IsTrue())
                    .Or().When(blinkableParam.IsFalse())
                    .Or().When(enableParam.IsFalse());
                opens[i].TransitionsTo(disable)
                    .WithTransitionDurationSeconds(my.openTransitionTime)
                    .When(layer.Av3().IsLocal.IsTrue())
                    .Or().When(blinkableParam.IsFalse())
                    .Or().When(enableParam.IsFalse());
                blinks[i].TransitionsTo(disable)
                    .WithTransitionDurationSeconds(my.openTransitionTime)
                    .When(layer.Av3().IsLocal.IsTrue())
                    .Or().When(blinkableParam.IsFalse())
                    .Or().When(enableParam.IsFalse());

                blinks[i].TransitionsTo(opens[i])
                    .WithTransitionDurationSeconds(my.openTransitionTime)
                    .When(remoteCloseParam.IsFalse());
                blinks[i].TransitionsTo(closes[i])
                    .When(remoteCloseParam.IsTrue());

                opens[i].TransitionsTo(closes[i])
                    .WithTransitionDurationSeconds(my.closeTransitionTime)
                    .When(remoteCloseParam.IsTrue());

                closes[i].TransitionsTo(opens[i])
                    .WithTransitionDurationSeconds(my.openTransitionTime)
                    .When(remoteCloseParam.IsFalse());

                closes[i].TransitionsTo(blinks[next])
                    .When(blinkCounterParams[0].IsNotEqualTo(lo))
                    .Or().When(blinkCounterParams[1].IsNotEqualTo(hi));
                opens[i].TransitionsTo(blinks[next])
                    .WithTransitionDurationSeconds(my.closeTransitionTime)
                    .When(blinkCounterParams[0].IsNotEqualTo(lo))
                    .Or().When(blinkCounterParams[1].IsNotEqualTo(hi));
            }
        }

        private AacFlController CreateController(VRCAvatarDescriptor.AnimLayerType layerType)
        {
            var hasGazeMotions = my.gazeMotions.Any(s => s.layerType == layerType);
            if (!hasGazeMotions && layerType != VRCAvatarDescriptor.AnimLayerType.FX) { return null; }

            var controller = aac.NewAnimatorController();
            if (layerType == VRCAvatarDescriptor.AnimLayerType.FX)
            {
                CreateInitializationLayer(controller);
                CreateCloseLayer(controller, 0);
                CreateCloseLayer(controller, 1);
            }
            if (hasGazeMotions) { CreateGazeLayer(controller, layerType); }
            return controller;
        }

        protected override AacPluginOutput Execute()
        {
            var maAc = MaAc.Create(my.gameObject);
            foreach (var value in Enum.GetValues(typeof(VRCAvatarDescriptor.AnimLayerType)))
            {
                var layerType = (VRCAvatarDescriptor.AnimLayerType)value;
                var controller = CreateController(layerType);
                if (controller != null)
                {
                    maAc.NewMergeAnimator(controller, layerType);
                }
            }
            return AacPluginOutput.Regular();
        }
    }
}
