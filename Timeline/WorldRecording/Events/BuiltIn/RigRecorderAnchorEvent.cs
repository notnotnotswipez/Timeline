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
    public class RigRecorderAnchorEvent : RecordingEvent
    {
        public override byte EventID => (byte) TimelineSerializedEvents.RIG_RECORDER_ANCHOR;

        public bool isAnchor = false;
        public ushort recorderID = 0;

        public RigRecorderAnchorEvent(bool isAnchor, ushort recorderID) {
            this.isAnchor = isAnchor;
            this.recorderID = recorderID;
        }

        public RigRecorderAnchorEvent() {
        
        }

        public override int GetSize()
        {
            return sizeof(byte) + sizeof(ushort);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            isAnchor = stream.ReadBool();
            recorderID = stream.ReadUInt16();
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            RigmanagerRecorder rigmanagerRecorder = (RigmanagerRecorder) recorder;

            if (isAnchor)
            {
                MarrowEntityRecorder marrowEntityRecorder = (MarrowEntityRecorder) WorldPlayer.Instance.GetObjectRecorder(recorderID);

                rigmanagerRecorder.SetMarrowRecorderAnchor(marrowEntityRecorder);
            }
            else {
                rigmanagerRecorder.SetPositionalAnchor(null);
            }
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteBool(isAnchor);
            stream.WriteUInt16(recorderID);
        }
    }
}
