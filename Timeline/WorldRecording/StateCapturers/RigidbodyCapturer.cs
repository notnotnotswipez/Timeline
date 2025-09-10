using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.SceneStreaming;
using MelonLoader;
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

    public class RigidbodyCapturer : GameObjectTransformCapturer
    {
        public bool isKinematic = false;
        public bool moveWithVelocity = false;

        public static float velocityStrengthMult = 9f;

        MarrowBody cachedBody;

        Vector3 lastPosition = Vector3.zero;
        public override byte SerializeableID => (byte) TimelineSerializedTypes.RIGIDBODY_CAPTURER;

        public RigidbodyCapturer(Rigidbody targetObject) : base(targetObject.gameObject)
        {
            isKinematic = targetObject.isKinematic;

            // It better have one!
            cachedBody = targetObject.GetComponent<MarrowBody>();
        }

        public RigidbodyCapturer(GameObject gameObject) : base(gameObject) {
            isKinematic = false;
        }

        // No data!
        public RigidbodyCapturer() {
        
        }

        public override void UpdateTargetObject(GameObject newTarget)
        {
            base.UpdateTargetObject(newTarget);

            if (targetTransform) {
                cachedBody = targetTransform.GetComponent<MarrowBody>();
            }
        }

        public override void ApplyToObject(float sceneTime, Transform toApply, TransformUpdateMode transformUpdateMode = TransformUpdateMode.BOTH)
        {
            if (!moveWithVelocity)
            {
                base.ApplyToObject(sceneTime, toApply, transformUpdateMode);
            }
            else {
                if (cachedBody && cachedBody._rigidbody)
                {
                    Rigidbody targetBody = cachedBody._rigidbody;

                    TransformFrame previousFrame = default;
                    TransformFrame nextFrame = default;

                    bool foundPrev = false;
                    bool foundNext = false;

                    foreach (var transformKeyframe in frames)
                    {
                        if (transformKeyframe.Key > sceneTime)
                        {
                            foundNext = true;
                            nextFrame = transformKeyframe.Value;
                            break;
                        }
                        foundPrev = true;
                        previousFrame = transformKeyframe.Value;
                    }

                    if (foundNext)
                    {
                        if (!foundPrev)
                        {
                            targetBody.position = nextFrame.position;
                            targetBody.rotation = nextFrame.rotation;

                            targetBody.velocity = Vector3.zero;
                            targetBody.angularVelocity = Vector3.zero;
                        }
                        else
                        {
                            float closeness = (sceneTime - previousFrame.time) / (nextFrame.time - previousFrame.time);

                            Vector3 targetPos = Vector3.Lerp(previousFrame.position, nextFrame.position, closeness);

                            Quaternion targetRot = Quaternion.Lerp(previousFrame.rotation, nextFrame.rotation, closeness);

                            VelocityMove(targetBody, targetPos, targetRot);
                        }
                    }
                }
            }
        }

        public void VelocityMove(Rigidbody body, Vector3 targetPos, Quaternion targetRot)
        {
            // Kinematic bodies should not move with velocity
            if (body.isKinematic) {
                targetTransform.position = targetPos;
                targetTransform.rotation = targetRot;
                return;
            }
            var delta = targetPos - body.position;

            if (delta.sqrMagnitude > 20) {
                body.position = targetPos;
                body.rotation = targetRot;
                body.velocity = Vector3.zero;
                return;
            }

            if (lastPosition == Vector3.zero) {
                lastPosition = targetPos;
            }

            body.velocity = delta * velocityStrengthMult;

            lastPosition = targetPos;

            Quaternion rotToTarget = targetRot * Quaternion.Inverse(body.rotation);
            rotToTarget.ToAngleAxis(out float angle, out Vector3 axis);

            Vector3 requiredAngularVelocity = axis * (angle * Mathf.Deg2Rad);
            body.angularVelocity = requiredAngularVelocity * velocityStrengthMult;
        }

        public override bool ShouldIgnoreCapture(Vector3 currentPos, Vector3 rotEul)
        {
            if (cachedBody && cachedBody._rigidbody) {
                return cachedBody._rigidbody.IsSleeping();
            }
            
            return base.ShouldIgnoreCapture(currentPos, rotEul);
        }

        public override int GetSize()
        {
            return sizeof(bool) + base.GetSize();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            base.WriteToStream(stream);

            stream.WriteBool(isKinematic);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            base.ReadFromStream(stream);

            isKinematic = stream.ReadBool();
        }
    }
}
