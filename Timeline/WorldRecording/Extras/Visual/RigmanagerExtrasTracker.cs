using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Interaction;
using Il2CppSLZ.Marrow;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Logging;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.Settings;
using Timeline.WorldRecording.Events.BuiltIn;
using Timeline.WorldRecording.Recorders;
using Timeline.WorldRecording.StateCapturers;
using UnityEngine;

namespace Timeline.WorldRecording.Extras.Visual
{
    public class RigmanagerExtrasTracker : SerializableMember
    {
        public override byte SerializeableID => (byte) TimelineSerializedTypes.RIGMANAGER_EXTRAS;

        private GameObjectTransformCapturer rightHolster = new GameObjectTransformCapturer();
        private GameObjectTransformCapturer leftHolster = new GameObjectTransformCapturer();
        private GameObjectTransformCapturer ammoPouch = new GameObjectTransformCapturer();
        private GameObjectTransformCapturer bodyLogBase = new GameObjectTransformCapturer();
        private GameObjectTransformCapturer bodyLogBall = new GameObjectTransformCapturer();
        RigmanagerRecorder recorder;

        private GameObject spawnedBodyLogSphere;
        private GameObject bodyLogLine;
        private Renderer lineRenderer;

        private bool ballPrevActiveState = false;

        private bool initialized = false;

        private List<GameObject> spawnedObjects = new List<GameObject>();

        public void InitOnLocalRig(RigmanagerRecorder recorder) {
            this.recorder = recorder;
            rightHolster.UpdateTargetObject(RuntimeCapturedAssets.rightHolster);
            leftHolster.UpdateTargetObject(RuntimeCapturedAssets.leftHolster);
            ammoPouch.UpdateTargetObject(RuntimeCapturedAssets.ammoPouch);
            bodyLogBase.UpdateTargetObject(RuntimeCapturedAssets.bodyLog);
            bodyLogBall.UpdateTargetObject(RuntimeCapturedAssets.bodyLog.transform.Find("spheregrip/Sphere").gameObject);

            spawnedBodyLogSphere = RuntimeCapturedAssets.bodyLog.transform.Find("spheregrip/Sphere/Art").gameObject;
        }

        public void InitializePlayback() {
            GameObject emptySpawner = new GameObject("D_PARENT");
            emptySpawner.SetActive(false);

            GameObject spawnedRightHolster = GameObject.Instantiate(RuntimeCapturedAssets.rightHolster, emptySpawner.transform);
            GameObject spawnedLeftHolster = GameObject.Instantiate(RuntimeCapturedAssets.leftHolster, emptySpawner.transform);
            GameObject spawnedAmmoPouch = GameObject.Instantiate(RuntimeCapturedAssets.ammoPouch, emptySpawner.transform);


            GameObject spawnedBodyLog = GameObject.Instantiate(RuntimeCapturedAssets.bodyLog, emptySpawner.transform);
            spawnedBodyLog.SetActive(true);

            spawnedObjects.Add(spawnedRightHolster);
            spawnedObjects.Add(spawnedLeftHolster);
            spawnedObjects.Add(spawnedAmmoPouch);
            spawnedObjects.Add(spawnedBodyLog);

            rightHolster.UpdateTargetObject(spawnedRightHolster);
            leftHolster.UpdateTargetObject(spawnedLeftHolster);
            ammoPouch.UpdateTargetObject(spawnedAmmoPouch);

            GameObject physicsSphere = spawnedBodyLog.transform.Find("spheregrip").gameObject;
            physicsSphere.GetComponent<Rigidbody>().isKinematic = true;

            spawnedBodyLogSphere = physicsSphere.transform.Find("Sphere/Art").gameObject;
            bodyLogLine = spawnedBodyLog.transform.Find("VFX/LineElement").gameObject;
            lineRenderer = bodyLogLine.GetComponent<LineRenderer>();

            
            

            bodyLogBase.UpdateTargetObject(spawnedBodyLog);
            bodyLogBall.UpdateTargetObject(physicsSphere);

            ToggleBodyLogBall(false);

            foreach (var spawned in spawnedObjects) {
                ClearUnnecessaryComponents(spawned);
                spawned.transform.parent = null;
            }

            GameObject.Destroy(emptySpawner);

            SetVisibilityToSettingVisibility();

            initialized = true;
        }

        public void SetVisibilityToSettingVisibility() {
            UpdateActiveHolstersAndPouch(!GlobalSettings.hidePlaybackPouches);
            UpdateActiveBodylog(!GlobalSettings.hidePlaybackBodylog);
        } 

