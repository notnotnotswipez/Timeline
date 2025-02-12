using Il2CppSLZ.Marrow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization.Binary;
using Timeline.Serialization;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.StateCapturers;
using UnityEngine;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSystem;
using MelonLoader;

namespace Timeline.WorldRecording.Recorders
{
    public class ATVRecorder : MarrowEntityRecorder
    {
        public Atv playbackAtv;
        public Atv recordingAtv;
        public int previousAtvInstanceId = -1;
        public bool satIn = false;

        private static Dictionary<int, ATVRecorder> recorderCache = new Dictionary<int, ATVRecorder>();

        public override byte SerializeableID => (byte) TimelineSerializedTypes.ATV_RECORDER;

        public ATVRecorder() {
            AddListenerEvents();
        }

        public override void OnPlayStarted(float playHead)
        {
            base.OnPlayStarted(playHead);

            if (playbackEntity) {
                ModifyPlaybackObjectInteractable(true);
            }
        }

        public override void OnPaused(float playHead)
        {
            base.OnPaused(playHead);

            if (playbackEntity) {
                foreach (var body in playbackEntity.Bodies) {
                    if (body._rigidbody) {
                        body._rigidbody.isKinematic = true;
                    }
                }
            }
        }

        private void AddListenerEvents() {
            MarrowEntityRecorder.OnPlaybackSpawnedEvent += (en) => {
                IgnoreCollisions(en, true);
            };

            MarrowEntityRecorder.OnPlaybackObjectInteractableChangeEvent += (en, isInteractable) => {

                if (isInteractable) {
                    IgnoreCollisions(en, false, true);
                }
            };
        }

        private void IgnoreCollisions(MarrowEntity marrowEntity, bool ignore, bool force = false) {
            if (playbackEntity) {

                // Same entity
                if (playbackEntity.GetInstanceID() == marrowEntity.GetInstanceID()) {
                    return;
                }

                if (!force) {
                    if (marrowEntity.Bodies != null) {
                        // Not a kinematic body, so don't ignore it. Future proofing for velocity movers
                        if (marrowEntity.Bodies.Count > 0)
                        {
                            if (marrowEntity.Bodies[0]._rigidbody) {
                                if (!marrowEntity.Bodies[0]._rigidbody.isKinematic)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                

                foreach (var colliderOne in marrowEntity.GetComponentsInChildren<Collider>())
                {
                    foreach (var colliderTwo in playbackEntity.GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(colliderOne, colliderTwo, ignore);
                    }
                }
            }
        }

        public static bool TryGetRecorderFromCache(Atv atv, out ATVRecorder recorder)
        {
            if (recorderCache.ContainsKey(atv.GetInstanceID()))
            {
                recorder = recorderCache[atv.GetInstanceID()];
                return true;
            }

            recorder = null;
            return false;
        }

        public override void UpdateEntityCache()
        {
            base.UpdateEntityCache();

            recorderCache.Remove(previousAtvInstanceId);

            Atv atv = playbackEntity.GetComponentInChildren<Atv>();


            if (recorderCache.ContainsKey(atv.GetInstanceID()))
            {
                return;
            }

            recorderCache.Add(atv.GetInstanceID(), this);
            previousAtvInstanceId = atv.GetInstanceID();
        }

        public override void OnPlaybackSpawned(GameObject go)
        {
            base.OnPlaybackSpawned(go);

            // Might be scrubbing
            if (WorldPlayer.playing)
            {
                ModifyPlaybackObjectInteractable(true);
            }

            SetRigidbodyCapturersToBeVelocity();

            if (!recording) {
                IgnoreExistingMarrowRecordersInScene();
            }
        }

        private void IgnoreExistingMarrowRecordersInScene() {
            foreach (var playbackRecorder in WorldPlayer.Instance.playbackRecorders)
            {
                if (playbackRecorder is MarrowEntityRecorder)
                {
                    MarrowEntityRecorder marrowEntityRecorder = (MarrowEntityRecorder) playbackRecorder;

                    if (marrowEntityRecorder.playbackEntity)
                    {
                        IgnoreCollisions(marrowEntityRecorder.playbackEntity, true);
                    }
                }
            }
        }

        private void SetRigidbodyCapturersToBeVelocity() {
            foreach (var capturer in capturers) {
                capturer.moveWithVelocity = true;
            }
        }

        public override void OnRecordingCompleted()
        {
            base.OnRecordingCompleted();
            satIn = false;
        }
    }
}
