using BoneLib;
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
    public class AvatarSwapEvent : RecordingEvent
    {
        string barcode;

        public AvatarSwapEvent(string barcode) {
            this.barcode = barcode;
        }

        // No data.
        public AvatarSwapEvent() {
            
        }

        public override byte EventID => (byte) TimelineSerializedEvents.AVATAR_SWAP_EVENT;

        public override int GetSize()
        {
            return BinaryStream.GetStringLength(barcode);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            string bCode = stream.ReadString();
            barcode = bCode;
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteString(barcode);
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            RigmanagerRecorder rigmanagerRecorder = (RigmanagerRecorder) recorder;

            // Unparent them so they don't get destroyed
            foreach (var entityRecorderPin in rigmanagerRecorder.marrowEntityPinAssociators.ToList()) {
                entityRecorderPin.Item1.SetAllCaptureAnchors(null, false);
            }

            rigmanagerRecorder.DestroyPlaybackAvatar();
            rigmanagerRecorder.CreateAndAssignAvatar(barcode);
        }

        
    }
}
