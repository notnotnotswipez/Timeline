using Il2CppSLZ.Bonelab;
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
    public class SimpleGripEventsTriggerEvent : RecordingEvent
    {
        public override byte EventID => (byte) TimelineSerializedEvents.SIMPLE_GRIP_EVENT;

        SimpleGripEventTypes selectedType;
        int index = 0;

        public SimpleGripEventsTriggerEvent(SimpleGripEventTypes eventTypes, int index) {
            this.selectedType = eventTypes;
            this.index = index;
        }

        public SimpleGripEventsTriggerEvent() {
            
        }

        public override int GetSize()
        {
            return sizeof(byte) + sizeof(byte);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            selectedType = (SimpleGripEventTypes) stream.ReadByte();
            index = stream.ReadByte();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteByte((byte)selectedType);

            // We are VERY LIKELY not going to have more than 255 simple grip events!
            stream.WriteByte((byte)index);
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            if (recorder is MarrowEntityRecorder) {
                MarrowEntityRecorder marrowEntityRecorder = (MarrowEntityRecorder) recorder;
                switch (selectedType)
                {
                    case SimpleGripEventTypes.INDEX_DOWN:
                        marrowEntityRecorder.FetchComponentManager<SimpleGripEventsComponentManager>().GetComponentByIndex<SimpleGripEvents>(index).OnIndexDown.Invoke();
                        break;
                    case SimpleGripEventTypes.ATTACH:
                        marrowEntityRecorder.FetchComponentManager<SimpleGripEventsComponentManager>().GetComponentByIndex<SimpleGripEvents>(index).OnAttach.Invoke();
                        break;
                    case SimpleGripEventTypes.DETACH:
                        marrowEntityRecorder.FetchComponentManager<SimpleGripEventsComponentManager>().GetComponentByIndex<SimpleGripEvents>(index).OnDetach.Invoke();
                        break;
                    case SimpleGripEventTypes.MENU_DOWN:
                        marrowEntityRecorder.FetchComponentManager<SimpleGripEventsComponentManager>().GetComponentByIndex<SimpleGripEvents>(index).OnMenuTapDown.Invoke();
                        break;
                }
            }
            else if (recorder is RigmanagerRecorder) {

                RigmanagerRecorder rigmanagerRecorder = (RigmanagerRecorder) recorder;
                // TODO: Track this
            }
        }

        
    }

    public enum SimpleGripEventTypes : byte {
        INDEX_DOWN = 0,
        ATTACH = 1,
        DETACH = 2,
        MENU_DOWN = 3
    }
}
