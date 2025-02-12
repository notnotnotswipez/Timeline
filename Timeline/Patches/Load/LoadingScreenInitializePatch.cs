using HarmonyLib;
using Il2CppSLZ.Bonelab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Audio;
using Timeline.Patches.PoolPatch;
using Timeline.WorldRecording;
using Timeline.WorldRecording.Recorders;

namespace Timeline.Patches.Load
{
    [HarmonyPatch(typeof(LoadingScene), nameof(LoadingScene.Start))]
    public class LoadingScreenInitializePatch
    {
        public static void Prefix() {
            // State reset!
            MarrowEntityRecorder.ClearAllEvents();
            MarrowEntityAwakePatch.ClearMarrowEntities();
            TimelineAudioManager.EndMicrophoneRecording((clip) => {
            
            });
            TimelineAudioManager.ClearAudioCache();
            TimelineMainClass.timelineHolder = null;
            WorldPlayer.recording = false;
            WorldPlayer.playing = false;
            WorldPlayer.paused = false;
            WorldPlayer.playHead = 0f;
        }
    }
}
