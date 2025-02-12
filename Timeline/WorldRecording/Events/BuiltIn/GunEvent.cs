using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Logging;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.WorldRecording.Recorders;

namespace Timeline.WorldRecording.Events.BuiltIn
{
    public class GunEvent : RecordingEvent
    {
        GunEventTypes eventType;

        public GunEvent(GunEventTypes eventTypes) {
            eventType = eventTypes;
        }

        // No Data
        public GunEvent() {
        
        }

        public override byte EventID => (byte) TimelineSerializedEvents.GUN_EVENT;

        public override int GetSize()
        {
            return sizeof(byte);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            eventType = (GunEventTypes) stream.ReadByte();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteByte((byte)eventType);
        }

        public override void RunEvent(ObjectRecorder recorder)
        {
            GunRecorder gunRecorder = (GunRecorder) recorder;

            TimelineLogger.Debug("Ran gun event: " + eventType.ToString());

            switch (eventType)
            {
                case GunEventTypes.SLIDE_PULL:
                    gunRecorder.SlidePull();
                    break;
                case GunEventTypes.SLIDE_GRAB:
                    gunRecorder.SlideGrab();
                    break;
                case GunEventTypes.SLIDE_RETURN:
                    gunRecorder.SlideReturn();
                    break;
                case GunEventTypes.SLIDE_GRAB_RELEASE:
                    gunRecorder.SlideGrabRelease();
                    break;
                case GunEventTypes.FIRE:
                    gunRecorder.PlayGunSFX();
                    break;
                case GunEventTypes.MAG_INSERT_SFX:
                case GunEventTypes.MAG_DROP_SFX:
                    gunRecorder.TriggerGunSFX(eventType);
                    break;
            }
        }

        
    }

    public enum GunEventTypes : byte {
        SLIDE_PULL = 0,
        SLIDE_GRAB = 1,
        SLIDE_RELEASE = 2,
        SLIDE_GRAB_RELEASE = 3,
        SLIDE_LOCKED = 4,
        SLIDE_RETURN = 5,
        MAG_INSERT_SFX = 6,
        MAG_DROP_SFX = 7,
        FIRE = 8
    }
}
