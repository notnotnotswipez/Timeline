using MelonLoader;
using Timeline.CameraRelated;
using Timeline.Logging;
using Timeline.Settings.Capture;
using Timeline.Settings.Menu;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Timeline.Settings.Panels
{
    public class CameraPanel : SettingsPanel
    {
        public override string Name => "Camera Options";

        public bool postProcess = true;

        UniversalAdditionalCameraData camData;

        public SliderSetting rollSetting;

        public override void InitializeMenu()
        {
            camData = TimelineMainClass.timelineHolder.controller.camera.gameObject.GetComponent<UniversalAdditionalCameraData>();

            CameraController controller = TimelineMainClass.timelineHolder.controller;
            controller.sensitivity = GlobalSettings.camSensitivity;
            controller.lerpSpeed = GlobalSettings.camLerpSpeed;


            rollSetting = AddSliderSetting("Camera Roll", controller.roll, -180, 180, f =>
            {
                controller.roll = f;

                Vector3 eulerAngles = controller.transform.eulerAngles;
                eulerAngles.z = f;

                controller.transform.eulerAngles = eulerAngles;
                TimelineLogger.Debug("Camera roll: " + f);
            });

            AddSliderSetting("Camera Speed", controller.speed, 1, 10, f =>
            {
                controller.speed = f;
                GlobalSettings.camSpeed = f;
                GlobalSettings.Save();
            });

            AddSliderSetting("Camera Sensitivity", controller.sensitivity, 1, 20, f =>
            {
                controller.sensitivity = f;
                GlobalSettings.camSensitivity = f;
                GlobalSettings.Save();
            });

            AddSliderSetting("Camera Smooth Speed", controller.lerpSpeed, 1, 20, f =>
            {
                controller.lerpSpeed = f;
                GlobalSettings.camLerpSpeed = f;
                GlobalSettings.Save();
            });

            AddButtonSetting("Post-Processing", postProcess, (b) =>
            {
                postProcess = b;
                UpdatePostProcessState();
            });
        }

        private void UpdatePostProcessState()
        {
            camData.renderPostProcessing = postProcess;
        }

        public override SettingsPanelCapture MakeCapture()
        {
            SettingsPanelCapture capture = new SettingsPanelCapture(1);
            return capture;
        }

        public override void HandleCapture(SettingsPanelCapture capture, SettingsPanelCapture previousCapture, float diff)
        {
            // We don't need to handle this. Its already done for us by the CameraStateCapture
            return;
        }
    }
}