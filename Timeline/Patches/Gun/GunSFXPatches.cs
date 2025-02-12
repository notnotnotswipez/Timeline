using BoneLib;
using Il2CppSLZ.Marrow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Recorders;
using Timeline.WorldRecording.Utils;
using Timeline.WorldRecording;
using HarmonyLib;
using Timeline.WorldRecording.Events.BuiltIn;

namespace Timeline.Patches.Gun
{

    [HarmonyPatch(typeof(GunSFX), nameof(GunSFX.MagazineInsert))]
    public class MagazineInsertSFXPatch
    {
        public static void Postfix(GunSFX __instance)
        {
            if (WorldPlayer.recording) {
                GunRecorder gunRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<GunRecorder>(__instance.gameObject);
                if (gunRecorder != null)
                {
                    if (gunRecorder.recording) {
                        gunRecorder.AddEvent(WorldPlayer.playHead, new GunEvent(GunEventTypes.MAG_INSERT_SFX));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GunSFX), nameof(GunSFX.MagazineDrop))]
    public class MagazineDropSFXPatch
    {
        public static void Postfix(GunSFX __instance)
        {
            if (WorldPlayer.recording)
            {
                GunRecorder gunRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<GunRecorder>(__instance.gameObject);
                if (gunRecorder != null)
                {
                    if (gunRecorder.recording)
                    {
                        gunRecorder.AddEvent(WorldPlayer.playHead, new GunEvent(GunEventTypes.MAG_DROP_SFX));
                    }
                }
            }
        }
    }
}
