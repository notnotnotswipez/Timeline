using BoneLib;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Audio;
using Timeline.Settings;
using Timeline.WorldRecording;
using UnityEngine;

namespace Timeline.Patches.Rigmanager
{
    [HarmonyPatch(typeof(PlayerAvatarArt), nameof(PlayerAvatarArt.UpdateAvatarHead))]
    public class PlayerArtPatch
    {

        static Transform lastJawObject = null;
        static Quaternion lastCapturedJawRot = Quaternion.identity;
        public static bool prevCapturedJaw = false;

        public static void Prefix(PlayerAvatarArt __instance)
        {
            // We are recording
            if (WorldPlayer.recording)
            {
                // One manager in control of the record loop. (Incase a player is for some reason recording in Fusion.)
                if (__instance._openCtrlRig.manager.GetInstanceID() == Player.RigManager.GetInstanceID())
                {
                    if (!prevCapturedJaw) {
                        prevCapturedJaw = true;
                        MelonCoroutines.Start(WaitAndCollectJaw(__instance._openCtrlRig.manager.avatar.animator));
                    }

                    if (GlobalSettings.moveMouthToMicrophone) {
                        // Move jaw based on mic
                        MoveJaw(__instance._openCtrlRig.manager);
                    }

                    // Capture
                    // See RecordLoop comment for why this is done this way.
                    WorldPlayer.Instance.RecordLoop();
                }
            }
        }

        private static IEnumerator WaitAndCollectJaw(Animator animator) {
            for (int i = 0; i < 20; i++) {
                yield return null;
            }

            var jaw = animator.GetBoneTransform(HumanBodyBones.Jaw);
            if (jaw)
            {
                lastJawObject = jaw;
                lastCapturedJawRot = jaw.localRotation;
            }

            yield break;
        }

        public static void ForgetJaw() {
            if (lastJawObject) {
                lastJawObject.transform.localRotation = lastCapturedJawRot;
            }
            prevCapturedJaw = false;

            lastJawObject = null;
        }

        private static void MoveJaw(RigManager rigManager) {

            if (lastJawObject && lastJawObject.gameObject.activeInHierarchy) {
                float angle = 20f * TimelineAudioManager.GetMicrophoneLoudness();
                lastJawObject.localRotation = lastCapturedJawRot;

                lastJawObject.Rotate(rigManager.physicsRig.m_head.right, angle, Space.World);
            }
        }
    }

    // This is unnecessary but kind of cool anyway to see the mouth move in mirrors while you record.
    [HarmonyPatch(typeof(Mirror), nameof(Mirror.LateUpdate))]
    public class MirrorPatch
    {
        public static void Prefix(Mirror __instance)
        {
            if (!__instance.rigManager)
            {
                return;
            }

            if (!__instance._reflection)
            {
                return;
            }

            Transform mirrorJaw = __instance._reflection.animator.GetBoneTransform(HumanBodyBones.Jaw);
            Transform avatarJaw = __instance.rigManager.avatar.animator.GetBoneTransform(HumanBodyBones.Jaw);
            if (mirrorJaw && avatarJaw)
            {
                mirrorJaw.localRotation = avatarJaw.localRotation;
            }
        }
    }
}
