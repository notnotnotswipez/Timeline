using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.PuppetMasta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Events.BuiltIn;
using UnityEngine;

namespace Timeline.WorldRecording.Extras.Impl
{
    public class BehaviorBaseNavComponentManager : ComponentManager
    {
        List<int> alreadyListenerifiedBehaviors = new List<int>();

        // British "people".
        public override Type ComponentType => typeof(BehaviourBaseNav);

        public override void OnReceiveComponentsFromPlayback(Component[] components)
        {
            // Do nothing
        }

        public override void OnReceiveComponentsFromRecorder(Component[] components)
        {
            foreach (var comp in components)
            {
                AddListenerEvents(comp.TryCast<BehaviourBaseNav>());
            }
        }

        private void AddListenerEvents(BehaviourBaseNav nav)
        {
            if (alreadyListenerifiedBehaviors.Contains(nav.GetInstanceID()))
            {
                return;
            }

            alreadyListenerifiedBehaviors.Add(nav.GetInstanceID());

            nav.OnDeath.AddListener(new Action(() =>
            {
                if (objectRecorder.recording)
                {
                    objectRecorder.AddEvent(WorldPlayer.playHead, new OneshotComponentEvent(ComponentOneshots.BEHAVIORBASENAV_KILL, (byte) GetIndexFromComponent(nav)));
                }
            }));
        }

        public override void OnRecorderCompleted(Component[] components)
        {
            // Do nothing.
        }
    }
}
