using System;
using System.Linq;
using BoneLib;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Audio;
using MelonLoader;
using Timeline.Audio;
using Timeline.CameraRelated;
using Timeline.Logging;
using Timeline.Menu;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.Serialization.Registry;
using Timeline.Settings;
using Timeline.Utils;
using Timeline.WorldRecording;
using Timeline.WorldRecording.Extras.Visual;
using Timeline.WorldRecording.Recorders;
using Timeline.WorldRecording.StateCapturers;
using UnityEngine;
using static MelonLoader.MelonLogger;

namespace Timeline
{
    public static class TimelineModInfo {
        public const string VERSION = "0.5.0";
    }
    public class TimelineMainClass : MelonMod
    {
        public static TimelineHolder timelineHolder;
        public static UIRig uiRig;
        public static bool waitingForInit = false;
        public static float initTime = 2f;

        public static float lastDeltaTime = 0f;
        
        public override void OnInitializeMelon()
        {
            TimelineLogger.SetLoggerInstance(LoggerInstance);

            GlobalSettings.Initialize();

            SaveManager.ValidateDirectories();
            TimelineAudioManager.ValidateDirectories();

            AssetBundle uiBundle = null;
            if (!HelperMethods.IsAndroid())
            {
                uiBundle = AssetBundle.LoadFromMemory(EmbeddedResourceHelper.GetResourceBytes("timeline.assets"));
            }
            else
            {
                TimelineLogger.Error("This mod is not supported on Quest/LemonLoader! Skipping initialization...");
                HarmonyInstance.UnpatchSelf();
                return;
            }

            TimelineAssets.LoadAssets(uiBundle);

            BonemenuHandler.Initialize();
            

            SerializableRegistry.RegisterAll();
        }
        
        // We need it early
        [HarmonyPatch(typeof(UIRig), "Awake")]
        public class RigManagerAwakePatch
        {
            public static void Prefix(UIRig __instance)
            {
                TimelineMainClass.waitingForInit = true;
                TimelineMainClass.initTime = 2f;
                TimelineMainClass.uiRig = __instance;

                TimelineLogger.Debug("UI Rig awake!");
            }
        }

        private void InitializeTimeline() {
            RuntimeCapturedAssets.CaptureAssets();

            GameObject spectatorCamera = uiRig.transform.parent.Find("Spectator Camera").gameObject;
            GameObject dupedCamera = GameObject.Instantiate(spectatorCamera);
            CameraController controller = dupedCamera.AddComponent<CameraController>();
            Camera camera = dupedCamera.GetComponentInChildren<Camera>(true);
            controller.camera = camera;
            camera.gameObject.SetActive(true);
            camera.depth = 100;
            timelineHolder = new TimelineHolder(controller);
            timelineHolder.StartUI();
            TimelineLogger.Debug("UI Started!");
        }

        public override void OnUpdate()
        {
            lastDeltaTime = Time.deltaTime;

            TimelineAudioManager.UpdateMicrophone();

            if (waitingForInit) {
                initTime -= lastDeltaTime;
                if (initTime <= 0) {
                    InitializeTimeline();
                    waitingForInit = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.K)) {
                RigidbodyCapturer.velocityStrengthMult -= 1f;
                MelonLogger.Msg(RigidbodyCapturer.velocityStrengthMult);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                RigidbodyCapturer.velocityStrengthMult += 1f;
                MelonLogger.Msg(RigidbodyCapturer.velocityStrengthMult);
            }

            if (timelineHolder != null)
            {
                TimerManager.Update();

                bool ignoreUI = timelineHolder.IgnoreUIInputs();

                if (!ignoreUI) {

                    if (Input.GetKeyDown(KeyCode.F4))
                    {
                        timelineHolder.HideAll(!timelineHolder.isHiddenCompletely);
                    }

                    if (Input.GetKeyDown(KeyCode.Space) && !timelineHolder.isHiddenCompletely)
                    {
                        if (timelineHolder.playing)
                        {
                            timelineHolder.Pause();
                        }
                        else
                        {
                            timelineHolder.Play();
                        }
                    }

                    if (!timelineHolder.isHiddenCompletely)
                    {
                        if (Input.GetKeyDown(KeyCode.H))
                        {
                            timelineHolder.HideUI(!timelineHolder.isUiHidden);
                        }
                    }
                }
                

                timelineHolder.worldPlayer.Update();
                timelineHolder.Update();
            }
        }
    }
}