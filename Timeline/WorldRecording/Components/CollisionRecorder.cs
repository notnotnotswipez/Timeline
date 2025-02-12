using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Recorders;
using Timeline.WorldRecording.Utils;
using UnityEngine;

namespace Timeline.WorldRecording.Components
{
    [RegisterTypeInIl2Cpp]
    public class CollisionRecorder : MonoBehaviour
    {

        public CollisionRecorder(IntPtr intPtr) : base(intPtr) {
        
        }

        private List<int> rigidbodiesInColliderAlready = new List<int>();

        public void OnCollisionEnter(Collision collision)
        {
            if (WorldPlayer.recording) {
                if (collision.rigidbody)
                {
                    if (rigidbodiesInColliderAlready.Contains(collision.rigidbody.GetInstanceID()))
                    {
                        return;
                    }

                    rigidbodiesInColliderAlready.Add(collision.rigidbody.GetInstanceID());

                    // Makes one if it doesn't exist
                    RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(collision.rigidbody.gameObject, true);

                    // Thats all we do here, we don't want to take control of objects just by grazing them with other objects, we want to only do that if its a definite interaction like forcegrab or grip.
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (WorldPlayer.recording)
            {
                if (collision.rigidbody)
                {
                    if (rigidbodiesInColliderAlready.Contains(collision.rigidbody.GetInstanceID()))
                    {
                        rigidbodiesInColliderAlready.Remove(collision.rigidbody.GetInstanceID());
                    }
                }
            }
        }
    }
}
