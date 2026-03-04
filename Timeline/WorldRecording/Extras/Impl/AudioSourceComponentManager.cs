using Il2CppSLZ.Bonelab;
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

namespace Timeline.WorldRecording.Extras.Impl
{
    public class AudioSourceComponentManager : ComponentManager
    {
        public override Type ComponentType => typeof(AudioSource);

        public static Dictionary<int, ObjectRecorder> cachedAudioSources = new Dictionary<int, ObjectRecorder>();

        public override void OnReceiveComponentsFromPlayback(Component[] components)
        {
            
        }

        public override void OnReceiveComponentsFromRecorder(Component[] components) {
            foreach (var comp in components) {
                int instanceId = comp.GetInstanceID();
                if (!cachedAudioSources.ContainsKey(instanceId)) {
                    cachedAudioSources.Add(instanceId, objectRecorder);
                }
            }
        }

        public override void OnRecorderCompleted(Component[] components)
        {
            
        }

        public void PlaySound(int index)
        {
            GetComponentByIndex<AudioSource>(index).Play();
        }
    }
}
