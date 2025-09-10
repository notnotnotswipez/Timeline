using Il2CppSLZ.Marrow.Audio;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Logging;
using Timeline.Patches.Rigmanager;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.Serialization.Registry;
using Timeline.WorldRecording.Extensions;
using Timeline.WorldRecording.Recorders;
using UnityEngine;
using UnityEngine.Profiling;

namespace Timeline.WorldRecording
{
    public class WorldPlayer : SerializableMember
    {

        public float totalSceneLength;
        public static float playHead;
        public static bool playing = false;
        public static bool recording = false;
        public static float latestRecordTimeSinceStartup = 0f;

        public static bool paused = false;

        // 0.1 is pretty good, timestep is necessary as recording every frame is unnecessary and wasteful! (300 fps recording 55 bones every frame gets large fast) Regarding storage and memory usage
        public const float rateOfRecord = 0.05f;
        private float recordTime = rateOfRecord;

        public static WorldPlayer Instance;

        private List<Action> endOfUpdateCycleQueue = new List<Action>();

        AudioSource referenceTrackSource;

        private static ushort lastRecorderID = 1;

        public WorldPlayer() {
            // Singleton
            Instance = this;
        }


        public List<ObjectRecorder> playbackRecorders = new List<ObjectRecorder>();
        private List<ObjectRecorder> currentlyRecording = new List<ObjectRecorder>();


        private List<ObjectRecorder> lastRecorderSessions = new List<ObjectRecorder>();

        public static RigmanagerRecorder currentRigmanagerRecorder = null;

        private Dictionary<ushort, ObjectRecorder> objectRecorderIDMap = new Dictionary<ushort, ObjectRecorder>();

        float lastTimeScale = 0f;

        public override byte SerializeableID => (byte) TimelineSerializedTypes.WORLD_PLAYER;

        public void Play(float playHeadOverride = 0, bool runPlayCallback = true, AudioClip referenceTrack = null) {

            if (paused)
            {
                paused = false;
            }
            else {
                playHead = playHeadOverride;
            }
            
            playing = true;

            if (runPlayCallback) {
                foreach (var objRecorder in playbackRecorders)
                {
                    objRecorder.OnPlayStarted(playHead);
                }
            }

            if (referenceTrack)
            {
                AttemptCreateReferenceSource();
                referenceTrackSource.clip = referenceTrack;
                referenceTrackSource.time = playHead;
                referenceTrackSource.Play();
            }
        }

        public void Pause() {
            playing = false;
            paused = true;

            foreach (var objRecorder in playbackRecorders)
            {
                objRecorder.OnPaused(playHead);
            }

            if (referenceTrackSource) {
                referenceTrackSource.Pause();
            }
        }

        public void StartRecording(bool playback = true, AudioClip referenceTrack = null) {
            if (playback) {
                Play(0, true, referenceTrack);
            }

            recording = true;
            latestRecordTimeSinceStartup = Time.realtimeSinceStartup;

            foreach (var recorder in currentlyRecording) {
                recorder.recording = true;
            }
        }

        private void AttemptCreateReferenceSource() {
            if (!referenceTrackSource) {
                GameObject playbackSource = new GameObject("TIMELINE_REFERENCE");
                AudioSource source = playbackSource.AddComponent<AudioSource>();
                source.spatialBlend = 0f;
                source.outputAudioMixerGroup = Audio3dManager.npcVocals;

                referenceTrackSource = source;
            }
        }

        public void Stop(bool totalWipe = true, bool clearEvents = true)
        {
            playing = false;
            paused = false;

            FinishRecording();

            ClearScene(totalWipe, clearEvents);


            TimelineLogger.Debug("Stopped timeline playback. Scene length: " + totalSceneLength + " and playhead: " + playHead);

            playHead = 0;

            
            if (referenceTrackSource)
            {
                if (totalWipe)
                {
                    GameObject.DestroyImmediate(referenceTrackSource.gameObject);
                }
                else {
                    referenceTrackSource.Pause();
                }
            }
        }

        public void AddRecorderToRecord(ObjectRecorder recorder) {

            // Doesn't have an ID
            if (recorder.recorderID == 0) {
                recorder.recorderID = lastRecorderID++;
                objectRecorderIDMap.Add(recorder.recorderID, recorder);
            }

            if (currentlyRecording.Count == 0) {
                lastRecorderSessions.Clear();
            }

            QueueEndOfLoopAction(() => {
                currentlyRecording.Add(recorder);
            });
            
            lastRecorderSessions.Add(recorder);

            // Its related to this fella here
            if (currentRigmanagerRecorder != null) {

                // We are not associated with ourselves, we are ourselves
                if (currentRigmanagerRecorder.recorderID != recorder.recorderID) {
                    currentRigmanagerRecorder.associatedRecorders.Add(recorder.recorderID);
                }
            }
        }

