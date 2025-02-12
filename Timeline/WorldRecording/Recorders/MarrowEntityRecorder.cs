using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Redacted;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Marrow.Zones;
using Il2CppSystem;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Logging;
using Timeline.Patches.PoolPatch;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.Extensions;
using Timeline.WorldRecording.Extras;
using Timeline.WorldRecording.Extras.Impl;
using Timeline.WorldRecording.StateCapturers;
using Timeline.WorldRecording.Utils;
using UnityEngine;

namespace Timeline.WorldRecording.Recorders
{
    public class MarrowEntityRecorder : ObjectRecorder, OverridableRecording
    {

        internal List<RigidbodyCapturer> capturers = new List<RigidbodyCapturer>();
        private GameObjectTransformCapturer root;
        public MarrowEntity recordedEntity;
        private Poolee poolee;
        private string barcode = "NO.BARCODE";

        private Poolee playbackPooleee;

        public GameObject playbackObject;
        public MarrowEntity playbackEntity;
        public MarrowEntityRecorder lastEntityRecorderParent;
        public RigmanagerRecorder lastRigmanagerRecorderParent;

        public bool hasPin = false;
        public bool useWorldEntity = false;

        int previousInstanceIdOfEntity = -1;

        public Vector3 scale = Vector3.one;
        public Transform lastParent;

        // Key: MarrowEntity InstanceID!
        private static Dictionary<int, MarrowEntityRecorder> recorderCache = new Dictionary<int, MarrowEntityRecorder>();

        public override byte SerializeableID => (byte) TimelineSerializedTypes.MARROW_ENTITY_RECORDER;

        bool previouslyActive = true;

        private Dictionary<System.Type, ComponentManager> componentManagers = new Dictionary<System.Type, ComponentManager>();

        public static System.Action<MarrowEntity> OnPlaybackSpawnedEvent;
        public static System.Action<MarrowEntity, bool> OnPlaybackObjectInteractableChangeEvent;

        public System.Action<MarrowEntityRecorder> OnBeforeDestroy;

        public static void ClearAllEvents() {
            OnPlaybackSpawnedEvent = null;
            OnPlaybackObjectInteractableChangeEvent = null;
        }

        public MarrowEntityRecorder() {
            RegisterAllComponentManagers();
        }

        private void RegisterAllComponentManagers() {
            RegisterComponentManager<SimpleGripEventsComponentManager>();
            RegisterComponentManager<ObjectDestructibleComponentManager>();
            RegisterComponentManager<BehaviorBaseNavComponentManager>();
            RegisterComponentManager<SLZLaserPointerComponentManager>();
        }

        private void RegisterComponentManager<T>() where T : ComponentManager {
            ComponentManager componentManager = (ComponentManager) System.Activator.CreateInstance(typeof(T));
            componentManagers.Add(typeof(T), componentManager);
        }

        public T FetchComponentManager<T>() where T : ComponentManager {
            if (componentManagers.ContainsKey(typeof(T))) {
                return (T) componentManagers[typeof(T)];
            }
            return null;
        }

        public static MarrowEntityRecorder TryGetRecorderFromCache(MarrowEntity entity) {
            if (recorderCache.ContainsKey(entity.GetInstanceID())) {
                return recorderCache[entity.GetInstanceID()];
            }

            return null;
        }

        public override string GetName()
        {
            if (recordedEntity) {
                return recordedEntity.gameObject.name;
            }
            return "DEFAULT";
        }

