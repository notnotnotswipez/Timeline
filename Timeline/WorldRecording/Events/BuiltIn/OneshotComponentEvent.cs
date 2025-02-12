using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow.PuppetMasta;
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
    public class OneshotComponentEvent : RecordingEvent
    {
        public override byte EventID => (byte) TimelineSerializedEvents.COMPONENT_ONESHOT_EVENT;

        ComponentOneshots selectedOneshot;
        byte secondaryData = 0;

        public OneshotComponentEvent(ComponentOneshots oneshots, byte secondaryData) {
            this.selectedOneshot = oneshots;
            this.secondaryData = secondaryData;
        }

        public OneshotComponentEvent() {
            
        }

        public override int GetSize()
        {
            return sizeof(byte) * 2;
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            selectedOneshot = (ComponentOneshots) stream.ReadByte();
            secondaryData = stream.ReadByte();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteByte((byte)selectedOneshot);
            stream.WriteByte(secondaryData);
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            MarrowEntityRecorder marrowEntityRecorder = (MarrowEntityRecorder) recorder;

            switch (selectedOneshot) {
                case ComponentOneshots.BEHAVIORBASENAV_KILL:
                    marrowEntityRecorder.FetchComponentManager<BehaviorBaseNavComponentManager>().GetComponentByIndex<BehaviourBaseNav>(secondaryData).PuppetMasterKill();
                    break;
                case ComponentOneshots.LASER_TOGGLE:
                    bool activeState = false;
                    byte realIndex = secondaryData;

                    if (realIndex >= 100) {
                        realIndex -= 100;
                        activeState = true;
                    }

                    marrowEntityRecorder.FetchComponentManager<SLZLaserPointerComponentManager>().ToggleLaser(realIndex, activeState);
                    break;
            }
        }

        
    }

    public enum ComponentOneshots : byte {
        BEHAVIORBASENAV_KILL = 0,
        LASER_TOGGLE = 1
    }
}
