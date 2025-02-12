using BoneLib.BoneMenu;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Audio;
using Timeline.Serialization;
using Timeline.Settings;
using Timeline.WorldRecording;
using Timeline.WorldRecording.Recorders;
using Timeline.WorldRecording.Utils;
using UnityEngine;
using UnityEngine.Playables;

namespace Timeline.Menu
{
    public class BonemenuHandler
    {

        public static void Initialize() {
            Page mainPage = Page.Root.CreatePage("Timeline", Color.yellow);

            Page scenePage = mainPage.CreatePage("Scene", Color.white);
            Page managementPage = scenePage.CreatePage("Management", Color.white);
            Page referencePage = managementPage.CreatePage("Audio Reference", Color.white);
            Page recordersPage = managementPage.CreatePage("Recorders", Color.white);

            Page settingsPage = scenePage.CreatePage("Settings", Color.white);
            Page microphonePage = settingsPage.CreatePage("Microphone", Color.white);
            Page microphoneDevicesPage = microphonePage.CreatePage("Devices", Color.white);

            Page savingPage = mainPage.CreatePage("Saving", Color.white);

            PopulateScenePage(scenePage);
            PopulateSavingPage(savingPage);

            PopulateManagementPage(managementPage);
            PopulateRecordersPage(recordersPage);
            PopulateReferenceClipsPage(referencePage);

            PopulateSettingsPage(settingsPage);
            PopulateMicrophonePage(microphonePage);
            PopulateDevicesPage(microphoneDevicesPage);
        }

        private static void PopulateRecordersPage(Page page) {
            page.RemoveAll();

            page.CreateFunction("Refresh", Color.white, () =>
            {
                PopulateRecordersPage(page);
            });

            if (WorldPlayer.Instance != null) {
                foreach (var recorder in WorldPlayer.Instance.playbackRecorders)
                {
                    // Just rig recorders for now, lots of objects get spawned and it would be a bit nightmarish to handle them through this silly Bonemenu.
                    if (recorder is RigmanagerRecorder) {
                        CreateRecorderPage(recorder, page);
                    }
                }
            }
        }

        private static void PopulateDevicesPage(Page page) {
            page.RemoveAll();

            page.CreateFunction("Refresh", Color.white, () =>
            {
                PopulateDevicesPage(page);
            });

            string selected = GlobalSettings.targetMic;

            if (selected == null) {
                selected = "Default";
            }

            List<string> allOptions = new List<string>();
            allOptions.Add("Default");
            allOptions.AddRange(Microphone.devices);

            MakeStringListPage(page, allOptions, selected, (s) =>
            {
                string target = s;
                if (target == "Default") {
                    target = null;
                }
                GlobalSettings.targetMic = target;
                PopulateDevicesPage(page);
                GlobalSettings.Save();
            });
        }

        private static void PopulateReferenceClipsPage(Page page)
        {
            page.RemoveAll();

            page.CreateFunction("Refresh", Color.white, () =>
            {
                PopulateReferenceClipsPage(page);
            });

            string selected = GlobalSettings.referenceTrackName;

            if (selected == null)
            {
                selected = "None";
            }

            List<string> allOptions = new List<string>();
            allOptions.Add("None");
            allOptions.AddRange(TimelineAudioManager.GetAllAudioClipNames());

            MakeStringListPage(page, allOptions, selected, (s) =>
            {
                GlobalSettings.referenceTrackName = s;
                PopulateReferenceClipsPage(page);
                GlobalSettings.Save();
            });
        }

        private static void PopulateMicrophonePage(Page page) {
            page.CreateBool("Record Voice", Color.yellow, GlobalSettings.recordMicrophoneClip, (b) =>
            {
                GlobalSettings.recordMicrophoneClip = b;
                GlobalSettings.Save();
            });

            page.CreateBool("Move Jaw", Color.yellow, GlobalSettings.moveMouthToMicrophone, (b) =>
            {
                GlobalSettings.moveMouthToMicrophone = b;
                GlobalSettings.Save();
            });
        }

        private static void MakeStringListPage(Page parent, List<string> strings, string currentSelection, Action<string> onSelected) {

            foreach (var entry in strings) {
                Color color = Color.yellow;
                if (entry == currentSelection) {
                    color = Color.green;
                }

                parent.CreateFunction(entry, color, () =>
                {
                    onSelected.Invoke(entry);
                });
            }
        }