        public override void OnInitializedPlayback()
        {
            if (barcode != "NO.BARCODE")
            {
                TimelineLogger.Debug("Spawning playback object for: " + barcode + "at init time: "+initTime);

                Barcode barcodeObj = new Barcode(barcode);

                // We don't have the spawnable, so its probably a save file
                if (!AssetWarehouse.Instance._crateRegistry.ContainsKey(barcodeObj))
                {
                    return;
                }

                // SPAWN ITEM
                SpawnableCrate spawnableCrate = AssetWarehouse.Instance._crateRegistry[barcodeObj].TryCast<SpawnableCrate>();

                System.Action<GameObject> action = new System.Action<GameObject>(o =>
                {
                    Vector3 spawnPos = Vector3.zero;
                    Quaternion spawnRot = Quaternion.identity;

                    if (capturers.Count > 0) {
                        spawnPos = capturers[0].GetFirstAvailablePosition();
                        spawnRot = capturers[0].GetFirstAvailableRotation();
                    }

                    GameObject go = GameObject.Instantiate(o, spawnPos, spawnRot);
                    
                    TimelineLogger.Debug("Playback spawned!");
                    
                    OnPlaybackSpawned(go);
                });

                spawnableCrate.LoadAsset(action);
            }
            else {
                if (recordedEntity) {
                    // Scene object, or something similar with no barcode. So our "playback" is what we recorded.
                    OnPlaybackSpawned(recordedEntity.gameObject);
                }
            }
        }

        public virtual void OnPlaybackSpawned(GameObject go) {

            SetAllCaptureAnchors(null);

            MarrowEntity marrowEntity = go.GetComponentInChildren<MarrowEntity>();
            marrowEntity.transform.localScale = scale;

            targetObject = marrowEntity.gameObject;

            int index = 0;

            foreach (var marrowBody in marrowEntity.Bodies)
            {
                if (marrowBody._rigidbody) {
                    // Bodies list is in the same order everytime.
                    capturers[index].UpdateTargetObject(marrowBody.gameObject);
                    index++;

                    marrowBody._rigidbody.isKinematic = true;
                }
            }

            root.UpdateTargetObject(marrowEntity.gameObject);

            playbackPooleee = marrowEntity._poolee;

            if (playbackPooleee) {
                playbackPooleee.SpawnableCrate = null;
            }

            playbackEntity = marrowEntity;

            // We don't want the playback to cull as it will cause issues regarding "ownership" transfer when overriding a recorder.
            // So we just don't disable it on cull.
            playbackEntity.PreventDisableOnCull();

            playbackObject = go;

            // Would only be inactive if we are reusing the object and it was previously disabled.
            playbackObject.SetActive(true);
            previouslyActive = true;
            UpdateEntityCache();
            DisableProblematicComponents(go);

            OnComponentManagersPlaybackSet();

            OnPlaybackSpawnedEvent?.Invoke(playbackEntity);

            Playback(initTime+0.1f);

        }

        public void PinToAvailableActorBone(ushort recorderID, HumanBodyBones bone) {
            RigmanagerRecorder rigmanagerRecorder = (RigmanagerRecorder) WorldPlayer.Instance.GetObjectRecorder(recorderID);
            PinToAvailableActorBone(rigmanagerRecorder, bone);
        }

        public void PinToAvailableActorBone(RigmanagerRecorder rigRecorder, HumanBodyBones bone)
        {
            if (recording)
            {
                AddEvent(WorldPlayer.playHead+0.01f, new MarrowEntityRecorderPinEvent(rigRecorder.recorderID, bone));
            }

            AttemptToClearRigPinReference();

            lastRigmanagerRecorderParent = rigRecorder;
            lastRigmanagerRecorderParent.marrowEntityPinAssociators.Add(new System.Tuple<MarrowEntityRecorder, HumanBodyBones>(this, bone));

            if (lastEntityRecorderParent != null) {
                lastEntityRecorderParent.associatedRecorders.Remove(recorderID);
                lastEntityRecorderParent = null;

                if (!recordedEntity)
                {
                    if (playbackEntity)
                    {
                        playbackEntity.transform.parent = null;

                        foreach (var col in playbackEntity.GetComponentsInChildren<Grip>())
                        {
                            col.enabled = true;
                        }

                        foreach (var col in playbackEntity.GetComponentsInChildren<Collider>())
                        {
                            col.enabled = true;
                        }
                    }
                }
            }

            SetAllCaptureAnchors(rigRecorder.GetBoneTransform(bone));
        }

