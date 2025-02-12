using BoneLib;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.VRMK;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.Recorders;

namespace Timeline.Patches.Rigmanager
{
    [HarmonyPatch(typeof(ArtRig), nameof(ArtRig.SetArtOutputAvatar))]
    public class RigmanagerAvatarSwitchEventPatch
    {
        public static void Postfix(ArtRig __instance, PhysicsRig inRig, Avatar avatar) {
            if (WorldPlayer.currentRigmanagerRecorder != null)
            {
                if (inRig.manager.GetInstanceID() == Player.RigManager.GetInstanceID())
                {
                    // We are recording ourselves
                    MelonCoroutines.Start(WaitForRigToUpdateAvatar(inRig.manager));
                }
            }
        }

        private static IEnumerator WaitForRigToUpdateAvatar(RigManager manager) {
            for (int i = 0; i < 3; i++) {
                yield return null;
            }
            string barcode = manager._avatarCrate.Barcode.ID;

            if (WorldPlayer.currentRigmanagerRecorder.currentAvatarBarcode != barcode) {
                AvatarSwapEvent avatarSwapEvent = new AvatarSwapEvent(barcode);

                
                WorldPlayer.currentRigmanagerRecorder.AddEvent(WorldPlayer.playHead, avatarSwapEvent);
                WorldPlayer.currentRigmanagerRecorder.AssignAvatarBones(manager.avatar);
                WorldPlayer.currentRigmanagerRecorder.currentRecordingAvatar = Player.RigManager.avatar;

                PlayerArtPatch.ForgetJaw();
            }

            yield break;
        }
    }
}
