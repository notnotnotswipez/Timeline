using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.BuiltInKeyframes;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.Recorders;

namespace Timeline.Serialization.Registry
{
    public class SerializableRegistry
    {

        private static Dictionary<byte, Type> eventTypeRegistry = new Dictionary<byte, Type>();
        private static Dictionary<byte, Type> recorderTypeRegistry = new Dictionary<byte, Type>();
        private static Dictionary<byte, Type> keyframeTypeRegistry = new Dictionary<byte, Type>();

        public static void RegisterAll() {
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.AVATAR_SWAP_EVENT, typeof(AvatarSwapEvent));
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.GUN_EVENT, typeof(GunEvent));
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.SIMPLE_GRIP_EVENT, typeof(SimpleGripEventsTriggerEvent));
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.GAMEOBJECT_DISABLE, typeof(ObjectDisableEvent));
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.OBJECT_DESTRUCT, typeof(ObjectDestructEvent));
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.COMPONENT_ONESHOT_EVENT, typeof(OneshotComponentEvent));
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.BODYLOG_TOGGLE_EVENT, typeof(BodyLogToggleEvent));
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.RIG_RECORDER_ANCHOR, typeof(RigRecorderAnchorEvent));
            eventTypeRegistry.Add((byte) TimelineSerializedEvents.MARROW_ENTITY_RECORDER_PIN, typeof(MarrowEntityRecorderPinEvent));

            recorderTypeRegistry.Add((byte) TimelineSerializedTypes.RIGMANAGER_RECORDER, typeof(RigmanagerRecorder));
            recorderTypeRegistry.Add((byte) TimelineSerializedTypes.MARROW_ENTITY_RECORDER, typeof(MarrowEntityRecorder));
            recorderTypeRegistry.Add((byte) TimelineSerializedTypes.GUN_RECORDER, typeof(GunRecorder));
            recorderTypeRegistry.Add((byte) TimelineSerializedTypes.SIMPLE_SPAWN_RECORDER, typeof(SimpleSpawnRecorder));
            recorderTypeRegistry.Add((byte) TimelineSerializedTypes.ATV_RECORDER, typeof(ATVRecorder));

            keyframeTypeRegistry.Add((byte) KeyframeTypes.LINEAR, typeof(LinearKeyframe));
            keyframeTypeRegistry.Add((byte) KeyframeTypes.INSTANT, typeof(InstantKeyframe));
        }

        public static bool AttemptGetEventFromType(byte eventIndex, out Type type) {
            type = null;
            if (eventTypeRegistry.ContainsKey(eventIndex)) {
                type = eventTypeRegistry[eventIndex];
                return true;
            }


            return false;
        }

        public static bool AttemptGetRecorderFromType(byte recorderIndex, out Type type)
        {
            type = null;
            if (recorderTypeRegistry.ContainsKey(recorderIndex))
            {
                type = recorderTypeRegistry[recorderIndex];
                return true;
            }


            return false;
        }

        public static bool AttemptGetKeyframeFromType(byte keyframeIndex, out Type type)
        {
            type = null;
            if (keyframeTypeRegistry.ContainsKey(keyframeIndex))
            {
                type = keyframeTypeRegistry[keyframeIndex];
                return true;
            }


            return false;
        }
    }
}
