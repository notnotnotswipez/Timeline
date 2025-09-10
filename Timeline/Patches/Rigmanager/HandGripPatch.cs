using BoneLib;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Settings;
using Timeline.WorldRecording;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.Recorders;
using Timeline.WorldRecording.Utils;
using UnityEngine;
using UnityEngine.Profiling;

namespace Timeline.Patches.Rigmanager
{
    public class GripCheckMethods {

        public static bool IsOtherHandGrippingSameObject(Hand hand, Grip grip) {
            if (!hand.otherHand.AttachedReceiver) {
                return false;
            }

            if (grip._marrowEntity) {
                Grip otherHandGrip = hand.otherHand.AttachedReceiver.TryCast<Grip>();
                if (otherHandGrip) {
                    if (otherHandGrip._marrowEntity.GetInstanceID() == grip._marrowEntity.GetInstanceID()) {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandDrop))]
    public class InventorySlotDropPatch {
        public static void Postfix(InventorySlotReceiver __instance, IGrippable host) {
            if (WorldPlayer.recording && WorldPlayer.currentRigmanagerRecorder != null) {
                if (__instance.GetComponentInParent<RigManager>().GetInstanceID() == Player.RigManager.GetInstanceID()) {
                    MarrowEntityRecorder marrowEntityRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(__instance._slottedWeapon.gameObject, true);

                    if (marrowEntityRecorder == null)
                    {
                        return;
                    }

                    marrowEntityRecorder.PinToAvailableActorBone(WorldPlayer.currentRigmanagerRecorder, HumanBodyBones.Spine);
                }
            }
        }
    }


    [HarmonyPatch(typeof(Grip), nameof(Grip.OnAttachedToHand))]
    public class GripAttachedPatch {
        public static void Postfix(Grip __instance, Hand hand) {
            if (hand.manager.GetInstanceID() == Player.RigManager.GetInstanceID()) {
                if (WorldPlayer.recording) {

                    // This makes a new marrow entity recorder if it does not exist, and auto starts recording.
                    MarrowEntityRecorder marrowEntityRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(__instance.gameObject, true);

                    // It couldn't make a recorder, because there was no MarrowEntity. (Static world grip).
                    if (marrowEntityRecorder == null) {
                        return;
                    }

                    bool canPin = true;

                    if (!marrowEntityRecorder.recording) {

                        canPin = false;

                        if (GlobalSettings.transferActorProp) {
                            TimelineMainClass.timelineHolder.worldPlayer.OverrideRecording(marrowEntityRecorder);
                            canPin = true;
                        }
                    }

                    if (WorldPlayer.currentRigmanagerRecorder == null)
                    {
                        return;
                    }

                    if (!canPin)
                    {
                        // Attach the RIG recorder to the object instead
                        // We don't want to do this if we are currently sitting in something
                        if (!WorldPlayer.currentRigmanagerRecorder.wasPrevInSeat) {
                            WorldPlayer.currentRigmanagerRecorder.SetMarrowRecorderAnchor(marrowEntityRecorder);
                            WorldPlayer.currentRigmanagerRecorder.AddEvent(WorldPlayer.playHead, new RigRecorderAnchorEvent(true, marrowEntityRecorder.recorderID));
                        }
                    }
                    else {
                        

                        if (!GripCheckMethods.IsOtherHandGrippingSameObject(hand, __instance))
                        {
                            if (marrowEntityRecorder is ATVRecorder)
                            {
                                return;
                            }

                            if (Player.RigManager.activeSeat) {
                                // We are sitting in this thing
                                if (Player.RigManager.activeSeat.GetComponentInParent<MarrowEntity>().GetInstanceID() == marrowEntityRecorder.playbackEntity.GetInstanceID()) {
                                    return;
                                }
                            }

                            HumanBodyBones targetHandBone = HumanBodyBones.LeftHand;

                            if (hand.handedness == Il2CppSLZ.Marrow.Interaction.Handedness.RIGHT)
                            {
                                targetHandBone = HumanBodyBones.RightHand;
                            }

                            marrowEntityRecorder.PinToAvailableActorBone(WorldPlayer.currentRigmanagerRecorder, targetHandBone);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Grip), nameof(Grip.OnDetachedFromHand))]
    public class GripDetachPatch
    {
        public static void Postfix(Grip __instance, Hand hand)
        {
            if (hand.manager.GetInstanceID() == Player.RigManager.GetInstanceID())
            {
                if (WorldPlayer.recording)
                {
                    if (WorldPlayer.currentRigmanagerRecorder == null)
                    {
                        return;
                    }

                    MarrowEntityRecorder marrowEntityRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(__instance.gameObject, true);

                    if (marrowEntityRecorder == null)
                    {
                        return;
                    }

                    // We need to be in control of this object
                    if (marrowEntityRecorder.recording)
                    {
                        if (marrowEntityRecorder is ATVRecorder)
                        {
                            return;
                        }

                        if (!GripCheckMethods.IsOtherHandGrippingSameObject(hand, __instance))
                        {
                            // No hands are gripping this object now
                            marrowEntityRecorder.ClearPins();
                        }
                    }
                    else {

                        // If we are in a seat we are already pinned to our seat object (Ideally)
                        if (!WorldPlayer.currentRigmanagerRecorder.wasPrevInSeat)
                        {
                            if (!GripCheckMethods.IsOtherHandGrippingSameObject(hand, __instance))
                            {
                                // No hands are gripping this object now, so we clear our OWN pin
                                WorldPlayer.currentRigmanagerRecorder.SetPositionalAnchor(null);
                                WorldPlayer.currentRigmanagerRecorder.AddEvent(WorldPlayer.playHead, new RigRecorderAnchorEvent(false, 0));
                            }
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnFarHandHoverUpdate))]
    public class ForcePullPatch
    {
        public static void Postfix(ForcePullGrip __instance, Hand hand)
        {
            if (hand.manager.GetInstanceID() == Player.RigManager.GetInstanceID() && __instance.pullCoroutine != null)
            {
                if (WorldPlayer.recording)
                {

                    // This makes a new marrow entity recorder if it does not exist, and auto starts recording.
                    MarrowEntityRecorder marrowEntityRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(__instance.gameObject, true);

                    // It couldn't make a recorder, because there was no MarrowEntity. I don't even know if this is possible if you're able to forcegrip but whatever.
                    if (marrowEntityRecorder == null)
                    {
                        return;
                    }

                    // Playback, we take control.
                    // ONLY IF its not currently pinned to anything. If it is, its either part of a larger thing which is what should REALLY be grabbed, not the other thing.
                    if (!marrowEntityRecorder.recording && GlobalSettings.transferActorProp && !marrowEntityRecorder.hasPin)
                    {
                        TimelineMainClass.timelineHolder.worldPlayer.OverrideRecording(marrowEntityRecorder);
                    }
                }
            }
        }
    }
}
