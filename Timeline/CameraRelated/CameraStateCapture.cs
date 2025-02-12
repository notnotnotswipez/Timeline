using Timeline.Serialization.Binary;

namespace Timeline.CameraRelated
{
    public struct CameraStateCapture
    {
        public float positionX;
        public float positionY;
        public float positionZ;
        public float rotationX;
        public float rotationY;
        public float rotationZ;
        public float fov;

        public int GetSize() {
            return (sizeof(float) * 7);
        }

        public void WriteSelfToBinaryStream(BinaryStream stream) {
            stream.WriteSingle(positionX);
            stream.WriteSingle(positionY);
            stream.WriteSingle(positionZ);
            stream.WriteSingle(rotationX);
            stream.WriteSingle(rotationY);
            stream.WriteSingle(rotationZ);
            stream.WriteSingle(fov);
        }

        public void ReadSelfFromBinaryStream(BinaryStream stream) {
        
            positionX = stream.ReadSingle();
            positionY = stream.ReadSingle();
            positionZ = stream.ReadSingle();
            rotationX = stream.ReadSingle();
            rotationY = stream.ReadSingle();
            rotationZ = stream.ReadSingle();
            fov = stream.ReadSingle();
        }
    }
}