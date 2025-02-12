using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Timeline.Serialization.Binary
{
    public class BinaryStream
    {
        // Uses BitConverter, a bit slow I know, but we don't need to use pointer manipulation. We aren't making a multiplayer mod here.
        // Just parses a file on load/save requested. (Rare!)

        byte[] reservedData;
        public int streamIndex = 0;

        public int binaryVersion = SaveManager.version;

        public BinaryStream(int size) {
            reservedData = new byte[size];
        }

        public BinaryStream(byte[] reservedData) {
            this.reservedData = reservedData;
        }

        public void WriteSingle(float o) {
            byte[] converted = BitConverter.GetBytes(o);
            int offset = converted.Length;
            Array.Copy(converted, 0, reservedData, streamIndex, offset);
            streamIndex += offset;
        }

        public void WriteInt32(int o)
        {
            byte[] converted = BitConverter.GetBytes(o);
            int offset = converted.Length;
            Array.Copy(converted, 0, reservedData, streamIndex, offset);
            streamIndex += offset;
        }

        public void WriteUInt16(ushort o) {
            byte[] converted = BitConverter.GetBytes(o);
            int offset = converted.Length;
            Array.Copy(converted, 0, reservedData, streamIndex, offset);
            streamIndex += offset;
        }

        public void WriteByte(byte o)
        {
            reservedData[streamIndex++] = o;
        }

        public void WriteString(string s) {
            byte[] converted = Encoding.UTF8.GetBytes(s);
            int offset = converted.Length;
            
            // Limit of 255 characters but whatever I don't think we are ever going to need that much string data.
            WriteByte((byte)offset);

            Array.Copy(converted, 0, reservedData, streamIndex, offset);
            streamIndex += offset;
        }

        public void WriteByteArray(byte[] arr) {
            int offset = arr.Length;
            Array.Copy(arr, 0, reservedData, streamIndex, offset);
            streamIndex += offset;
        }

        public void WriteBool(bool o) {
            WriteByte(o ? (byte)1 : (byte)0);
        }

        public void WriteVector3(Vector3 vector3) {
            WriteSingle(vector3.x);
            WriteSingle(vector3.y);
            WriteSingle(vector3.z);
        }

        public void WriteQuaternion(Quaternion quaternion)
        {
            WriteSingle(quaternion.x);
            WriteSingle(quaternion.y);
            WriteSingle(quaternion.z);
            WriteSingle(quaternion.w);
        }

        public void WriteSerializableMember(SerializableMember sMember) {
            sMember.WriteToStream(this);
        }

        // Instance exists
        public void ReadSerializableMember(SerializableMember sMember)
        {
            sMember.ReadFromStream(this);
        }

        // Make a new instance
        public T ReadSerializableMember<T>() where T : SerializableMember
        {
            return (T) ReadSerializableMember(typeof(T));
        }

        public SerializableMember ReadSerializableMember(Type type)
        {
            SerializableMember serializableMember = (SerializableMember) Activator.CreateInstance(type);
            serializableMember.ReadFromStream(this);

            return serializableMember;
        }

        public float ReadSingle()
        {
           int size = sizeof(float);
           float o = BitConverter.ToSingle(GetDataSection(streamIndex, size));
           streamIndex += size;

           return o;
        }

        public int ReadInt32()
        {
            int size = sizeof(int);
            int o = BitConverter.ToInt32(GetDataSection(streamIndex, size));
            streamIndex += size;

            return o;
        }

        public ushort ReadUInt16() {
            int size = sizeof(ushort);
            ushort o = BitConverter.ToUInt16(GetDataSection(streamIndex, size));
            streamIndex += size;

            return o;
        }

        public byte ReadByte()
        {
            byte o = reservedData[streamIndex++];

            return o;
        }

        public bool ReadBool() {
            byte o = ReadByte();

            return (o == 1) ? true : false;
        }

        public byte[] ReadByteArray(int length) {
            byte[] result = GetDataSection(streamIndex, length);
            streamIndex += length;

            return result;
        }

        public Vector3 ReadVector3() {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }

        public Quaternion ReadQuaternion()
        {
            return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        public string ReadString() {
            // Length of the string is stored as a byte so we don't need a cutoff character or something like that.
            byte length = ReadByte();
            byte[] stringData = GetDataSection(streamIndex, length);
            string result = Encoding.UTF8.GetString(stringData);
            streamIndex += length;

            return result;
        }

        private byte[] GetDataSection(int offset, int length) {
            byte[] targetArr = new byte[length];
            Array.Copy(reservedData, offset, targetArr, 0, length);

            return targetArr;
        }

        public static int GetStringLength(string s) {

            // Length of the UTF8 string + the byte that tells us the size of it (This is so we don't have to remember to add the byte when
            // writing the length of a string)
            byte[] converted = Encoding.UTF8.GetBytes(s);
            int length = converted.Length + sizeof(byte);

            return length;
        }

        public byte[] GetReservedData() {
            return reservedData;
        }

        public void Compress()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(reservedData, 0, reservedData.Length);
                }
                reservedData = memoryStream.ToArray();
            }
        }

        public void Decompress() {
            using (var memoryStream = new MemoryStream(reservedData))
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(decompressedStream);
                        reservedData = decompressedStream.ToArray();
                    }
                }
            }
        }
    }
}
