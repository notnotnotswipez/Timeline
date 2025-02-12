using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using UnityEngine;

namespace Timeline.WorldRecording.StateCapturers
{
    public class FloatCapturer : SerializableMember
    {
        SortedList<float, float> frames = new SortedList<float, float>();

        public FloatCapturer()
        {
        }

        public override byte SerializeableID => (byte) TimelineSerializedTypes.FLOAT_CAPTURER;

        public void Capture(float sceneTime, float value)
        {
            if (frames.ContainsKey(sceneTime)) {
                return;
            }
            frames.Add(sceneTime, value);
        }

        public void ClearAllDataAfterTime(float sceneTime)
        {
            List<float> keysToRemove = new List<float>();

            foreach (var frame in frames)
            {
                if (frame.Key > sceneTime)
                {
                    keysToRemove.Add(frame.Key);
                }
            }

            foreach (var toRemove in keysToRemove)
            {
                frames.Remove(toRemove);
            }
        }

        public override int GetSize()
        {
            // 2 floats per entry
            return sizeof(int) + ((sizeof(float) + sizeof(float)) * frames.Count);
        }

        public float GetValue(float sceneTime)
        {
            float previousFrame = 0;
            float nextFrame = 0;

            float previousFrameTime = 0;
            float nextFrameTime = 0;

            bool foundPrev = false;
            bool foundNext = false;

            foreach (var floatKeyFrame in frames)
            {
                if (floatKeyFrame.Key > sceneTime)
                {
                    foundNext = true;
                    nextFrame = floatKeyFrame.Value;
                    nextFrameTime = floatKeyFrame.Key;
                    break;
                }
                foundPrev = true;
                previousFrame = floatKeyFrame.Value;
                previousFrameTime = floatKeyFrame.Key;
            }

            if (foundNext)
            {
                if (!foundPrev)
                {
                    return nextFrame;
                }
                else
                {
                    float closeness = (sceneTime - previousFrameTime) / (nextFrameTime - previousFrameTime);
                    return Mathf.Lerp(previousFrame, nextFrame, closeness);
                }
            }

            return 0;
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            int size = stream.ReadInt32();

            for (int i = 0; i < size; i++)
            {
                float time = stream.ReadSingle();
                float value = stream.ReadSingle();

                frames.Add(time, value);
            }
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteInt32(frames.Count);

            foreach (var frame in frames) {
                stream.WriteSingle(frame.Key);
                stream.WriteSingle(frame.Value);
            }
        }
    }
}
