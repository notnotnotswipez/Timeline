using Il2CppSLZ.Bonelab.SaveData;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timeline.Settings
{
    public class GlobalSettings
    {

        public static bool transferActorProp = true;
        public static bool recordAvatar = true;
        public static bool useWorldObject = false;
        public static bool recordMicrophoneClip = true;
        public static bool moveMouthToMicrophone = true;
        public static bool hidePlaybackPouches = false;
        public static bool hidePlaybackBodylog = false;
        public static float eyePeek = 0f;
        public static string referenceTrackName = "None";
        public static string targetMic = null;

        public static float camSensitivity = 10f;
        public static float camLerpSpeed = 10f;
        public static float camSpeed = 5f;

        static MelonPreferences_Category category;

        static MelonPreferences_Entry<bool> MP_transferActorProp;
        static MelonPreferences_Entry<bool> MP_recordAvatar;
        static MelonPreferences_Entry<bool> MP_useWorldObject;
        static MelonPreferences_Entry<bool> MP_recordMicrophoneClip;
        static MelonPreferences_Entry<bool> MP_moveMouthToMicrophone;
        static MelonPreferences_Entry<bool> MP_hidePlaybackPouches;
        static MelonPreferences_Entry<bool> MP_hidePlaybackBodylog;
        static MelonPreferences_Entry<float> MP_eyePeek;
        static MelonPreferences_Entry<string> MP_referenceTrackName;
        static MelonPreferences_Entry<string> MP_targetMic;
        static MelonPreferences_Entry<float> MP_camSensitivity;
        static MelonPreferences_Entry<float> MP_camLerpSpeed;
        static MelonPreferences_Entry<float> MP_camSpeed;

        public static void Initialize() {
            category = MelonPreferences.CreateCategory("Timeline");
            MP_transferActorProp = category.CreateEntry<bool>("transferActorProp", true);
            MP_recordAvatar = category.CreateEntry<bool>("recordAvatar", true);
            MP_useWorldObject = category.CreateEntry<bool>("useWorldObject", false);
            MP_recordMicrophoneClip = category.CreateEntry<bool>("recordMicrophoneClip", true);
            MP_moveMouthToMicrophone = category.CreateEntry<bool>("moveMouthToMicrophone", true);
            MP_hidePlaybackPouches = category.CreateEntry<bool>("hidePlaybackPouches", false);
            MP_hidePlaybackBodylog = category.CreateEntry<bool>("hidePlaybackBodylog", false);

            MP_eyePeek = category.CreateEntry<float>("eyePeek", 0f);

            MP_referenceTrackName = category.CreateEntry<string>("referenceTrackName", "None");
            MP_targetMic = category.CreateEntry<string>("targetMic", "Default");


            MP_camSensitivity = category.CreateEntry<float>("camSensitivity", 10);
            MP_camLerpSpeed = category.CreateEntry<float>("camLerpSpeed", 10);
            MP_camSpeed = category.CreateEntry<float>("camSpeed", 5);

            UpdateStoredValues();
        }

        private static void UpdateStoredValues() {
            transferActorProp = MP_transferActorProp.Value;
            recordAvatar= MP_recordAvatar.Value;
            useWorldObject = MP_useWorldObject.Value;
            recordMicrophoneClip = MP_recordMicrophoneClip.Value;
            moveMouthToMicrophone = MP_moveMouthToMicrophone.Value;
            hidePlaybackPouches = MP_hidePlaybackPouches.Value;
            hidePlaybackBodylog = MP_hidePlaybackBodylog.Value;

            eyePeek = MP_eyePeek.Value;

            referenceTrackName = MP_referenceTrackName.Value;

            camSensitivity = MP_camSensitivity.Value;
            camLerpSpeed = MP_camLerpSpeed.Value;
            camSpeed = MP_camSpeed.Value;

            string attemptedMic = MP_targetMic.Value;

            if (attemptedMic == "Default") {
                attemptedMic = null;
            }

            targetMic = attemptedMic;
        }

        public static void Save() {
            MP_transferActorProp.Value = transferActorProp;
            MP_recordAvatar.Value = recordAvatar;
            MP_useWorldObject.Value = useWorldObject;
            MP_recordMicrophoneClip.Value = recordMicrophoneClip;
            MP_moveMouthToMicrophone.Value = moveMouthToMicrophone;
            MP_hidePlaybackPouches.Value = hidePlaybackPouches;
            MP_hidePlaybackBodylog.Value = hidePlaybackBodylog;

            MP_eyePeek.Value = eyePeek;

            MP_referenceTrackName.Value = referenceTrackName;

            MP_camSensitivity.Value = camSensitivity;
            MP_camLerpSpeed.Value = camLerpSpeed;
            MP_camSpeed.Value = camSpeed;

            string attemptedMic = targetMic;

            if (attemptedMic == null) {
                attemptedMic = "Default";
            }

            MP_targetMic.Value = attemptedMic;

            category.SaveToFile(false);
        }
    }
}
