using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Audio;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Audio;
using Timeline.Settings;
using Timeline.Utils;
using Timeline.WorldRecording.Components;
using Timeline.WorldRecording.Recorders;
using UnityEngine;

namespace Timeline.WorldRecording.Utils
{
    public class RecordingUtils
    {

        public static MarrowEntity GetEntityFromObject(GameObject gameObject) {
            
            return gameObject.GetComponentInParent<MarrowEntity>(true);
        }

        public static T GetMarrowEntityRecorderFromGameObject<T>(GameObject gameObject, bool createIfNonExistant = false) where T : MarrowEntityRecorder {
            MarrowEntity marrowEntity = GetEntityFromObject(gameObject);

            if (marrowEntity) {
                MarrowEntityRecorder marrowEntityRecorder = MarrowEntityRecorder.TryGetRecorderFromCache(marrowEntity);

                if (marrowEntityRecorder != null)
                {
                    return (T) marrowEntityRecorder;
                }
                else {
                    if (createIfNonExistant) {
                        Type properRecorderType = GetProperRecorderTypeForEntity(marrowEntity);

                        if (properRecorderType == null) {
                            return null;
                        }

                        MarrowEntityRecorder recorderCreated = (MarrowEntityRecorder) Activator.CreateInstance(properRecorderType);
                        recorderCreated.useWorldEntity = GlobalSettings.useWorldObject;
                        recorderCreated.recording = true;
                        recorderCreated.scale = marrowEntity.transform.localScale;
                        recorderCreated.OnInitializedRecording(marrowEntity);

                        // Capture the first frame it was ever marked as recorded because otherwise it might be slightly late due to the timestep recording process
                        recorderCreated.ForceCapture(0);

                        TryAddRecorderComponentsToBodies(marrowEntity);

                        if (ShouldHaveInitTime(marrowEntity)) {
                            // Create entity recorder with initialization time of the current playhead time.
                            // This allows for people to pull out mags without them already being in the scene
                            recorderCreated.initTime = WorldPlayer.playHead;
                        }
                        
                        // Was created
                        TimelineMainClass.timelineHolder.worldPlayer.AddRecorderToRecord(recorderCreated);

                        return (T) recorderCreated;
                    }
                }
            }

            return null;
        }

        public static void TryAddRecorderComponentsToBodies(MarrowEntity root) {
            foreach (var body in root.Bodies) {
                if (body.GetComponent<CollisionRecorder>()) {
                    continue;
                }

                body.gameObject.AddComponent<CollisionRecorder>();
            }
        }

        // Is it a mag or bullet casing/shell
        private static bool ShouldHaveInitTime(MarrowEntity marrowEntity) {
            if (marrowEntity.GetComponentInChildren<CartridgeData>() || marrowEntity.GetComponentInChildren<FirearmCartridge>()) {
                return true;
            }

            if (marrowEntity.GetComponentInChildren<Magazine>())
            {
                return true;
            }

            return false;
        }

        private static Type GetProperRecorderTypeForEntity(MarrowEntity marrowEntity) {

            // We certainly do NOT want to spawn an entire rig.
            if (marrowEntity.GetComponentInChildren<PhysicsRig>())
            {
                return null;
            }

            if (marrowEntity.GetComponentInChildren<Gun>()) {
                return typeof(GunRecorder);
            }

            if (marrowEntity.GetComponentInChildren<Atv>())
            {
                return typeof(ATVRecorder);
            }

            return typeof(MarrowEntityRecorder);
        }

        public static void AttemptBeginRecordingSequence()
        {
            if (WorldPlayer.recording)
            {
                return;
            }

            TimelineMainClass.timelineHolder.worldPlayer.Stop();

            // Playhead at 0, no callback call. Its like it never happened....
            TimelineMainClass.timelineHolder.worldPlayer.Play(0, false);

            // Pre record to allow for people to pickup props to use during the (Ding ding ding) thing.
            WorldPlayer.recording = true;

            TimerManager.DelayAction(0.1f, () =>
            {
                // Freeze worldplayer
                TimelineMainClass.timelineHolder.worldPlayer.Pause();

                Audio3dManager.Play2dOneShot(TimelineAssets.singularBeep, Audio3dManager.hardInteraction, new Il2CppSystem.Nullable<float>(1f), new Il2CppSystem.Nullable<float>(1f));
            });

            TimerManager.DelayAction(1f, () =>
            {
                Audio3dManager.Play2dOneShot(TimelineAssets.singularBeep, Audio3dManager.hardInteraction, new Il2CppSystem.Nullable<float>(1f), new Il2CppSystem.Nullable<float>(1f));
            });

            TimerManager.DelayAction(2f, () =>
            {
                Audio3dManager.Play2dOneShot(TimelineAssets.singularBeep, Audio3dManager.hardInteraction, new Il2CppSystem.Nullable<float>(1f), new Il2CppSystem.Nullable<float>(2f));

                if (GlobalSettings.recordAvatar)
                {
                    RigmanagerRecorder recorder = new RigmanagerRecorder();
                    recorder.OnInitializedRecording(Player.RigManager);

                    TimelineMainClass.timelineHolder.worldPlayer.AddRecorderToRecord(recorder);

                    WorldPlayer.currentRigmanagerRecorder = recorder;
                }

                AudioClip referenceTrack = null;

                if (GlobalSettings.referenceTrackName != "None") {
                    referenceTrack = TimelineAudioManager.AttemptLoad(GlobalSettings.referenceTrackName);
                }

                TimelineMainClass.timelineHolder.worldPlayer.StartRecording(true, referenceTrack);
                WorldPlayer.playHead = 0f;
            });
        }
    }
}
