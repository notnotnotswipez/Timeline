using Timeline.CameraRelated;
using UnityEngine;
using UnityEngine.Timeline;

namespace Timeline.BuiltInKeyframes
{
    public class LinearKeyframe : Keyframe
    {
        public LinearKeyframe()
        {
            type = KeyframeTypes.LINEAR;
            texture = TimelineAssets.linearKeyframe;
        }

        public override void PreformAction(CameraController desiredObject, Keyframe previousKeyframe, float closeness)
        {
            if (previousKeyframe != null)
            {
                Quaternion previousRotation = Quaternion.Euler(previousKeyframe.cameraStateCapture.rotationX, previousKeyframe.cameraStateCapture.rotationY, previousKeyframe.cameraStateCapture.rotationZ);
                Quaternion currentRotation = Quaternion.Euler(cameraStateCapture.rotationX, cameraStateCapture.rotationY, cameraStateCapture.rotationZ);
                Quaternion interpolatedRotation = Quaternion.Slerp(previousRotation, currentRotation, closeness);

                CameraStateCapture interpolatedState = new CameraStateCapture
                {
                    positionX = Mathf.Lerp(previousKeyframe.cameraStateCapture.positionX, cameraStateCapture.positionX, closeness),
                    positionY = Mathf.Lerp(previousKeyframe.cameraStateCapture.positionY, cameraStateCapture.positionY, closeness),
                    positionZ = Mathf.Lerp(previousKeyframe.cameraStateCapture.positionZ, cameraStateCapture.positionZ, closeness),
                    rotationX = interpolatedRotation.eulerAngles.x,
                    rotationY = interpolatedRotation.eulerAngles.y,
                    rotationZ = interpolatedRotation.eulerAngles.z,
                    fov = Mathf.Lerp(previousKeyframe.cameraStateCapture.fov, cameraStateCapture.fov, closeness)
                };
                desiredObject.ApplyCameraStateCapture(interpolatedState, true);
            }
            else
            {
                if (closeness > 0.9)
                {
                    desiredObject.ApplyCameraStateCapture(cameraStateCapture, true);
                }
            }
        }
    }
}