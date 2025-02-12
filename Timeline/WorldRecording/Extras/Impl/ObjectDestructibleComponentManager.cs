using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Events.BuiltIn;
using UnityEngine;

namespace Timeline.WorldRecording.Extras.Impl
{
    public class ObjectDestructibleComponentManager : ComponentManager
    {
        List<int> alreadyListenerifiedDestructibles = new List<int>();

        public override Type ComponentType => typeof(ObjectDestructible);

        private List<float> originalHealth = new List<float>();

        public override void OnReceiveComponentsFromPlayback(Component[] components)
        {
            originalHealth.Clear();
            // Playback objects should not be breakable
            foreach (ObjectDestructible component in components)
            {
                originalHealth.Add(component._health);
                component._health = 999999;
            }
        }

        public override void OnReceiveComponentsFromRecorder(Component[] components)
        {
            int index = 0;
            foreach (var comp in components)
            {
                ObjectDestructible component = (ObjectDestructible)comp;
                AddListenerEvents(component);

                // Reset their healths if they were previously indesctructible (Playback object ownership got taken)
                if (originalHealth.Count > 0) {
                    component._health = originalHealth[index];
                    index++;
                }
            }
        }

        private void AddListenerEvents(ObjectDestructible dest)
        {
            if (alreadyListenerifiedDestructibles.Contains(dest.GetInstanceID()))
            {
                return;
            }

            alreadyListenerifiedDestructibles.Add(dest.GetInstanceID());

            dest.OnDestruct.AddListener(new Action(() =>
            {
                if (objectRecorder.recording)
                {

                    objectRecorder.AddEvent(WorldPlayer.playHead, new ObjectDestructEvent(GetIndexFromComponent(dest)));
                }
            }));
        }

        public override void OnRecorderCompleted(Component[] components)
        {
            // We don't need to do anything
        }
    }
}
