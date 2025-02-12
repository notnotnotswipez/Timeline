using Il2CppSLZ.Marrow;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.WorldRecording.Extras.Impl;
using Timeline.WorldRecording.Recorders;

namespace Timeline.WorldRecording.Events.BuiltIn
{
    public class ObjectDestructEvent : RecordingEvent
    {
        public override byte EventID => (byte) TimelineSerializedEvents.OBJECT_DESTRUCT;

        int index = 0;

        public ObjectDestructEvent(int index) {
            this.index = index;
        }

        public ObjectDestructEvent() {
        
        }

        public override int GetSize()
        {
            return sizeof(byte);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            index = stream.ReadByte();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteByte((byte)index);
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            MarrowEntityRecorder marrowEntityRecorder = (MarrowEntityRecorder) recorder;

            marrowEntityRecorder.FetchComponentManager<ObjectDestructibleComponentManager>().GetComponentByIndex<ObjectDestructible>(index)._impactSfx.DestructionEvent();
        }
    }
}
