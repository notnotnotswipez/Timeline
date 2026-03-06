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
using UnityEngine;
using Il2CppSLZ.Marrow.Utilities;

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

    [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.Play), new Type[0] { })]
    public class AudioSourcePlayPatch
    {
        public static void Postfix(AudioSource __instance)
        {
            if (WorldPlayer.recording)
            {
                int audioSourceInstanceId = __instance.GetInstanceID();


                if (AudioSourceComponentManager.cachedAudioSources.ContainsKey(audioSourceInstanceId)) {
                    ObjectRecorder cachedRecorder = AudioSourceComponentManager.cachedAudioSources[audioSourceInstanceId];

                    if (cachedRecorder.recording && __instance.clip && cachedRecorder is GunRecorder)
                    {
                        GunRecorder gunRecorder = (GunRecorder) cachedRecorder;
                        gunRecorder.AddEvent(WorldPlayer.playHead, new AudioSourceManualPlayEvent((byte) gunRecorder.FetchComponentManager<AudioSourceComponentManager>().GetIndexFromComponent(__instance), __instance.clip.name));
                    }
                }
            }
        }
    }
}