        private static void CreateRecorderPage(ObjectRecorder recorder, Page parent) {

            string name = recorder.GetName();
            string attemptedName = name;
            int index = 1;

            while (PageContainsElementWithThisName(parent, attemptedName)) {
                attemptedName = $"{name} ({index})";
                index++;
            }

            Page page = parent.CreatePage(attemptedName, Color.yellow);
            page.CreateBool("Hide", Color.cyan, recorder.hidden, (b) =>
            {
                recorder.hidden = b;
                recorder.OnHide(b);
            });

            page.CreateFunction("Delete Recorder", Color.yellow, () =>
            {
                WorldPlayer.Instance.RemoveRecorder(recorder);
                PopulateRecordersPage(parent);
                BoneLib.BoneMenu.Menu.OpenPage(parent);
            });
        }

        private static bool PageContainsElementWithThisName(Page page, string name) {
            foreach (var element in page.Elements) {
                if (element.ElementName == name) {
                    return true;
                }
            }

            return false;
        }

        private static void PopulateScenePage(Page page) {
            page.CreateFunction("Play", Color.green, () =>
            {
                if (!WorldPlayer.paused)
                {
                    TimelineMainClass.timelineHolder.worldPlayer.Stop(false);
                }

                TimelineMainClass.timelineHolder.worldPlayer.Play(0, true, TimelineAudioManager.AttemptLoad(GlobalSettings.referenceTrackName));
            });

            page.CreateFunction("Pause", Color.yellow, () =>
            {
                TimelineMainClass.timelineHolder.worldPlayer.Pause();
            });

            page.CreateFunction("Stop", Color.cyan, () =>
            {
                TimelineMainClass.timelineHolder.worldPlayer.Stop();
            });

            page.CreateFunction("Record", Color.red, () =>
            {
                RecordingUtils.AttemptBeginRecordingSequence();
            });
        }

        private static void PopulateManagementPage(Page page) {
            page.CreateFunction("Remove Last Recorder", Color.yellow, () =>
            {
                TimelineMainClass.timelineHolder.worldPlayer.Stop();
                TimelineMainClass.timelineHolder.worldPlayer.RemoveLastRecorder();
            });

            page.CreateFunction("Clear Scene Entirely", Color.yellow, () =>
            {
                TimelineMainClass.timelineHolder.worldPlayer.Stop();
                TimelineMainClass.timelineHolder.worldPlayer.RemoveAllRecorders();
            });
        }

        private static void PopulateSettingsPage(Page page) {
            page.CreateBool("Record Avatar", Color.yellow, GlobalSettings.recordAvatar, (b) =>
            {
                GlobalSettings.recordAvatar = b;
                GlobalSettings.Save();
            });

            page.CreateBool("Transfer Props", Color.yellow, GlobalSettings.transferActorProp, (b) =>
            {
                GlobalSettings.transferActorProp = b;
                GlobalSettings.Save();
            });

            page.CreateBool("Use World Object", Color.yellow, GlobalSettings.useWorldObject, (b) =>
            {
                GlobalSettings.useWorldObject = b;
                GlobalSettings.Save();
            });

            page.CreateBool("Hide Bodylogs", Color.yellow, GlobalSettings.hidePlaybackBodylog, (b) =>
            {
                GlobalSettings.hidePlaybackBodylog = b;
                GlobalSettings.Save();
            });

            page.CreateBool("Hide Pouches", Color.yellow, GlobalSettings.hidePlaybackPouches, (b) =>
            {
                GlobalSettings.hidePlaybackPouches = b;
                GlobalSettings.Save();
            });

            page.CreateFloat("Eye Peek", Color.yellow, GlobalSettings.eyePeek, 0.05f, 0f, 10f, (f) =>
            {
                GlobalSettings.eyePeek = f;
                GlobalSettings.Save();
            });
        }

        private static void PopulateSavingPage(Page page) {
            page.CreateString("Name", Color.yellow, "default", (s) =>
            {
                TimelineMainClass.timelineHolder.selectedSessionName = s;
            });

            page.CreateFunction("SAVE", Color.green, () => {
                SaveManager.SaveToFile(TimelineMainClass.timelineHolder.selectedSessionName);
            });

            page.CreateFunction("LOAD", Color.yellow, () => {
                SaveManager.LoadFromFile(TimelineMainClass.timelineHolder.selectedSessionName);
            });
        }
    }
}
