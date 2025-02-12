using Timeline.CameraRelated;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using UnityEngine;

namespace Timeline
{
    public abstract class Keyframe : SerializableMember
    {
        public float time;
        public CameraStateCapture cameraStateCapture;
        public KeyframeTypes type;
        public Texture2D texture;
        public bool selected = false;

        public override byte SerializeableID => (byte) TimelineSerializedTypes.KEYFRAME;

        public abstract void PreformAction(CameraController desiredObject, Keyframe previousKeyframe, float closeness);

        public override int GetSize()
        {
            return cameraStateCapture.GetSize();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            cameraStateCapture.WriteSelfToBinaryStream(stream);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            cameraStateCapture.ReadSelfFromBinaryStream(stream);
        }
    }

    public enum KeyframeTypes : byte
    {
        LINEAR = 0,
        INSTANT = 1
    }
}