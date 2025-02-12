using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.WorldRecording.StateCapturers;
using UnityEngine;

namespace Timeline.WorldRecording.Recorders
{
    public class SimpleSpawnRecorder : ObjectRecorder
    {
        public override byte SerializeableID => (byte) TimelineSerializedTypes.SIMPLE_SPAWN_RECORDER;

        public string barcode;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale = Vector3.one;
        public Color color;

        public GameObject recentlySpawned;

        public override void Capture(float sceneTime)
        {
            
        }

        public override string GetName()
        {
            return "IGNORE";
        }

        public override void OnInitializedPlayback()
        {
            Barcode barcodeObj = new Barcode(barcode);

            // We don't have the spawnable, so its probably a save file
            if (!AssetWarehouse.Instance._crateRegistry.ContainsKey(barcodeObj))
            {
                return;
            }

            // SPAWN OBJECT
            SpawnableCrate spawnableCrate = AssetWarehouse.Instance._crateRegistry[barcodeObj].TryCast<SpawnableCrate>();

            System.Action<GameObject> action = new System.Action<GameObject>(o =>
            {

                GameObject go = GameObject.Instantiate(o, position, rotation);

                recentlySpawned = go;

                targetObject = recentlySpawned;
                AttemptModifyParticleSystem(targetObject);

                Poolee poolee = recentlySpawned.GetComponentInChildren<Poolee>();
                if (poolee) {
                    poolee.SpawnableCrate = null;
                    poolee.transform.localScale = scale;
                }

                
            });

            spawnableCrate.LoadAsset(action);
        }

        private void AttemptModifyParticleSystem(GameObject root) {
            foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>()) {
                particleSystem.startColor = color;

                // Bodylog particles, so our "associated recorder" should be a rigmanager recorder in the first space
                if (barcode == "SLZ.BONELAB.Content.Spawnable.TransformVFX") {
                    if (associatedRecorders.Count > 0)
                    {
                        ObjectRecorder firstRecorder = WorldPlayer.Instance.GetObjectRecorder(associatedRecorders[0]);
                        if (firstRecorder != null)
                        {
                            RigmanagerRecorder rigmanagerRecorder = (RigmanagerRecorder) firstRecorder;
                            if (rigmanagerRecorder.currentPlaybackAvatar)
                            {
                                int maxSize = rigmanagerRecorder.currentPlaybackAvatar.bodyMeshes.Length;
                                particleSystem.shape.skinnedMeshRenderer = rigmanagerRecorder.currentPlaybackAvatar.bodyMeshes[UnityEngine.Random.RandomRangeInt(0, maxSize)];
                            }
                        }
                    }
                }
            }

            ParticleTint tint = root.GetComponentInChildren<ParticleTint>();

            if (tint) {
                tint.TintParticles(color);
            }
        }

        public override void OnInitializedRecording(UnityEngine.Object rootObject)
        {
            
        }

        public override void OnPlaybackCompleted()
        {
            if (recentlySpawned) {
                GameObject.DestroyImmediate(recentlySpawned);
            }
        }

        public override void OnRecordingCompleted()
        {
            
        }

        public override void Playback(float sceneTime)
        {
            
        }

        public override int GetSize()
        {
            int originalSize = base.GetSize();
            originalSize += BinaryStream.GetStringLength(barcode);
            originalSize += sizeof(float) * 3;
            originalSize += sizeof(float) * 4;
            originalSize += sizeof(float) * 3;
            originalSize += sizeof(float) * 3;

            return originalSize;
        }

        public override void WriteToStream(BinaryStream stream)
        {
            base.WriteToStream(stream);

            stream.WriteString(barcode);
            stream.WriteVector3(position);
            stream.WriteQuaternion(rotation);
            stream.WriteSingle(color.r);
            stream.WriteSingle(color.g);
            stream.WriteSingle(color.b);
            stream.WriteVector3(scale);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            base.ReadFromStream(stream);

            barcode = stream.ReadString();
            position = stream.ReadVector3();
            rotation = stream.ReadQuaternion();

            color = new Color(stream.ReadSingle(), stream.ReadSingle(), stream.ReadSingle());

            scale = stream.ReadVector3();
        }
    }
}
