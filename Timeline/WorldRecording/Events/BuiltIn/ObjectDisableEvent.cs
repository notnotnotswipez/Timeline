using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.WorldRecording.Recorders;

namespace Timeline.WorldRecording.Events.BuiltIn
{
    public class ObjectDisableEvent : RecordingEvent
    {
        public override byte EventID => (byte) TimelineSerializedEvents.GAMEOBJECT_DISABLE;

        public bool active = false;

        public ObjectDisableEvent(bool activated)
        {
            this.active = activated;
        }

        public ObjectDisableEvent() {
        
        }

        public override int GetSize()
        {
            return sizeof(byte);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            active = stream.ReadBool();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteBool(active);
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            if (recorder.targetObject) {
                recorder.targetObject.SetActive(active);
            }
        }
    }
}
