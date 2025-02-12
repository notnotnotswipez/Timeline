using BoneLib;
using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Audio;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.RoadToMarrow;
using Il2CppSLZ.VRMK;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Audio;
using Timeline.Logging;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.Settings;
using Timeline.WorldRecording.Components;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.Extras.Visual;
using Timeline.WorldRecording.StateCapturers;
using Timeline.WorldRecording.Utils;
using UnityEngine;
using UnityEngine.Playables;

namespace Timeline.WorldRecording.Recorders
{
    public class RigmanagerRecorder : ObjectRecorder
    {
        public Dictionary<HumanBodyBones, GameObjectTransformCapturer> avatarTransformCapturers = new Dictionary<HumanBodyBones, GameObjectTransformCapturer>();
        public GameObjectTransformCapturer root;
        public RigManager recordingRigManager;

        public Il2CppSLZ.VRMK.Avatar currentRecordingAvatar;

        public Il2CppSLZ.VRMK.Avatar currentPlaybackAvatar;
        string initialAvatarBarcode;
        public string currentAvatarBarcode;

        public RigmanagerExtrasTracker rigmanagerExtrasTracker = new RigmanagerExtrasTracker();

        public MarrowEntityRecorder latestMarrowEntityRecorderPin;

        AudioSource voiceSource;
        AudioClip voiceClip = null;

        ATVRecorder prevATVRecorder = null;

        private bool hasVoice = true;
        

        private string safeName = null;

        float lastTimescale = 0f;

        private CollisionRecorder leftHandCollisionRecorder;
        private CollisionRecorder rightHandCollisionRecorder;

        public bool wasPrevInSeat = false;

        public bool hasPin = false;

        public List<Tuple<MarrowEntityRecorder, HumanBodyBones>> marrowEntityPinAssociators = new List<Tuple<MarrowEntityRecorder, HumanBodyBones>>();

        List<MarrowEntityRecorder> listenedFor = new List<MarrowEntityRecorder>();

        public override byte SerializeableID => (byte) TimelineSerializedTypes.RIGMANAGER_RECORDER;

        // Potential opportunity for facial capture? Via some sort of external program. Just need to make a "Blend Shape Capturer" and we should be good. Easy to add in the future!
        // TODO: Record root object scale (If someone uses a mod to scale their avatar).

        public override void Capture(float sceneTime)
        {
            if (currentRecordingAvatar)
            {
                if (!currentRecordingAvatar.gameObject.activeInHierarchy)
                {
                    return;
                }
            }
            else {
                return;
            }

            if (recordingRigManager.physicsRig.rbFeet.IsSleeping()) {
                return;
            }

            foreach (var transformCaptures in avatarTransformCapturers)
            {
                transformCaptures.Value.Capture(sceneTime);
            }

            root.Capture(sceneTime);

            rigmanagerExtrasTracker.Capture(sceneTime);

            if (!wasPrevInSeat)
            {
                if (recordingRigManager.activeSeat)
                {
                    wasPrevInSeat = true;

                    MarrowEntityRecorder seatRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(recordingRigManager.activeSeat.gameObject, true);

                    if (seatRecorder != null)
                    {
                        SetMarrowRecorderAnchor(seatRecorder);
                        AddEvent(sceneTime, new RigRecorderAnchorEvent(true, seatRecorder.recorderID));
                    }
                }
            }
            else {
                if (!recordingRigManager.activeSeat) {
                    wasPrevInSeat = false;
                    SetPositionalAnchor(null);
                    AddEvent(sceneTime, new RigRecorderAnchorEvent(false, 0));
                }
            }
        }

        public void SetPositionalAnchor(GameObject anchor) {
            if (anchor)
            {
                hasPin = true;

                if (currentPlaybackAvatar) {
                    currentPlaybackAvatar.transform.parent = anchor.transform;
                }
            }
            else {
                hasPin = false;

                latestMarrowEntityRecorderPin = null;

                if (currentPlaybackAvatar)
                {
                    currentPlaybackAvatar.transform.parent = null;
                }
            }

            foreach (var capturer in avatarTransformCapturers.Values) {
                capturer.SetAnchor(anchor);
            }

            root.SetAnchor(anchor);

            rigmanagerExtrasTracker.SetPositionalAnchor(anchor);
        }

        public void SetMarrowRecorderAnchor(MarrowEntityRecorder marrowEntityRecorder) {
            if (marrowEntityRecorder.targetObject) {
                MarrowEntity marrowEntity = marrowEntityRecorder.targetObject.GetComponent<MarrowEntity>();
                SetPositionalAnchor(marrowEntity.Bodies[0].gameObject);

                latestMarrowEntityRecorderPin = marrowEntityRecorder;

                if (!listenedFor.Contains(latestMarrowEntityRecorderPin)) {
                    latestMarrowEntityRecorderPin.OnBeforeDestroy += (rec) =>
                    {
                        
                        if (latestMarrowEntityRecorderPin != null) {
                            if (latestMarrowEntityRecorderPin == rec)
                            {
                                SetPositionalAnchor(null);
                            }
                        }
                    };

                    listenedFor.Add(latestMarrowEntityRecorderPin);
                }
                
            }
        }

