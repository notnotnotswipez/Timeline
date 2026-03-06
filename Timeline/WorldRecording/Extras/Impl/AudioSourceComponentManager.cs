using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow.Utilities;
using Il2CppSLZ.SFX;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.Recorders;
using UnityEngine;
using UnityEngine.Playables;

namespace Timeline.WorldRecording.Extras.Impl
{
    public class AudioSourceComponentManager : ComponentManager
    {
        public override Type ComponentType => typeof(AudioSource);

        public static Dictionary<int, ObjectRecorder> cachedAudioSources = new Dictionary<int, ObjectRecorder>();

        Dictionary<int, Transform> originalParents = new Dictionary<int, Transform>();
        Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public override void OnReceiveComponentsFromPlayback(Component[] components)
        {
            if (objectRecorder is GunRecorder) {
                GunRecorder gunRecorder = (GunRecorder) objectRecorder;
                foreach (var comp in components)
                {
                    originalParents.Add(comp.GetInstanceID(), comp.transform.parent);

                    comp.transform.parent = gunRecorder.playbackObject.transform;
                    comp.gameObject.SetActive(true);

                    AudioSource source = comp.TryCast<AudioSource>();
                    AudioClip clip = source.clip;

                    if (clip) {
                        string clipName = clip.name;
                        if (!clips.ContainsKey(clipName)) {
                            clips.Add(clip.name, clip);
                        }
                    }
                }
            }
        }

        public override void OnReceiveComponentsFromRecorder(Component[] components) {
            foreach (var comp in components) {
                int instanceId = comp.GetInstanceID();
                if (cachedAudioSources.ContainsKey(instanceId)) {
                    cachedAudioSources.Remove(instanceId);
                }

                cachedAudioSources.Add(instanceId, objectRecorder);


                if (originalParents.ContainsKey(instanceId)) {
                    comp.transform.parent = originalParents[instanceId];
                    originalParents.Remove(instanceId);
                }
            }
        }

        public override void OnRecorderCompleted(Component[] components)
        {
            clips.Clear();

            foreach (int instanceId in originalParents.Keys) {
                cachedAudioSources.Remove(instanceId);
            }

            originalParents.Clear();
        }

        public void PlaySound(int index, string clipName)
        {
            AudioSource source = GetComponentByIndex<AudioSource>(index);
            source.gameObject.SetActive(true);

            if (clips.ContainsKey(clipName))
            {
                source.clip = clips[clipName];
            }

            source.Play();
        }
    }
}
