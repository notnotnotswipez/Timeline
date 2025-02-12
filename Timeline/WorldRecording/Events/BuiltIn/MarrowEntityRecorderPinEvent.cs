using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.WorldRecording.Recorders;
using UnityEngine;

namespace Timeline.WorldRecording.Events.BuiltIn
{
    public class MarrowEntityRecorderPinEvent : RecordingEvent
    {
        public override byte EventID => (byte) TimelineSerializedEvents.MARROW_ENTITY_RECORDER_PIN;

        public bool isAttach;
        public ushort recorderID;
        public byte secondaryData;

        public MarrowEntityRecorderPinEvent(ushort recorderID, HumanBodyBones bones) {
            this.recorderID = recorderID;
            this.secondaryData = (byte) bones;
            isAttach = true;
        }

        public MarrowEntityRecorderPinEvent(ushort recorderID, byte secondaryData)
        {
            this.recorderID = recorderID;
            this.secondaryData = secondaryData;
            isAttach = true;
        }

        public MarrowEntityRecorderPinEvent(ushort recorderID)
        {
            this.recorderID = recorderID;
            this.secondaryData = 0;
            isAttach = true;
        }

        public MarrowEntityRecorderPinEvent(bool attach) {
            isAttach = attach;
        }

        public MarrowEntityRecorderPinEvent() {
        
        }

        public override int GetSize()
        {
            return sizeof(ushort) + sizeof(byte) + sizeof(byte);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            isAttach = stream.ReadBool();
            recorderID = stream.ReadUInt16();
            secondaryData = stream.ReadByte();
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            MarrowEntityRecorder marrowEntityRecorder = (MarrowEntityRecorder)recorder;
            if (!isAttach)
            {
                marrowEntityRecorder.ClearPins();
            }
            else {
                ObjectRecorder targetRecorder = WorldPlayer.Instance.GetObjectRecorder(recorderID);

                if (targetRecorder is RigmanagerRecorder) {
                    marrowEntityRecorder.PinToAvailableActorBone(recorderID, (HumanBodyBones) secondaryData);
                }
                else if (targetRecorder is MarrowEntityRecorder) {
                    marrowEntityRecorder.PinToEntityRecorder(recorderID);
                }
            }
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteBool(isAttach);
            stream.WriteUInt16(recorderID);
            stream.WriteByte(secondaryData);
        }
    }
}