        public Transform GetBoneTransform(HumanBodyBones bone) {
            if (currentPlaybackAvatar)
            {
                return currentPlaybackAvatar.animator.GetBoneTransform(bone);
            }

            return recordingRigManager.avatar.animator.GetBoneTransform(bone);
        }

        // TODO: Link these to the main TimelineUI, recorder management purposes.
        public override string GetName()
        {
            if (safeName == null) {

                Barcode barcodeObj = new Barcode(initialAvatarBarcode);

                // We don't have the avatar, so its probably a save file
                if (!AssetWarehouse.Instance._crateRegistry.ContainsKey(barcodeObj))
                {
                    safeName = "NO_CRATE";
                    return "NO_CRATE";
                }

                AvatarCrate avatarCrate = AssetWarehouse.Instance._crateRegistry[barcodeObj].TryCast<AvatarCrate>();

                safeName = avatarCrate.Title;
            }

            if (safeName != null)
            {
                return safeName;
            }
            return "RIGMANAGER_RECORDING";
        }

        private void AttemptCreateVoiceSource() {
            if (!voiceSource) {
                GameObject playbackSource = new GameObject("TIMELINE_VOICE");
                AudioSource source = playbackSource.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.outputAudioMixerGroup = Audio3dManager.npcVocals;

                voiceSource = source;
            }
        }

        private void UpdateVoiceSource() {
            if (voiceSource && avatarTransformCapturers.ContainsKey(HumanBodyBones.Head)) {
                Transform headTrans = avatarTransformCapturers[HumanBodyBones.Head].targetTransform;

                if (headTrans) {
                    voiceSource.transform.position = headTrans.position;
                }

                float currentTimeScale = Time.timeScale;

                float timeScaleDiff = Math.Abs(lastTimescale - currentTimeScale);
                float timeDiff = Math.Abs(voiceSource.time - WorldPlayer.playHead);

                // Change audio source pitch AND reset its time to match with the playhead due to minor desync when switching timescales
                if (timeScaleDiff > 0.1f || timeDiff > 0.1) {
                    voiceSource.pitch = currentTimeScale;
                    voiceSource.time = WorldPlayer.playHead;
                }

                lastTimescale = currentTimeScale;
            }
        }

        private void StartMicrophoneRecording() {
            TimelineAudioManager.StartMicrophoneRecording();
        }

        private void AttemptLoadVoiceClip() {
            if (!voiceClip)
            {
                TryGetMetadata("voiceclip", out var clipName);

                if (clipName != "NONE")
                {
                    hasVoice = true;

                    voiceClip = TimelineAudioManager.AttemptLoad(clipName, true);

                    if (voiceClip)
                    {
                        voiceSource.clip = voiceClip;
                    }
                }
                else {
                    hasVoice = false;
                }
            }
            else {
                voiceSource.clip = voiceClip;
            }
        }

        public override void OnPaused(float playHead)
        {
            if (hasVoice) {
                if (voiceSource)
                {
                    voiceSource.Pause();
                }
            }
        }

        public override void OnPlayStarted(float playHead)
        {
            if (hasVoice) {
                lastTimescale = 0f;
                AttemptCreateVoiceSource();
                AttemptLoadVoiceClip();

                if (voiceSource)
                {
                    
                    if (voiceClip)
                    {
                        //voiceSource.clip = voiceClip;
                        voiceSource.time = playHead;

                        voiceSource.Play();
                    }
                }
            }
        }

        public override void OnHide(bool hidden)
        {
            if (hidden)
            {
                if (currentPlaybackAvatar)
                {
                    currentPlaybackAvatar.gameObject.SetActive(false);
                    rigmanagerExtrasTracker.UpdateActiveBodylog(false);
                    rigmanagerExtrasTracker.UpdateActiveHolstersAndPouch(false);

                    if (voiceSource) {
                        voiceSource.volume = 0f;
                    }
                    
                }
            }
            else {
                if (currentPlaybackAvatar)
                {
                    currentPlaybackAvatar.gameObject.SetActive(true);
                    rigmanagerExtrasTracker.SetVisibilityToSettingVisibility();

                    if (voiceSource) {
                        voiceSource.volume = 1f;
                    }
                    
                }
            }
        }