        private void ClearUnnecessaryComponents(GameObject gameObject) {
            foreach (var pullCord in gameObject.GetComponentsInChildren<PullCordDevice>()) {
                GameObject.DestroyImmediate(pullCord);
            }

            foreach (var inventoryReceiver in gameObject.GetComponentsInChildren<InventoryAmmoReceiver>())
            {
                GameObject.DestroyImmediate(inventoryReceiver);
            }

            foreach (var interIcon in gameObject.GetComponentsInChildren<InteractableIcon>())
            {
                GameObject.DestroyImmediate(interIcon);
            }

            Transform transform = gameObject.transform;

            // Object in holster
            AttemptClearAllChildren(transform, "ItemReciever");

            // Art targets (Should be empty on spawned copies!)
            AttemptClearAllChildren(transform, "InventoryAmmoReceiver/Holder/ArtTarget01");
            AttemptClearAllChildren(transform, "InventoryAmmoReceiver/Holder/ArtTarget02");
            AttemptClearAllChildren(transform, "InventoryAmmoReceiver/Holder/ArtTarget03");
            AttemptClearAllChildren(transform, "InventoryAmmoReceiver/Holder/ArtTarget04");
        }

        public void UpdateActiveHolstersAndPouch(bool active) {
            if (!initialized)
            {
                return;
            }
            GameObject leftHolsterGO = leftHolster.targetTransform.gameObject;
            GameObject rightHolsterGO = rightHolster.targetTransform.gameObject;
            GameObject ammoPouchGO = ammoPouch.targetTransform.gameObject;

            leftHolsterGO.SetActive(active);
            rightHolsterGO.SetActive(active);
            ammoPouchGO.SetActive(active);
        }

        public void UpdateActiveBodylog(bool active) {
            if (!initialized)
            {
                return;
            }

            GameObject bodyLogGO = bodyLogBase.targetTransform.gameObject;

            bodyLogGO.SetActive(active);
        }

        private void AttemptClearAllChildren(Transform root, string path) {
            Transform foundTransform = root.Find(path);
            if (foundTransform)
            {
                int childCount = foundTransform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    GameObject.DestroyImmediate(foundTransform.GetChild(i).gameObject);
                }
            }
        }

        public void Cleanup() {
            foreach (var GO in spawnedObjects) {
                GameObject.Destroy(GO);
            }

            spawnedObjects.Clear();

            initialized = false;
        }

        public void ToggleBodyLogBall(bool enabled) {
            spawnedBodyLogSphere.SetActive(enabled);
            bodyLogLine.SetActive(enabled);
        }

        public void ClearAllObjectTargetReferences() {
            rightHolster.UpdateTargetObject(null);
            leftHolster.UpdateTargetObject(null);
            ammoPouch.UpdateTargetObject(null);
            bodyLogBase.UpdateTargetObject(null);
            bodyLogBall.UpdateTargetObject(null);
        }

        public void Playback(float sceneTime) {
            rightHolster.ApplyToObject(sceneTime);
            leftHolster.ApplyToObject(sceneTime);
            ammoPouch.ApplyToObject(sceneTime);
            bodyLogBase.ApplyToObject(sceneTime);
            bodyLogBall.ApplyToObject(sceneTime);

            // This fixes the line renderer bounds being off (Causing the line to not render if the bounds were not registered as being looked at). I don't know why this is happening, but something changes the bounds
            // Of the bodylog line on the local rig. So we gotta do it too.
            if (lineRenderer) {
                Bounds bounds = new Bounds(bodyLogLine.transform.position, new Vector3(40, 40, 40));
                lineRenderer.bounds = bounds;
            }
        }

        public void Capture(float sceneTime) {
            rightHolster.Capture(sceneTime);
            leftHolster.Capture(sceneTime);
            ammoPouch.Capture(sceneTime);
            bodyLogBase.Capture(sceneTime);
            bodyLogBall.Capture(sceneTime);

            if (spawnedBodyLogSphere) {
                if (ballPrevActiveState != spawnedBodyLogSphere.active)
                {
                    // Add event!!!
                    recorder.AddEvent(sceneTime, new BodyLogToggleEvent(spawnedBodyLogSphere.active));
                }

                ballPrevActiveState = spawnedBodyLogSphere.active;
            }
        }

        public override int GetSize()
        {
            int size = 0;
            size += rightHolster.GetSize();
            size += leftHolster.GetSize();
            size += ammoPouch.GetSize();
            size += bodyLogBase.GetSize();
            size += bodyLogBall.GetSize();

            return size;
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            rightHolster = stream.ReadSerializableMember<GameObjectTransformCapturer>();
            leftHolster = stream.ReadSerializableMember<GameObjectTransformCapturer>();
            ammoPouch = stream.ReadSerializableMember<GameObjectTransformCapturer>();
            bodyLogBase = stream.ReadSerializableMember<GameObjectTransformCapturer>();
            bodyLogBall = stream.ReadSerializableMember<GameObjectTransformCapturer>();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteSerializableMember(rightHolster);
            stream.WriteSerializableMember(leftHolster);
            stream.WriteSerializableMember(ammoPouch);
            stream.WriteSerializableMember(bodyLogBase);
            stream.WriteSerializableMember(bodyLogBall);
        }

        public void SetPositionalAnchor(GameObject anchor)
        {
            rightHolster.SetAnchor(anchor);
            leftHolster.SetAnchor(anchor);
            ammoPouch.SetAnchor(anchor);
            bodyLogBase.SetAnchor(anchor);
            bodyLogBall.SetAnchor(anchor);
        }
    }
}
