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
    public class GameObjectTransformCapturer : SerializableMember
    {
        internal SortedList<float, TransformFrame> frames = new SortedList<float, TransformFrame>();
        public Transform targetTransform;
        public Transform lastAnchor;
        public Transform anchor;

        Vector3 lastLocalPosition;
        Quaternion lastLocalRotation;

        private const float distanceMargin = 0.00005f;

        public override byte SerializeableID => (byte) TimelineSerializedTypes.GAMEOBJECT_TRANSFORM_CAPTURE;

        public GameObjectTransformCapturer(GameObject targetObject) {
            targetTransform = targetObject.transform;
        }

        public GameObjectTransformCapturer() {
            // No constructor. No data.
        }

        public virtual void UpdateTargetObject(GameObject newTarget) {
            if (!newTarget) {
                targetTransform = null;
                return;
            }
            targetTransform = newTarget.transform;
        }

        public void SetAnchor(GameObject anchor) {
            if (!anchor)
            {
                this.anchor = null;
                return;
            }
            else {
                lastAnchor = anchor.transform;
            }
            
            this.anchor = anchor.transform;
        }

        public void SetAnchor(Transform anchor)
        {
            this.anchor = anchor;
        }

        public void Capture(float sceneTime, bool force = false) {
            if (frames.ContainsKey(sceneTime)) {
                return;
            }

            if (!targetTransform) {
                return;
            }

            Vector3 savedPos = targetTransform.position;
            Quaternion savedRot = targetTransform.rotation;
            Vector3 rotEul = savedRot.eulerAngles;

            if (!force) {
                if (ShouldIgnoreCapture(savedPos, rotEul))
                {
                    return;
                }
            }

            if (anchor) {
                GetRelativePositionAndRotation(targetTransform, anchor, out var relPos, out var relRot);
                savedPos = relPos;
                savedRot = relRot;
            }

            lastLocalPosition = targetTransform.localPosition;
            lastLocalRotation = targetTransform.localRotation;

            TransformFrame frame = new TransformFrame() {
                position = savedPos,
                rotation = savedRot,
                time = sceneTime
            };

            frames.Add(sceneTime, frame);
        }

        // Come back to this.
        // No matter how much I lower the distance, the small nuances in performance always seem to get lost.
        // Surely there is a better way to do this.
        public virtual bool ShouldIgnoreCapture(Vector3 currentPos, Vector3 rotEul) {
            //return GetMargin(lastPosition, currentPos) < distanceMargin && GetMargin(lastRotation, rotEul) < distanceMargin;
            return false;
        }

        public Vector3 GetFirstAvailablePosition() {

            foreach (var frame in frames) {
                return frame.Value.position;
            }
            return Vector3.zero;
        }

        public float GetFirstAvailableTime() {
            foreach (var frame in frames)
            {
                return frame.Value.time;
            }

            return 0f;
        }

        public Quaternion GetFirstAvailableRotation()
        {

            foreach (var frame in frames)
            {
                return frame.Value.rotation;
            }
            return Quaternion.identity;
        }

        private float GetMargin(Vector3 first, Vector3 second) {
            return (first - second).sqrMagnitude;
        }

        public void ClearAllDataAfterTime(float sceneTime) {
            List<float> keysToRemove = new List<float>();

            foreach (var frame in frames) {
                if (frame.Key > sceneTime) {
                    keysToRemove.Add(frame.Key);
                }
            }

            foreach (var toRemove in keysToRemove)
            {
                frames.Remove(toRemove);
            }
        }

        public virtual void ApplyToObject(float sceneTime, TransformUpdateMode transformUpdateMode = TransformUpdateMode.BOTH) {
            ApplyToObject(sceneTime, targetTransform, transformUpdateMode);
        }

        public virtual void ApplyToObject(float sceneTime, Transform toApply, TransformUpdateMode transformUpdateMode = TransformUpdateMode.BOTH) {
            if (!toApply) {
                return;
            }

            TransformFrame previousFrame = default;
            TransformFrame nextFrame = default;

            bool foundPrev = false;
            bool foundNext = false;

            float firstEverTime = GetFirstAvailableTime();
            

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

            bool updatePos = false;
            bool updateRot = false;

            if (transformUpdateMode == TransformUpdateMode.BOTH || transformUpdateMode == TransformUpdateMode.ROTATION) {
                updateRot = true;
            }

            if (transformUpdateMode == TransformUpdateMode.BOTH || transformUpdateMode == TransformUpdateMode.POSITION)
            {
                updatePos = true;
            }

            bool hasAnchor = false;

            if (anchor) {
                hasAnchor = true;
            }

            if (foundNext) {
                
                Vector3 nextFramePos = nextFrame.position;
                Quaternion nextFrameRot = nextFrame.rotation;

                if (hasAnchor)
                {
                    GetRelativePositionAndRotationToWorldSpace(nextFramePos, nextFrameRot, anchor, out var newPos, out var newRot);
                    nextFramePos = newPos;
                    nextFrameRot = newRot;
                }

                if (!foundPrev || Math.Abs(firstEverTime - sceneTime) < 0.3f)
                {
                    
                    if (updatePos) {
                        toApply.position = nextFramePos;
                    }

                    if (updateRot) {
                        toApply.rotation = nextFrameRot;
                    }
                }
                else {

                    Vector3 prevFramePos = previousFrame.position;
                    Quaternion prevFrameRot = previousFrame.rotation;

                    

                    if (hasAnchor)
                    {
                        GetRelativePositionAndRotationToWorldSpace(prevFramePos, prevFrameRot, anchor, out var newPos, out var newRot);
                        prevFramePos = newPos;
                        prevFrameRot = newRot;
                    }

                    float timeDiff = nextFrame.time - previousFrame.time;
                    
                    float closeness = (sceneTime - previousFrame.time) / (nextFrame.time - previousFrame.time);

                    // Was stood still but is now in motion
                    if (timeDiff > 0.3f) {
                        return;
                    }

                    float posDiff = GetMargin(prevFramePos, nextFramePos);

                    // We are clearly missing ALOT of data so we should preserve the position its at until it actually updates.
                    if (posDiff > 10)
                    {
                        // Try again, maybe the previous one just didn't have the anchor
                        if (hasAnchor)
                        {
                            // Maybe the prev was right, its us thats off.
                            prevFramePos = previousFrame.position;
                            prevFrameRot = previousFrame.rotation;

                            posDiff = GetMargin(prevFramePos, nextFramePos);

                            // Nope we were right all along.
                            if (posDiff > 10)
                            {
                                return;
                            }
                        }
                        else {
                            // Maybe the prev is in anchor space while we are in worldspace
                            if (lastAnchor)
                            {
                                GetRelativePositionAndRotationToWorldSpace(prevFramePos, prevFrameRot, lastAnchor, out var testPos, out var testRot);
                                prevFramePos = testPos;
                                prevFrameRot = testRot;

                                posDiff = GetMargin(prevFramePos, nextFramePos);

                                // Nope we were right all along.
                                if (posDiff > 10)
                                {
                                    return;
                                }
                            }
                            else {
                                // No way its any other configuration
                                return;
                            }
                        }
                    }

                    if (updatePos && updateRot) {
                        Vector3 targetPos = Vector3.Lerp(prevFramePos, nextFramePos, closeness);
                        Quaternion targetRot = Quaternion.Lerp(prevFrameRot, nextFrameRot, closeness);

                        /*if (hasAnchor)
                        {
                            targetPos = Vector3.Lerp(toApply.position, targetPos, 0.8f);
                        }*/

                        toApply.SetPositionAndRotation(targetPos, targetRot);
                    }
                    else if (updateRot) {
                        Quaternion targetRot = Quaternion.Lerp(prevFrameRot, nextFrameRot, closeness);
                        toApply.rotation = targetRot;
                    }
                }
            }
        }

        public void GetRelativePositionAndRotation(Transform targetTransform, Transform relativeTo, out Vector3 relativePosition, out Quaternion relativeRotation)
        {
            relativePosition = relativeTo.InverseTransformPoint(targetTransform.position);
            relativeRotation = Quaternion.Inverse(relativeTo.rotation) * targetTransform.rotation;
        }

        public static void GetRelativePositionAndRotationToWorldSpace(Vector3 relativePosition, Quaternion relativeRotation, Transform relativeTo, out Vector3 worldPosition, out Quaternion worldRotation)
        {
            worldPosition = relativeTo.TransformPoint(relativePosition);
            worldRotation = relativeTo.rotation * relativeRotation;
        }

        public override int GetSize()
        {
            int sizeOfSingleVector3 = sizeof(float) * 3;
            int sizeOfSingleQuat = sizeof(float) * 4;

            // Frame pos/rot data and playhead time, count of frames
            return sizeof(int) + (frames.Count * (sizeOfSingleQuat + sizeOfSingleVector3 + sizeof(float)));
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteInt32(frames.Count);

            foreach (var keyPair in frames) {
                stream.WriteSingle(keyPair.Key);

                stream.WriteVector3(keyPair.Value.position);
                stream.WriteQuaternion(keyPair.Value.rotation);
            }
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            int frameCount = stream.ReadInt32();

            for (int i = 0; i < frameCount; i++)
            {
                float time = stream.ReadSingle();
                Vector3 position = stream.ReadVector3();
                Quaternion rotation = stream.ReadQuaternion();

                TransformFrame transformFrame = new TransformFrame() {
                    time = time,
                    position = position,
                    rotation = rotation
                };

                frames.Add(time, transformFrame);
            }
        }
    }

    public struct TransformFrame {
        public Vector3 position;
        public Quaternion rotation;
        public float time;
    }

    public enum TransformUpdateMode
    {
        POSITION,
        ROTATION,
        BOTH
    }
}
