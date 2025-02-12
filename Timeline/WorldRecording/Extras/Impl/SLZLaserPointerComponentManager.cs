using Il2CppSLZ.Bonelab;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Logging;
using Timeline.WorldRecording.Events.BuiltIn;
using UnityEngine;

namespace Timeline.WorldRecording.Extras.Impl
{
    public class SLZLaserPointerComponentManager : ComponentManager
    {
        public override Type ComponentType => typeof(SLZ_LaserPointer);

        // This is so unnecessary but it allows for multiple SLZLaserPointers on the same recorder object to be handled properly.
        List<SLZ_LaserPointer> m_Lasers = new List<SLZ_LaserPointer>();
        Dictionary<int, bool> previousStates = new Dictionary<int, bool>();

        public override void OnReceiveComponentsFromPlayback(Component[] components)
        {
            PopulateLaserLists(components);
        }

        public override void OnReceiveComponentsFromRecorder(Component[] components)
        {
            PopulateLaserLists(components);
        }

        private void PopulateLaserLists(Component[] components) {
            previousStates.Clear();
            m_Lasers.Clear();
            foreach (var comp in components)
            {
                m_Lasers.Add(comp.TryCast<SLZ_LaserPointer>());
                previousStates.Add(comp.GetInstanceID(), false);
            }
        }

        public override void OnRecorderCompleted(Component[] components)
        {
            
        }

        public override void OnUpdate(bool playback)
        {
            // Recording
            if (!playback)
            {
                int index = 0;
                foreach (var laser in m_Lasers)
                {

                    bool state = previousStates[laser.GetInstanceID()];
                    bool active = laser.gameObject.active;
                    if (state != laser.gameObject.active)
                    {

                        int addition = 0;
                        if (laser.gameObject.active) {
                            addition = 100;
                        }

                        // Changed!
                        objectRecorder.AddEvent(WorldPlayer.playHead, new OneshotComponentEvent(ComponentOneshots.LASER_TOGGLE, (byte)(index + addition)));
                    }

                    previousStates[laser.GetInstanceID()] = active;

                    index++;
                }
            }
        }

        public void ToggleLaser(int laserIndex, bool active) {
            if (m_Lasers.Count > laserIndex) {
                GameObject laserObject = m_Lasers[laserIndex].gameObject;
                laserObject.SetActive(active);
            }
        }
    }
}
