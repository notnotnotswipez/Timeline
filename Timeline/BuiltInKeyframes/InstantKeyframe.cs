using Timeline.CameraRelated;

namespace Timeline.BuiltInKeyframes
{
    public class InstantKeyframe : Keyframe
    {
        public InstantKeyframe()
        {
            type = KeyframeTypes.INSTANT;
            texture = TimelineAssets.instantKeyframe;
        }

        public override void PreformAction(CameraController desiredObject, Keyframe previousKeyframe, float closeness)
        {
            if (closeness > 0.94)
            {
                desiredObject.ApplyCameraStateCapture(cameraStateCapture);
            }
            else {
                if (previousKeyframe != null) {
                    desiredObject.ApplyCameraStateCapture(previousKeyframe.cameraStateCapture);
                }
            }
        }
    }
}