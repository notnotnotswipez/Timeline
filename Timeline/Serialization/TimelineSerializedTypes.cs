using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timeline.Serialization
{
    public enum TimelineSerializedTypes : byte {
        GAMEOBJECT_TRANSFORM_CAPTURE = 0,
        FLOAT_CAPTURER = 1,
        RIGIDBODY_CAPTURER = 2,
        MARROW_ENTITY_RECORDER = 3,
        RIGMANAGER_RECORDER = 4,
        RECORDING_EVENT = 5,
        GUN_RECORDER = 6,
        WORLD_PLAYER = 7,
        TIMELINE_HOLDER = 8,
        KEYFRAME = 9,
        SIMPLE_SPAWN_RECORDER = 10,
        RIGMANAGER_EXTRAS = 11,
        ATV_RECORDER = 12
    }

    public enum TimelineSerializedEvents : byte
    {
        AVATAR_SWAP_EVENT = 0,
        GUN_EVENT = 1,
        SIMPLE_GRIP_EVENT = 2,
        GAMEOBJECT_DISABLE = 3,
        OBJECT_DESTRUCT = 4,
        COMPONENT_ONESHOT_EVENT = 5,
        BODYLOG_TOGGLE_EVENT = 6,
        RIG_RECORDER_ANCHOR = 7,
        MARROW_ENTITY_RECORDER_PIN = 8,
        MARROW_ENTITY_CROSS_PIN = 9
    }
}
