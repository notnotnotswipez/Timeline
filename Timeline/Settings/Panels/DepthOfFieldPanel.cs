using Timeline.Settings.Capture;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Timeline.Settings.Panels
{
    // None of this works as it was stripped from volumes in Bonelab. Sad.
    public class DepthOfFieldPanel : SettingsPanel
    {
        public override string Name => "Depth of Field";
        private DepthOfField dof;

        public override void InitializeMenu()
        {
            CreateVolume();
            AddButtonSetting("Enabled", false, b =>
            {
                dof.active = b;
            });
            AddSliderSetting("Focus Distance", 0, 0, 100, f =>
            {
                dof.focusDistance.value = f;
                dof.focusDistance.overrideState = true;
            });
            AddSliderSetting("Aperture", 0, 0, 32, f =>
            {
                dof.aperture.value = f;
                dof.aperture.overrideState = true;
            });
            AddSliderSetting("Focal Length", 0, 0, 300, f =>
            {
                dof.focalLength.value = f;
                dof.focalLength.overrideState = true;
            });
        }

        public override SettingsPanelCapture MakeCapture()
        {
            SettingsPanelCapture capture = new SettingsPanelCapture(sizeof(bool) + sizeof(float) * 3);
            capture.AddBool(dof.active);
            capture.AddFloat(dof.focusDistance.value);
            capture.AddFloat(dof.aperture.value);
            capture.AddFloat(dof.focalLength.value);
            return capture;
        }

        public override void HandleCapture(SettingsPanelCapture capture, SettingsPanelCapture previousCapture, float diff)
        {
            if (previousCapture == null)
            {
                if (diff > 0.98f)
                {
                    dof.active = capture.GetBool();
                    dof.focusDistance.value = capture.GetFloat();
                    dof.focusDistance.overrideState = true;
                    dof.aperture.value = capture.GetFloat();
                    dof.aperture.overrideState = true;
                    dof.focalLength.value = capture.GetFloat();
                    dof.focalLength.overrideState = true;
                }
            }
            else
            {
                if (diff > 0.98f)
                {
                    dof.active = capture.GetBool();
                    dof.focusDistance.value = capture.GetFloat();
                    dof.focusDistance.overrideState = true;
                    dof.aperture.value = capture.GetFloat();
                    dof.aperture.overrideState = true;
                    dof.focalLength.value = capture.GetFloat();
                    dof.focalLength.overrideState = true;
                }
                else
                {
                    dof.active = previousCapture.GetBool();
                    dof.focusDistance.value = Mathf.Lerp(previousCapture.GetFloat(), capture.GetFloat(), diff);
                    dof.focusDistance.overrideState = true;
                    dof.aperture.value = Mathf.Lerp(previousCapture.GetFloat(), capture.GetFloat(), diff);
                    dof.aperture.overrideState = true;
                    dof.focalLength.value = Mathf.Lerp(previousCapture.GetFloat(), capture.GetFloat(), diff);
                    dof.focalLength.overrideState = true;
                }
            }
        }

        private void CreateVolume()
        {
            GameObject volumeObject = new GameObject("DOFVolume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 3f;
            volume.weight = 1;
            volume.sharedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            dof = volume.sharedProfile.Add<DepthOfField>();
            dof.active = false;
            dof.mode.value = DepthOfFieldMode.Bokeh;
            dof.mode.overrideState = true;
        }
    }
}