        public override void OnInitializedPlayback()
        {
            SetPositionalAnchor(null);
            CreateAndAssignAvatar(initialAvatarBarcode);
            rigmanagerExtrasTracker.InitializePlayback();
        }

        public override void OnInitializedRecording(UnityEngine.Object rootObject)
        {
            if (GlobalSettings.recordMicrophoneClip) {
                StartMicrophoneRecording();
            }

            RigManager rigManager = rootObject.TryCast<RigManager>();

            recordingRigManager = rigManager;
            leftHandCollisionRecorder = rigManager.physicsRig.leftHand.rb.gameObject.AddComponent<CollisionRecorder>();
            rightHandCollisionRecorder = rigManager.physicsRig.rightHand.rb.gameObject.AddComponent<CollisionRecorder>();

            targetObject = rigManager.gameObject;

            initialAvatarBarcode = rigManager._avatarCrate.Barcode.ID;
            currentAvatarBarcode = initialAvatarBarcode;

            AssignAvatarBones(rigManager.avatar);

            currentRecordingAvatar = rigManager.avatar;

            AddEvent(0, new AvatarSwapEvent(initialAvatarBarcode));
            AddEvent(0, new BodyLogToggleEvent(false));
            AddEvent(0, new RigRecorderAnchorEvent(false, 0));

            rigmanagerExtrasTracker.InitOnLocalRig(this);
        }

        public override void OnPlaybackCompleted()
        {
            SetPositionalAnchor(null);
            DestroyPlaybackAvatar();
            rigmanagerExtrasTracker.Cleanup();

            if (voiceSource)
            {
                GameObject.DestroyImmediate(voiceSource.gameObject);
            }

            /*WorldPlayer.Instance.QueueEndOfLoopAction(() =>
            {
                if (voiceSource)
                {
                    if (!WorldPlayer.playing)
                    {
                        GameObject.DestroyImmediate(voiceSource.gameObject);
                    }
                    else {
                        voiceSource.Pause();
                    }
                }
            });*/
        }

        public override void Playback(float sceneTime)
        {
            foreach (var transformCaptures in avatarTransformCapturers) {
                transformCaptures.Value.ApplyToObject(sceneTime);
            }

            root.ApplyToObject(sceneTime);

            rigmanagerExtrasTracker.Playback(sceneTime);

            AttemptPeekEyes(sceneTime, GlobalSettings.eyePeek);

            UpdateVoiceSource();
        }

        private void AttemptPeekEyes(float sceneTime, float futureTime) {
            if (avatarTransformCapturers.ContainsKey(HumanBodyBones.LeftEye)) {
                avatarTransformCapturers[HumanBodyBones.LeftEye].ApplyToObject(sceneTime + futureTime, TransformUpdateMode.ROTATION);
            }

            if (avatarTransformCapturers.ContainsKey(HumanBodyBones.RightEye))
            {
                avatarTransformCapturers[HumanBodyBones.RightEye].ApplyToObject(sceneTime + futureTime, TransformUpdateMode.ROTATION);
            }
        }

        public void CreateAndAssignAvatar(string barcode) {
            Barcode barcodeObj = new Barcode(barcode);

            // We don't have the avatar, so its probably a save file
            if (!AssetWarehouse.Instance._crateRegistry.ContainsKey(barcodeObj)) {
                return;
            }

            AvatarCrate avatarCrate = AssetWarehouse.Instance._crateRegistry[barcodeObj].TryCast<AvatarCrate>();

            Action<GameObject> action = new Action<GameObject>(o =>
            {
                GameObject go = GameObject.Instantiate(o);

                Il2CppSLZ.VRMK.Avatar avatar = go.GetComponentInChildren<Il2CppSLZ.VRMK.Avatar>();
                currentPlaybackAvatar = avatar;
                currentAvatarBarcode = barcode;

                AssignAvatarBones(avatar);

                OnHide(hidden);

                // We got a pin already so we gotta repin ourselves to it (We changed avatars)
                if (latestMarrowEntityRecorderPin != null) {
                    SetMarrowRecorderAnchor(latestMarrowEntityRecorderPin);
                }
            });

            avatarCrate.LoadAsset(action);
        }

        public void RePinAttachedMarrowEntities() {
            // Copy it to prevent a mod to the list while its being iterated
            foreach (var marrowentityPinnedToUs in marrowEntityPinAssociators.ToList())
            {
                marrowentityPinnedToUs.Item1.PinToAvailableActorBone(this, marrowentityPinnedToUs.Item2);
            }
        }