        private void AttemptToClearRigPinReference()
        {
            if (lastRigmanagerRecorderParent != null)
            {
                lastRigmanagerRecorderParent.marrowEntityPinAssociators.RemoveAll((x) => x.Item1 == this);
                lastRigmanagerRecorderParent = null;
            }
        }

        public void PinToEntityRecorder(ushort recorderID) {
            MarrowEntityRecorder secondEntityRecorder = (MarrowEntityRecorder) WorldPlayer.Instance.GetObjectRecorder(recorderID);
            PinToEntityRecorder(secondEntityRecorder);
        }

        public void PinToEntityRecorder(MarrowEntityRecorder recorder)
        {
            if (lastEntityRecorderParent != null) {
                lastEntityRecorderParent.associatedRecorders.Remove(recorderID);
            }

            AttemptToClearRigPinReference();

            if (recording)
            {
                AddEvent(WorldPlayer.playHead, new MarrowEntityRecorderPinEvent(recorder.recorderID));
            }

            if (recorder.playbackEntity)
            {
                SetAllCaptureAnchors(recorder.playbackEntity.Bodies[0].transform);

                if (!recordedEntity)
                {
                    if (playbackEntity)
                    {
                        foreach (var col in playbackEntity.GetComponentsInChildren<Grip>())
                        {
                            col.enabled = false;
                        }

                        foreach (var col in playbackEntity.GetComponentsInChildren<Collider>())
                        {
                            col.enabled = false;
                        }
                    }
                }
            }

            lastEntityRecorderParent = recorder;
            recorder.associatedRecorders.Add(recorderID);
        }

        public void SetAllCaptureAnchors(Transform transform, bool clearRef = true) {
            if (transform == null)
            {
                if (clearRef) {
                    AttemptToClearRigPinReference();
                }
               

                hasPin = false;

                if (!recordedEntity) {
                    if (playbackEntity)
                    {
                        playbackEntity.transform.parent = null;

                        foreach (var col in playbackEntity.GetComponentsInChildren<Grip>())
                        {
                            col.enabled = true;
                        }

                        foreach (var col in playbackEntity.GetComponentsInChildren<Collider>())
                        {
                            col.enabled = true;
                        }
                    }
                }

                if (lastEntityRecorderParent != null) {

                    WorldPlayer.Instance.QueueEndOfLoopAction(() =>
                    {
                        if (lastEntityRecorderParent != null) {
                            lastEntityRecorderParent.associatedRecorders.Remove(recorderID);
                            lastEntityRecorderParent = null;
                        }
                    });
                }
            }
            else {
                hasPin = true;

                if (!recordedEntity) {
                    if (playbackEntity)
                    {
                        playbackEntity.transform.parent = transform;
                    }
                }
            }

            foreach (var capturer in capturers)
            {
                capturer.SetAnchor(transform);
            }

            root.SetAnchor(transform);
        }

        public void ClearPins() {
            if (recording) {
                AddEvent(WorldPlayer.playHead, new MarrowEntityRecorderPinEvent(false));
            }
            SetAllCaptureAnchors(null);
        }

        private void DisableProblematicComponents(GameObject go) {
            foreach (var plug in go.GetComponentsInChildren<Il2CppSLZ.Marrow.Plug>()) {
                TimelineLogger.Debug("Found ammo plug!");
                if (plug.enabled) {
                    plug.gameObject.name += "(tl_enabled)";
                    plug.enabled = false;

                    plug.GetComponent<SphereCollider>().enabled = false;
                }
            }

            foreach (var socket in go.GetComponentsInChildren<Il2CppSLZ.Marrow.Socket>())
            {
                TimelineLogger.Debug("Found ammo socket!");
                if (socket.enabled)
                {
                    socket.gameObject.name += "(tl_enabled)";
                    socket.enabled = false;

                    socket.GetComponent<SphereCollider>().enabled = false;

                }
            }
        }

