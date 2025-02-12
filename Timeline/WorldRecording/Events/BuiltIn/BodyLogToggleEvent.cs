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
    public class BodyLogToggleEvent : RecordingEvent
    {
        public override byte EventID => (byte) TimelineSerializedEvents.BODYLOG_TOGGLE_EVENT;

        public bool active = false;

        public BodyLogToggleEvent(bool enabled) {
            active = enabled;
        }

        public BodyLogToggleEvent() {
            
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

            RigmanagerRecorder rigmanagerRecorder = (RigmanagerRecorder) recorder;

            rigmanagerRecorder.rigmanagerExtrasTracker.ToggleBodyLogBall(active);
        }

        
    }
}
