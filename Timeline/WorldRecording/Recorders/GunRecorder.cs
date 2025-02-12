using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.StateCapturers;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Timeline.WorldRecording.Recorders
{
    // TODO: This can *probably* be moved to a ComponentManager<Gun> rather than its entirely own recorder.
    // It would just take some finangling to the ComponentManager handling, but it could work.
    public class GunRecorder : MarrowEntityRecorder
    {
        public Gun playbackGun;
        public Gun recordingGun;
        public int previousGunInstanceId = -1;

        private FloatCapturer pullbackPercCapture = new FloatCapturer();

        // Key: Gun InstanceID!
        private static Dictionary<int, GunRecorder> recorderCache = new Dictionary<int, GunRecorder>();

        public override byte SerializeableID => (byte) TimelineSerializedTypes.GUN_RECORDER;

        public static bool TryGetRecorderFromCache(Gun gun, out GunRecorder recorder) {
            if (recorderCache.ContainsKey(gun.GetInstanceID()))
            {
                recorder = recorderCache[gun.GetInstanceID()];
                return true;
            }

            recorder = null;
            return false;
        }

        public override void UpdateEntityCache()
        {
            base.UpdateEntityCache();

            recorderCache.Remove(previousGunInstanceId);

            Gun gun = playbackEntity.GetComponentInChildren<Gun>();

            if (recorderCache.ContainsKey(gun.GetInstanceID()))
            {
                return;
            }

            recorderCache.Add(gun.GetInstanceID(), this);
            previousGunInstanceId = gun.GetInstanceID();
        }

        public override void Capture(float sceneTime)
        {
            base.Capture(sceneTime);
        }

        public override void Playback(float sceneTime)
        {
            base.Playback(sceneTime);
            UpdateSlidePercentage(pullbackPercCapture.GetValue(sceneTime));

        }

        public void CaptureSlidePullPerc(float sceneTime, float perc) {
            pullbackPercCapture.Capture(sceneTime, perc);
        }

        public override void OnPlaybackSpawned(GameObject go) {
            base.OnPlaybackSpawned(go);
            playbackGun = go.GetComponentInChildren<Gun>();
        }

        public override void OnRecordingEntitySet(GameObject go) {
            base.OnRecordingEntitySet(go);
            recordingGun = go.GetComponentInChildren<Gun>();
            AddActionsToGun();
        }

        public void AddActionsToGun() {
            if (!recordingGun) {
                return;
            }
            recordingGun.onFireDelegate += new System.Action<Gun>((g) => {
                if (recording)
                {
                    AddEvent(WorldPlayer.playHead, new GunEvent(GunEventTypes.FIRE));
                }
            });

            if (!recordingGun.slideVirtualController) {
                return;
            }

            recordingGun.slideVirtualController.OnSlideUpdate += new System.Action<float>((f) => {
                if (recording) {
                    CaptureSlidePullPerc(WorldPlayer.playHead, f);
                }
            });

            recordingGun.slideVirtualController.OnSlidePulled += new System.Action(() => {
                if (recording)
                {
                    AddEvent(WorldPlayer.playHead, new GunEvent(GunEventTypes.SLIDE_PULL));
                }
            });

            recordingGun.slideVirtualController.OnSlideReleased += new System.Action(() => {
                if (recording)
                {
                    AddEvent(WorldPlayer.playHead, new GunEvent(GunEventTypes.SLIDE_RELEASE));
                }
            });

            recordingGun.slideVirtualController.OnSlideGrabbed += new System.Action(() => {
                if (recording)
                {
                    AddEvent(WorldPlayer.playHead, new GunEvent(GunEventTypes.SLIDE_GRAB));
                }
            });
        }

        public override void OnOverrideStart(float sceneTime)
        {
            base.OnOverrideStart(sceneTime);
            pullbackPercCapture.ClearAllDataAfterTime(sceneTime);
        }

        public void PlayGunSFX() {
            // Some guns do not use gunsfx to play their gunshots. Weird. Whatever.
            // Think about it later.
            playbackGun.gunSFX.GunShot();
        }

        public void TriggerGunSFX(GunEventTypes sfxEvent) {
            switch (sfxEvent) {
                case GunEventTypes.MAG_INSERT_SFX:
                    playbackGun.gunSFX.MagazineInsert();
                    break;
                case GunEventTypes.MAG_DROP_SFX:
                    playbackGun.gunSFX.MagazineDrop();
                    break;
            }
        }

        public void UpdateSlidePercentage(float perc)
        {
            if (!playbackGun) {
                return;
            }
            if (!playbackGun.slideVirtualController)
            {
                return;
            }
            playbackGun.slideVirtualController.OnSlideUpdate?.Invoke(perc);
        }

        public void SlidePull() {
            if (!playbackGun.slideVirtualController) {
                return;
            }
            playbackGun.slideVirtualController.OnSlidePulled.Invoke();
        }

        public void SlideGrab()
        {
            if (!playbackGun.slideVirtualController)
            {
                return;
            }
            playbackGun.slideVirtualController.OnSlideGrabbed.Invoke();
        }

        public void SlideGrabRelease()
        {
            if (!playbackGun.slideVirtualController)
            {
                return;
            }
            playbackGun.slideVirtualController.OnSlideReleased.Invoke();
        }

        public void SlideReturn()
        {
            if (!playbackGun.slideVirtualController)
            {
                return;
            }
            playbackGun.slideVirtualController.OnSlideReturned.Invoke();
        }

        public override int GetSize()
        {
            return base.GetSize() + pullbackPercCapture.GetSize();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            base.WriteToStream(stream);

            stream.WriteSerializableMember(pullbackPercCapture);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            base.ReadFromStream(stream);

            pullbackPercCapture = (FloatCapturer) stream.ReadSerializableMember<FloatCapturer>();
        }
    }
}
