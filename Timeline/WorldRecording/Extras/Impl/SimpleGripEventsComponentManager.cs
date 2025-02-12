using Il2CppSLZ.Bonelab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Events.BuiltIn;
using UnityEngine;
using UnityEngine.Events;

namespace Timeline.WorldRecording.Extras.Impl
{
    public class SimpleGripEventsComponentManager : ComponentManager
    {
        List<int> alreadyListenerifiedEvents = new List<int>();

        public override Type ComponentType => typeof(SimpleGripEvents);

        public override void OnReceiveComponentsFromPlayback(Component[] components)
        {
            // This component manager doesn't need to do anything to the playback components.
        }

        private void AddListenerEvents(SimpleGripEvents events)
        {
            if (alreadyListenerifiedEvents.Contains(events.GetInstanceID()))
            {
                return;
            }

            alreadyListenerifiedEvents.Add(events.GetInstanceID());

            events.OnAttach.AddListener(new Action(() =>
            {
                if (objectRecorder.recording)
                {
                    objectRecorder.AddEvent(WorldPlayer.playHead, new SimpleGripEventsTriggerEvent(SimpleGripEventTypes.ATTACH, GetIndexFromComponent(events)));
                }
            }));

            events.OnDetach.AddListener(new Action(() =>
            {
                if (objectRecorder.recording)
                {
                    objectRecorder.AddEvent(WorldPlayer.playHead, new SimpleGripEventsTriggerEvent(SimpleGripEventTypes.DETACH, GetIndexFromComponent(events)));
                }
            }));

            events.OnIndexDown.AddListener(new Action(() =>
            {
                if (objectRecorder.recording)
                {
                    objectRecorder.AddEvent(WorldPlayer.playHead, new SimpleGripEventsTriggerEvent(SimpleGripEventTypes.INDEX_DOWN, GetIndexFromComponent(events)));
                }
            }));

            events.OnMenuTapDown.AddListener(new Action(() =>
            {
                if (objectRecorder.recording)
                {
                    objectRecorder.AddEvent(WorldPlayer.playHead, new SimpleGripEventsTriggerEvent(SimpleGripEventTypes.MENU_DOWN, GetIndexFromComponent(events)));
                }
            }));
        }

        public override void OnReceiveComponentsFromRecorder(Component[] components)
        {
            // Add listeners
            foreach (var comp in components)
            {
                AddListenerEvents(comp.TryCast<SimpleGripEvents>());
            }
        }

        public override void OnRecorderCompleted(Component[] components)
        {
            // We don't need to do anything here
        }
    }
}
