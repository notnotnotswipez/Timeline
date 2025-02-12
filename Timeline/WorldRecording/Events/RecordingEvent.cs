using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization;
using Timeline.WorldRecording.Recorders;

namespace Timeline.WorldRecording.Events
{
    public abstract class RecordingEvent : SerializableMember
    {
        public bool ranEvent = false;

        public override byte SerializeableID => (byte) TimelineSerializedTypes.RECORDING_EVENT;

        public abstract byte EventID { get; }

        public abstract void RunEvent(ObjectRecorder recorder);
    }
}
