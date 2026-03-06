using BoneLib;
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
    public class AudioSourceManualPlayEvent : RecordingEvent
    {
        string clipName;
        byte index;

        public AudioSourceManualPlayEvent(byte index, string clipName) {
            this.clipName = clipName;
            this.index = index;
        }

        // No data.
        public AudioSourceManualPlayEvent() {
            
        }

        public override byte EventID => (byte) TimelineSerializedEvents.AUDIO_SOURCE_MANUAL_PLAY;

        public override int GetSize()
        {
            return BinaryStream.GetStringLength(clipName) + sizeof(byte);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            string cName = stream.ReadString();
            clipName = cName;

            index = stream.ReadByte();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteString(clipName);
            stream.WriteByte(index);
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            MarrowEntityRecorder entityRecorder = (MarrowEntityRecorder) recorder;

            entityRecorder.FetchComponentManager<AudioSourceComponentManager>().PlaySound(index, clipName);
        }
    }
}