        public void AssignAvatarBones(Il2CppSLZ.VRMK.Avatar avatar) {
            Animator animator = avatar.animator;

            // Wipe all, sometimes, avatars may have less (or more) bones than the previous avatar that these capturers handled.
            // This leaves the bones that weren't changed remaining in the capturers, and under specific conditions, they get recorded
            // as a mix of existing and non-existing bones, messing up things.
            
            foreach (var existingBoneMap in avatarTransformCapturers) {
                existingBoneMap.Value.UpdateTargetObject(null);
            }

            foreach (HumanBodyBones bodyBones in Enum.GetValues(typeof(HumanBodyBones))) {
                AttemptSafeAssignBone(animator, bodyBones);
            }

            if (root != null)
            {
                root.UpdateTargetObject(avatar.gameObject);
            }
            else {
                root = new GameObjectTransformCapturer(avatar.gameObject);
            }

            // Repin attached marrow entities to match the rigs new avatar
            RePinAttachedMarrowEntities();
        }

        private void AttemptSafeAssignBone(Animator animator, HumanBodyBones bodyBone) {
            Transform targetBone = null;

            try
            {
                targetBone = animator.GetBoneTransform(bodyBone);
            }
            catch (Exception e) {
                // Yeah
            }

            if (avatarTransformCapturers.ContainsKey(bodyBone))
            {
                GameObjectTransformCapturer capturer = avatarTransformCapturers[bodyBone];
                if (targetBone)
                {
                    capturer.UpdateTargetObject(targetBone.gameObject);
                }
            }
            else {
                if (targetBone) {
                    GameObjectTransformCapturer capturer = new GameObjectTransformCapturer(targetBone.gameObject);
                    avatarTransformCapturers.Add(bodyBone, capturer);
                }
            }
        }

        public void DestroyPlaybackAvatar() {
            if (currentPlaybackAvatar) {
                GameObject.DestroyImmediate(currentPlaybackAvatar.gameObject);
            }
        }

        public override void OnRecordingCompleted()
        {

            SetPositionalAnchor(null);

            // We are done here.
            rigmanagerExtrasTracker.ClearAllObjectTargetReferences();
            WorldPlayer.currentRigmanagerRecorder = null;

            TimelineAudioManager.EndMicrophoneRecording((clip) => {
                voiceClip = clip;
            });

            GameObject.Destroy(leftHandCollisionRecorder);
            GameObject.Destroy(rightHandCollisionRecorder);
        }

        public override int GetSize()
        {
            int total = 0;

            int originalSize = base.GetSize();
            total += originalSize;

            int barcodeSize = BinaryStream.GetStringLength(initialAvatarBarcode);
            total += barcodeSize;

            // Size of capture list
            total += sizeof(int);

            foreach (var keyPair in avatarTransformCapturers) {

                // HumanBodyBones ref
                total += sizeof(byte);
                total += keyPair.Value.GetSize();
            }

            // Root
            total += root.GetSize();

            total += rigmanagerExtrasTracker.GetSize();

            return total;
        }

        public override void WriteToStream(BinaryStream stream)
        {
            base.WriteToStream(stream);
            stream.WriteString(initialAvatarBarcode);
            stream.WriteInt32(avatarTransformCapturers.Count);

            foreach (var keyPair in avatarTransformCapturers)
            {
                stream.WriteByte((byte)keyPair.Key);
                stream.WriteSerializableMember(keyPair.Value);
            }

            stream.WriteSerializableMember(root);

            stream.WriteSerializableMember(rigmanagerExtrasTracker);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            base.ReadFromStream(stream);
            initialAvatarBarcode = stream.ReadString();

            TimelineLogger.Debug($"Initial barcode read {initialAvatarBarcode}");

            int avatarTransformCount = stream.ReadInt32();

            TimelineLogger.Debug($"transform count read {avatarTransformCount}");

            for (int i = 0; i < avatarTransformCount; i++) {
                HumanBodyBones bone = (HumanBodyBones) stream.ReadByte();
                GameObjectTransformCapturer gameObjectTransformCapturer = (GameObjectTransformCapturer) stream.ReadSerializableMember<GameObjectTransformCapturer>();

                avatarTransformCapturers.Add(bone, gameObjectTransformCapturer);
            }

            root = (GameObjectTransformCapturer) stream.ReadSerializableMember<GameObjectTransformCapturer>();

            rigmanagerExtrasTracker = stream.ReadSerializableMember<RigmanagerExtrasTracker>();
        }

        public override void OnSave()
        {
            TryGetMetadata("voiceclip", out var result);

            if (result != "NONE") {
                // We already saved it to a file, we don't need to save it again.
                return;
            }
            // We got a clip so we gotta save it.
            if (voiceClip != null) {

                // Random GUID
                Guid guid = Guid.NewGuid();
                string guidAsString = guid.ToString();

                SetMetadata("voiceclip", guidAsString);

                TimelineAudioManager.AttemptSaveClipToFile(guidAsString, voiceClip, true);
            }
        }
    }
}