        private void EnableProblematicComponents(GameObject go) {

            foreach (var plug in go.GetComponentsInChildren<Il2CppSLZ.Marrow.Plug>())
            {
                if (!plug.enabled && plug.gameObject.name.Contains("(tl_enabled)"))
                {
                    plug.gameObject.name.Replace("(tl_enabled)", "");
                    plug.enabled = true;

                    plug.GetComponent<SphereCollider>().enabled = true;
                }
            }

            foreach (var socket in go.GetComponentsInChildren<Il2CppSLZ.Marrow.Socket>())
            {
                if (!socket.enabled && socket.gameObject.name.Contains("(tl_enabled)"))
                {
                    socket.gameObject.name.Replace("(tl_enabled)", "");
                    socket.enabled = true;

                    socket.GetComponent<SphereCollider>().enabled = true;
                }
            }
        }

        public virtual void UpdateEntityCache() {
            recorderCache.Remove(previousInstanceIdOfEntity);

            if (recorderCache.ContainsKey(playbackEntity.GetInstanceID())) {
                return;
            }

            recorderCache.Add(playbackEntity.GetInstanceID(), this);
            previousInstanceIdOfEntity = playbackEntity.GetInstanceID();
        }

        public override void OnInitializedRecording(UnityEngine.Object rootObject)
        {
            
            capturers.Clear();
            MarrowEntity marrowEntity = rootObject.TryCast<MarrowEntity>();
            

            this.recordedEntity = marrowEntity;

            // We are technically "playing back", but we control the future. Haha.
            playbackEntity = recordedEntity;
            UpdateEntityCache();

            poolee = marrowEntity._poolee;

            // If UseWorldEntity is true, then that means the recorder wants to reuse the same entity in the world rather than spawning in a new one every playback.
            if (poolee && !useWorldEntity) {
                if (poolee.SpawnableCrate) {
                    TimelineLogger.Debug("Got spawnable crate for recorded entity!");
                    barcode = poolee.SpawnableCrate.Barcode.ID;
                }
            }
            

            foreach (var marrowBody in marrowEntity.Bodies) {
                if (marrowBody._rigidbody)
                {
                    RigidbodyCapturer rigidbodyCapturer = new RigidbodyCapturer(marrowBody._rigidbody);
                    capturers.Add(rigidbodyCapturer);
                }
                else {
                    // The rigidbody was destroyed (This is probably a purely visual variant of a spawnable, like the bullets in ammo magazines)
                    RigidbodyCapturer rigidbodyCapturer = new RigidbodyCapturer(marrowBody.gameObject);
                    capturers.Add(rigidbodyCapturer);
                }
            }

            root = new GameObjectTransformCapturer(marrowEntity.gameObject);


            AddEvent(0, new ObjectDisableEvent(true));
            AddEvent(0, new MarrowEntityRecorderPinEvent(false));

            OnRecordingEntitySet(recordedEntity.gameObject);
        }

        public override void OnPlaybackCompleted()
        {
            // Means it was spawned in.
            if (barcode != "NO.BARCODE")
            {
                OnBeforeDestroy?.Invoke(this);
                GameObject.DestroyImmediate(playbackObject);
            }
            else {

                // World object! Reset its positions to when it was initialized.
                if (playbackObject) {
                    Playback(initTime);
                }
            }
        }

        public override void Playback(float sceneTime)
        {
            // Playback
            ApplyTransformCapturers(sceneTime);

            foreach (var compManager in componentManagers)
            {
                compManager.Value.OnUpdate(true);
            }

        }