        public void RemoveLastRecorder() {

            foreach (var lastRec in lastRecorderSessions) {
                if (lastRec.initialized)
                {
                    lastRec.OnPlaybackCompleted();
                }

                // Forget it in the object map
                objectRecorderIDMap.Remove(lastRec.recorderID);
            }

            playbackRecorders.RemoveAll((x) => lastRecorderSessions.Contains(x));

            lastRecorderSessions.Clear();

            UpdateTotalTakeLength();
        }

        public void RemoveRecorder(ObjectRecorder recorder) {
            if (recorder.initialized) {
                recorder.OnPlaybackCompleted();
            }

            playbackRecorders.Remove(recorder);

            objectRecorderIDMap.Remove(recorder.recorderID);

            foreach (var associatedId in recorder.associatedRecorders) {
                ObjectRecorder recorderAssociated = GetObjectRecorder(associatedId);

                if (recorderAssociated != null) {
                    if (recorderAssociated.initialized)
                    {
                        recorderAssociated.OnPlaybackCompleted();
                    }

                    playbackRecorders.Remove(recorderAssociated);
                    objectRecorderIDMap.Remove(recorderAssociated.recorderID);
                }
            }

            UpdateTotalTakeLength();
        }

        public void RemoveAllRecorders()
        {
            foreach (var recorder in playbackRecorders) {
                if (recorder.initialized) {
                    recorder.OnPlaybackCompleted();
                }
            }

            playbackRecorders.Clear();
            objectRecorderIDMap.Clear();
            MarrowEntityRecorder.ClearAllEvents();

            lastRecorderID = 1;

            UpdateTotalTakeLength();
        }

        public ObjectRecorder GetObjectRecorder(ushort ID) {
            if (objectRecorderIDMap.ContainsKey(ID)) {
                return objectRecorderIDMap[ID];
            }

            return null;
        }

        public void Update()
        {
            HandleUpdateActionQueue();

            if (playing)
            {
                float deltaTime = TimelineMainClass.lastDeltaTime;

                playHead += deltaTime;

                UpdateScene(playHead);

                if (!recording)
                {
                    if (playHead >= totalSceneLength)
                    {
                        Stop();
                    }
                }

                if (referenceTrackSource) {
                    float currentTimeScale = Time.timeScale;

                    float timeScaleDiff = Math.Abs(lastTimeScale - currentTimeScale);
                    float timeDiff = Math.Abs(referenceTrackSource.time - WorldPlayer.playHead);

                    // Change audio source pitch AND reset its time to match with the playhead due to minor desync when switching timescales
                    if (timeScaleDiff > 0.1f || timeDiff > 0.1f)
                    {
                        referenceTrackSource.pitch = currentTimeScale;
                        referenceTrackSource.time = playHead;
                    }

                    lastTimeScale = currentTimeScale;
                }
            }
        }

        // We do not do this one on the WorldPlayer update loop! Its actually ran from the PlayerArtPatch. This is regarding player
        // Bone positions being corrected, but as a result, the desynced update rate of every other recorder caused floating issues. (IE, a held object would drift slightly away from the hand of the recorded avatar when in motion)
        // Since every recorder is now being updated at the same time the rigmanager's art is just before changing the bones around, there shouldn't be any drift
        // issues on recorded objects.
        public void RecordLoop() {
            if (!recording) {
                return;
            }

            // Someones tryna break it.
            if (paused) {
                return;
            }
            recordTime -= TimelineMainClass.lastDeltaTime;
            if (recordTime <= 0)
            {
                recordTime = rateOfRecord;
                UpdateCurrentlyRecording(playHead);
            }

            if (playHead > totalSceneLength)
            {
                totalSceneLength = playHead;
            }
        }

        public void OverrideRecording(ObjectRecorder objectRecorder) {

            if (!typeof(OverridableRecording).IsAssignableFrom(objectRecorder.GetType())) {
                TimelineLogger.Error("Tried to override recording data from a non-overridable recording!");
                return;
            }

            OverridableRecording overridableRecording = (OverridableRecording) objectRecorder;

            if (objectRecorder.recording) {
                return;
            }

            objectRecorder.recording = true;

            objectRecorder.ClearAllEventsAfterTime(playHead);
            overridableRecording.OnOverrideStart(playHead);


            QueueEndOfLoopAction(() =>
            {
                playbackRecorders.Remove(objectRecorder);

                // We do not call AddRecorderToRecord on this because that modifies the OG recorder holder that "owns" this recorder.
                // This is more of a behavior choice, not a bug fix. We don't want people to remove recorders that interacted with things
                // That were originally from other recordings and end up deleting things (Even though they were interacted with).
                currentlyRecording.Add(objectRecorder);
            });
        }

        public void UpdateCurrentlyRecording(float sceneTime) {
            foreach (ObjectRecorder recorder in currentlyRecording)
            {
                recorder.Capture(sceneTime);
            }
        }

