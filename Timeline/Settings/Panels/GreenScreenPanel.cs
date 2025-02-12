using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Settings.Capture;
using UnityEngine;

namespace Timeline.Settings.Panels
{
    public class GreenScreenPanel : SettingsPanel
    {
        public override string Name => "Green Screen";

        Camera targetCamera;
        GameObject greenScreenPanel;
        float originalNearClip = 0.001f;
        float nearClipOffset = 0.04f;
        float distance = 1f;
        bool enabled = false;
        bool inverse = false;

        public override void HandleCapture(SettingsPanelCapture capture, SettingsPanelCapture previousCapture, float diff)
        {
            // Do not handle!
            return;
        }

        public override void InitializeMenu()
        {
            greenScreenPanel = GameObject.Instantiate(TimelineAssets.greenScreenPlane);
            targetCamera = TimelineMainClass.timelineHolder.controller.camera;
            greenScreenPanel.transform.parent = targetCamera.transform;
            originalNearClip = targetCamera.nearClipPlane;

            UpdateState();

            MoveGreenScreenPlane();

            AddButtonSetting("Enabled", enabled, (b) =>
            {
                enabled = b;
                UpdateState();

                MoveGreenScreenPlane();
            });

            AddButtonSetting("Invert", inverse, (b) =>
            {
                inverse = b;
                UpdateState();

                MoveGreenScreenPlane();
            });

            AddSliderSetting("Distance", distance, 0.2f, 10f, (f) =>
            {
                distance = f;

                MoveGreenScreenPlane();

                if (inverse) {
                    targetCamera.nearClipPlane = f - nearClipOffset;
                }
            });

            /*AddSliderSetting("Near Clip Offset", nearClipOffset, 0f, 1, (f) =>
            {
                nearClipOffset = f;

                if (inverse)
                {
                    targetCamera.nearClipPlane = distance - nearClipOffset;
                }
            });*/
        }

        public void UpdateState() {
            if (!enabled) {
                greenScreenPanel.SetActive(false);
                ResetCamera();
                return;
            }

            if (inverse) {
                greenScreenPanel.SetActive(false);
                targetCamera.nearClipPlane = distance - nearClipOffset;
            }
            else {
                greenScreenPanel.SetActive(true);
                ResetCamera();
            }
        }

        public void ResetCamera() {
            targetCamera.nearClipPlane = originalNearClip;
        }

        public void MoveGreenScreenPlane() {
            greenScreenPanel.transform.position = targetCamera.transform.position + (targetCamera.transform.forward * distance);
            greenScreenPanel.transform.LookAt(targetCamera.transform);
        }

        public override SettingsPanelCapture MakeCapture()
        {
            return new SettingsPanelCapture(1);
        }
    }
}