        public override void Capture(float sceneTime)
        {
            if (targetObject)
            {
                foreach (var capturer in capturers)
                {
                    capturer.Capture(sceneTime);
                }

                if (targetObject.activeInHierarchy != previouslyActive)
                {
                    TimelineLogger.Debug("Added object disable event: State: " + targetObject.activeInHierarchy);
                    AddEvent(WorldPlayer.playHead, new ObjectDisableEvent(targetObject.activeInHierarchy));
                }

                previouslyActive = targetObject.activeInHierarchy;

                Transform parentQuickAccess = targetObject.transform.parent;

                bool hasParentThisFrame = parentQuickAccess;
                bool hadParentLastFrame = lastParent;

                if (hadParentLastFrame)
                {
                    if (hasParentThisFrame)
                    {
                        // Parent changed
                        if (lastParent.GetInstanceID() != parentQuickAccess.GetInstanceID()) {
                            TryFindParentRecorderAndPinSelfToIt(parentQuickAccess.gameObject);
                        }
                    }
                    else
                    {
                        // We had a parent, now we don't
                        ClearPins();
                    }
                }
                else {
                    // We have a parent this frame while we didn't the last frame
                    if (hasParentThisFrame) {
                        TryFindParentRecorderAndPinSelfToIt(parentQuickAccess.gameObject);
                    }
                }


                lastParent = parentQuickAccess;
            }
            else
            {
                if (previouslyActive)
                {
                    // Got destroyed or something
                    AddEvent(WorldPlayer.playHead, new ObjectDisableEvent(false));
                    previouslyActive = false;
                }
            }

            foreach (var compManager in componentManagers) {
                compManager.Value.OnUpdate(false);
            }
        }

        private void TryFindParentRecorderAndPinSelfToIt(GameObject root) {
            MarrowEntityRecorder marrowEntityRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(root);

            if (marrowEntityRecorder != null)
            {
                PinToEntityRecorder(marrowEntityRecorder);
            }
            else {
                if (!root.GetComponentInParent<PhysicsRig>()) {
                    ClearPins();
                }
            }
        }

        public void ForceCapture(float sceneTime) {
            if (targetObject)
            {
                foreach (var capturer in capturers)
                {
                    capturer.Capture(sceneTime, true);
                }
            }
        }

        public virtual void OnRecordingEntitySet(GameObject go) {
            targetObject = go;
            OnComponentManagersRecorderSet();
            EnableProblematicComponents(go);
        }

        private void OnComponentManagersRecorderSet() {
            foreach (var compManager in componentManagers.Values) {
                compManager.Populate(this);

                if (compManager.valid) {
                    compManager.OnReceiveComponentsFromRecorder(compManager.GetAllComponentInstances());
                }
            }
        }

        private void OnComponentManagersPlaybackSet() {
            foreach (var compManager in componentManagers.Values)
            {
                compManager.Populate(this);

                if (compManager.valid)
                {
                    compManager.OnReceiveComponentsFromPlayback(compManager.GetAllComponentInstances());
                }
            }
        }

        private void OnComponentManagersRecordingCompleted() {
            foreach (var compManager in componentManagers.Values)
            {
                if (compManager.valid)
                {
                    compManager.OnRecorderCompleted(compManager.GetAllComponentInstances());
                }
            }
        }

        private void ApplyTransformCapturers(float sceneTime) {
            foreach (var capturer in capturers)
            {
                capturer.ApplyToObject(sceneTime);
            }

            root.ApplyToObject(sceneTime);
        }

        public override void OnRecordingCompleted()
        {
            ClearPins();
            OnComponentManagersRecordingCompleted();

            // If this is NOT true. This is a world object. Which means the target should PERSIST on the recorders completion.
            // This removes the thing we recorded after we are done recording it. Make this a setting eventually.
            if (barcode != "NO.BARCODE")
            {
                foreach (var capturer in capturers)
                {
                    capturer.UpdateTargetObject(null);
                }

                root.UpdateTargetObject(null);

                if (recordedEntity) {

                    recorderCache.Remove(recordedEntity.GetInstanceID());

                    if (recordedEntity.GetComponentInParent<PhysicsRig>())
                    {
                        // Hell no!!! This does not prevent the issue that you may think happens, the physicsrig is never recorded as a marrowentity.
                        // But rather, the ammo in the ammo pouches ARE marrowentities that are children of the physrig, and despawning those causes serious problems!
                        return;
                    }

                    if (recordedEntity._poolee) {
                        // We should despawn this because its going to be spawned in playback
                        recordedEntity._poolee.Despawn();
                    }
                }

                recordedEntity = null;
            }
        }