        public void UpdateScene(float sceneTime) {
            foreach (ObjectRecorder recorder in playbackRecorders)
            {
                // They have no more data, they do not need to update.
                if (recorder.takeLength < sceneTime)
                {
                    continue;
                }

                if (!recorder.initialized)
                {
                    if (recorder.initTime < sceneTime)
                    {
                        try {
                            recorder.OnInitializedPlayback();
                            if (recorder.hidden) {
                                recorder.OnHide(true);
                            }
                        }
                        catch (Exception ex) {
                            // We don't want to brick the scene
                        }
                        recorder.initialized = true;
                    }
                }

                if (recorder.initialized) {

                    bool isSimpleSpawn = false;

                    if (recorder is SimpleSpawnRecorder) {
                        isSimpleSpawn = true;
                    }

                    // This means a replay was triggered, and the object that was supposed to only exist after a certain point (A Magazine or something) exists before its creation. Odd phrase!
                    if (recorder.initTime > sceneTime)
                    {
                        recorder.OnPlaybackCompleted();
                        recorder.initialized = false;

                        if (isSimpleSpawn) {
                            // 0.3 is a fine margin
                            if (recorder.initTime - sceneTime < 0.3f)
                            {
                                // It was scrubbed and we just went past it
                                recorder.OnInitializedPlayback();
                            }
                        }
                    }

                    if (isSimpleSpawn)
                    {
                        continue;
                    }

                    recorder.CheckAndRunEvents(sceneTime);
                    recorder.Playback(sceneTime);
                }
            }
        }

        public void QueueEndOfLoopAction(Action action) {
            endOfUpdateCycleQueue.Add(action);
        }

        public void HandleUpdateActionQueue() {
            if (endOfUpdateCycleQueue.Count > 0) {
                foreach (var action in endOfUpdateCycleQueue)
                {
                    action.Invoke();
                }

                endOfUpdateCycleQueue.Clear();
            }
        }

        public void ClearScene(bool totalWipe, bool clearEvents) {
            foreach (ObjectRecorder recorder in playbackRecorders)
            {
                if (recorder.initialized)
                {
                    if (totalWipe) {
                        recorder.OnPlaybackCompleted();
                        recorder.initialized = false;
                    }

                    if (clearEvents) {
                        recorder.MarkAllEventsAsNotRun();
                    }
                }
            }
        }

        public void FinishRecording() {
            foreach (ObjectRecorder recorder in currentlyRecording)
            {
                recorder.OnRecordingCompleted();
                recorder.recording = false;
                recorder.takeLength = playHead;
                TimelineLogger.Debug("Set take length of recorder to: " + recorder.takeLength);
            }
            playbackRecorders.AddRange(currentlyRecording);
            currentlyRecording.Clear();

            PlayerArtPatch.ForgetJaw();

            recording = false;
        }

        public void UpdateTotalTakeLength() {
            float totalSceneLengthFound = 0f;

            foreach (ObjectRecorder recorder in playbackRecorders)
            {
                // Only these ones should affect the total take time.
                if (recorder is MarrowEntityRecorder || recorder is RigmanagerRecorder) {
                    if (recorder.takeLength > totalSceneLengthFound)
                    {
                        totalSceneLengthFound = recorder.takeLength;
                    }
                }
            }

            totalSceneLength = totalSceneLengthFound;

            ClearRecordersOutOfBounds();
        }

        private void ClearRecordersOutOfBounds() {
            // Out of bounds of play length
            playbackRecorders.RemoveAll(x => x.initTime > totalSceneLength);
        }

        public void TriggerSaveCallback() {
            foreach (var objRecorder in playbackRecorders) {
                objRecorder.OnSave();
            }
        }

        public override int GetSize()
        {
            int accumulation = 0;

            accumulation += sizeof(int);

            foreach (var playbackRecorder in playbackRecorders) {
                accumulation += sizeof(byte);
                accumulation += playbackRecorder.GetSize();
            }

            return accumulation;
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteInt32(playbackRecorders.Count);

            foreach (var playbackRecorder in playbackRecorders) {
                stream.WriteByte(playbackRecorder.SerializeableID);
                stream.WriteSerializableMember(playbackRecorder);
            }
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            MarrowEntityRecorder.ClearAllEvents();
            lastRecorderID = 1;
            playbackRecorders.Clear();
            objectRecorderIDMap.Clear();

            int playbackRecordersCount = stream.ReadInt32();

            for (int i = 0; i < playbackRecordersCount; i++) {
                byte ID = stream.ReadByte();

                SerializableRegistry.AttemptGetRecorderFromType(ID, out var playbackType);

                ObjectRecorder objectRecorder = (ObjectRecorder) stream.ReadSerializableMember(playbackType);
                if (lastRecorderID <= objectRecorder.recorderID) {
                    lastRecorderID = (ushort) (objectRecorder.recorderID + 1);
                }

                objectRecorderIDMap.Add(objectRecorder.recorderID, objectRecorder);

                playbackRecorders.Add(objectRecorder);
            }

            UpdateTotalTakeLength();
        }
    }
}
