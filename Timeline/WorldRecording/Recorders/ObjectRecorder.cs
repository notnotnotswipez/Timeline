using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.Serialization.Registry;
using Timeline.WorldRecording.Events;
using UnityEngine;

namespace Timeline.WorldRecording.Recorders
{
    public abstract class ObjectRecorder : SerializableMember
    {
        public bool recording = false;
        public bool initialized = false;

        SortedList<float, RecordingEvent> recordingEvents = new SortedList<float, RecordingEvent>();
        Dictionary<string, string> metaData = new Dictionary<string, string>();

        public List<ushort> associatedRecorders = new List<ushort>();

        public float initTime = 0;
        public float takeLength = 0;

        public ushort recorderID = 0;

        public GameObject targetObject;

        public bool hidden = false;

        

        public abstract void OnInitializedRecording(UnityEngine.Object rootObject);
        public abstract void OnInitializedPlayback();
        public abstract void Capture(float sceneTime);
        public abstract void Playback(float sceneTime);
        public abstract void OnPlaybackCompleted();
        public abstract void OnRecordingCompleted();
        public abstract string GetName();

        public override int GetSize() {

            int totalSize = 0;

            // InitTime
            totalSize += sizeof(float);

            // Take Length
            totalSize += sizeof(float);

            // Recorder ID
            totalSize += sizeof(ushort);

            // Associated recorders length
            totalSize += sizeof(int);

            // Associated recorder entries
            totalSize += sizeof(ushort) * associatedRecorders.Count;

            // Recording events length
            totalSize += sizeof(int);

            foreach (var eventPair in recordingEvents)
            {
                // Time key
                totalSize += sizeof(float);

                // Event ID and associated Data size.
                totalSize += sizeof(byte) + eventPair.Value.GetSize();
            }

            // Metadata entry size
            totalSize += sizeof(int);

            foreach (var keyPair in metaData) {

                // Key and Val
                totalSize += BinaryStream.GetStringLength(keyPair.Key);
                totalSize += BinaryStream.GetStringLength(keyPair.Value);
            }

            return totalSize;
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteSingle(initTime);
            stream.WriteSingle(takeLength);
            stream.WriteUInt16(recorderID);
            stream.WriteInt32(associatedRecorders.Count);

            for (int i = 0; i < associatedRecorders.Count; i++)
            {
                stream.WriteUInt16(associatedRecorders[i]);
            }

            stream.WriteInt32(recordingEvents.Count);

            foreach (var keyPair in recordingEvents) {
                stream.WriteSingle(keyPair.Key);

                RecordingEvent recordingEvent = keyPair.Value;

                stream.WriteByte(recordingEvent.EventID);
                stream.WriteSerializableMember(recordingEvent);
            }

            stream.WriteInt32(metaData.Count);

            foreach (var keyPair in metaData) {
                stream.WriteString(keyPair.Key);
                stream.WriteString(keyPair.Value);
            }
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            initTime = stream.ReadSingle();
            takeLength = stream.ReadSingle();
            recorderID = stream.ReadUInt16();

            int associatedRecordersLength = stream.ReadInt32();

            for (int i = 0; i < associatedRecordersLength; i++) {
                associatedRecorders.Add(stream.ReadUInt16());
            }

            int recordingEventsLength = stream.ReadInt32();

            for (int i = 0; i < recordingEventsLength; i++) {
                float time = stream.ReadSingle();

                byte eventId = stream.ReadByte();

                // PULL FROM REGISTRY!
                SerializableRegistry.AttemptGetEventFromType(eventId, out var determinedEventType);
                RecordingEvent recordingEvent = (RecordingEvent) stream.ReadSerializableMember(determinedEventType);

                recordingEvents.Add(time, recordingEvent);
            }

            int metaDataPairs = stream.ReadInt32();

            for (int i = 0; i < metaDataPairs; i++) {
                string key = stream.ReadString();
                string value = stream.ReadString();

                metaData.Add(key, value);
            }
        }

        public bool TryGetMetadata(string key, out string data) {
            data = "NONE";

            if (metaData.ContainsKey(key)) {
                data = metaData[key];
                return true;
            }

            return false;
        }

        public void SetMetadata(string key, string val) {
            if (metaData.ContainsKey(key))
            {
                metaData[key] = val;
            }
            else {
                metaData.Add(key, val);
            }
        }

        public void ClearAllEventsAfterTime(float sceneTime) {
            List<float> keysToRemove = new List<float>();

            foreach (var frame in recordingEvents)
            {
                if (frame.Key > sceneTime)
                {
                    keysToRemove.Add(frame.Key);
                }
            }

            foreach (var toRemove in keysToRemove)
            {
                recordingEvents.Remove(toRemove);
            }
        }

        public void AddEvent(float sceneTime, RecordingEvent recordingEvent) {

            while (recordingEvents.ContainsKey(sceneTime)) {
                sceneTime += 0.01f;
            }

            recordingEvents.Add(sceneTime, recordingEvent);
        }

        public void CheckAndRunEvents(float sceneTime) {

            RecordingEvent lastEvent = null;
            RecordingEvent lastRanEvent = null;

            foreach (var recordingEvent in recordingEvents) {
                if (recordingEvent.Key < sceneTime)
                {
                    if (!recordingEvent.Value.ranEvent)
                    {
                        lastEvent = recordingEvent.Value;
                        break;
                    }
                    else {
                        lastRanEvent = recordingEvent.Value;
                    }
                }
                else {
                    if (recordingEvent.Value.ranEvent) {
                        recordingEvent.Value.ranEvent = false;

                        if (lastRanEvent != null) {
                            
                            lastRanEvent.RunEvent(this);
                            lastRanEvent = null;
                        }
                    }
                }
            }

            if (lastEvent != null) {
                try
                {
                    lastEvent.RunEvent(this);
                }
                catch (Exception ex) {
                    
                }
                lastEvent.ranEvent = true;
            }
        }

        public void MarkAllEventsAsNotRun() {
            foreach (var recordingEvent in recordingEvents)
            {
                recordingEvent.Value.ranEvent = false;
            }
        }

        public virtual void OnSave() {
        
        }

        public virtual void OnPlayStarted(float playHead) {
            
        }

        public virtual void OnPaused(float playHead)
        {

        }

        public virtual void OnHide(bool hidden) {
        
        }
    }
}