        public virtual void OnOverrideStart(float sceneTime)
        {
            foreach (var capturer in capturers)
            {
                capturer.ClearAllDataAfterTime(sceneTime);
            }

            root.ClearAllDataAfterTime(sceneTime);

            OnRecordingEntitySet(playbackEntity.gameObject);
            ModifyPlaybackObjectInteractable(true);

            ClearPins();

            foreach (var id in associatedRecorders) {
                ObjectRecorder entityRecorder = WorldPlayer.Instance.GetObjectRecorder(id);
                MarrowEntityRecorder marrowEntityRecorder = (MarrowEntityRecorder) entityRecorder;

                bool isPinned = false;
                Transform lastParent = null;
                Vector3 localPos = Vector3.one;
                Quaternion localRot = Quaternion.identity;

                if (marrowEntityRecorder.lastEntityRecorderParent != null)
                {
                    lastParent = marrowEntityRecorder.targetObject.transform.parent;
                    localPos = marrowEntityRecorder.targetObject.transform.localPosition;
                    localRot = marrowEntityRecorder.targetObject.transform.localRotation;
                    isPinned = true;
                }

                WorldPlayer.Instance.OverrideRecording(entityRecorder);

                if (marrowEntityRecorder.targetObject && isPinned)
                {
                    
                    marrowEntityRecorder.targetObject.transform.parent = lastParent;

                    WorldPlayer.Instance.QueueEndOfLoopAction(() =>
                    {
                        marrowEntityRecorder.targetObject.transform.localPosition = localPos;
                        marrowEntityRecorder.targetObject.transform.localRotation = localRot;
                        foreach (var col in marrowEntityRecorder.targetObject.GetComponentsInChildren<Grip>())
                        {
                            col.enabled = false;
                        }

                        foreach (var col in marrowEntityRecorder.targetObject.GetComponentsInChildren<Collider>())
                        {
                            col.enabled = false;
                            GameObject.Destroy(col);
                        }

                        foreach (var col in marrowEntityRecorder.targetObject.GetComponentsInChildren<Rigidbody>())
                        {
                            col.isKinematic = true;
                        }
                    });
                    
                }

            }
        }


        public virtual void ModifyPlaybackObjectInteractable(bool interactable) {
            if (playbackEntity) {

                OnPlaybackObjectInteractableChangeEvent?.Invoke(playbackEntity, interactable);

                if (interactable) {

                    // Add the components that allow for the object to record others on collide, but only if its interactable again.
                    RecordingUtils.TryAddRecorderComponentsToBodies(playbackEntity);
                }

                int index = 0;

                foreach (var marrowBody in playbackEntity.Bodies)
                {

                    bool target = capturers[index].isKinematic;

                    if (!interactable)
                    {
                        target = false;
                    }

                    if (marrowBody._rigidbody) {
                        marrowBody._rigidbody.isKinematic = target;
                    }
                    index++;
                }
            }
        }

        public override int GetSize()
        {
            int total = 0;

            int originalSize = base.GetSize();
            total += originalSize;

            int barcodeSize = BinaryStream.GetStringLength(barcode);
            total += barcodeSize;

            // Size of capture list
            total += sizeof(int);

            foreach (var capture in capturers)
            {
                total += capture.GetSize();
            }

            total += root.GetSize();

            total += sizeof(float) * 3;

            return total;
        }

        public override void WriteToStream(BinaryStream stream)
        {
            base.WriteToStream(stream);
            stream.WriteString(barcode);
            stream.WriteInt32(capturers.Count);

            foreach (var capturer in capturers)
            {
                stream.WriteSerializableMember(capturer);
            }

            stream.WriteSerializableMember(root);

            stream.WriteVector3(scale);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            base.ReadFromStream(stream);
            barcode = stream.ReadString();
            int capturerCount = stream.ReadInt32();

            for (int i = 0; i < capturerCount; i++)
            {
                RigidbodyCapturer capturer = (RigidbodyCapturer) stream.ReadSerializableMember<RigidbodyCapturer>();

                capturers.Add(capturer);
            }

            root = (GameObjectTransformCapturer) stream.ReadSerializableMember<GameObjectTransformCapturer>();

            scale = stream.ReadVector3();
        }
    }
}
