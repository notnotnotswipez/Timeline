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
using Il2CppSLZ.SFX;
using Timeline.WorldRecording.Extras.Impl;
using MelonLoader;

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

    [HarmonyPatch(typeof(SimpleSFX), nameof(SimpleSFX.AUDIOPLAY))]
    public class SimpleSFXAudioPatch
    {
        public static void Postfix(SimpleSFX __instance, int clipSpecific)
        {
            if (WorldPlayer.recording)
            {
                MarrowEntityRecorder entityRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(__instance.gameObject);
                if (entityRecorder != null)
                {
                    if (entityRecorder.recording)
                    {
                        int index = entityRecorder.FetchComponentManager<SimpleSFXComponentManager>().GetIndexFromComponent(__instance);
                        int remapped = index * 5;

                        if (clipSpecific >= 5) {
                            clipSpecific = 4;
                        }

                        remapped += clipSpecific;

                        entityRecorder.AddEvent(WorldPlayer.playHead, new OneshotComponentEvent(ComponentOneshots.SIMPLE_SFX, (byte) remapped));
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
