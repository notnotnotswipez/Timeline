using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Recorders;
using Timeline.WorldRecording.Utils;
using Timeline.WorldRecording;
using MelonLoader;
using Timeline.Settings;
using UnityEngine;
using Il2CppSystem.Collections;
using Il2CppSLZ.Marrow.Utilities;
using Timeline.Logging;
using static UnityEngine.Rendering.Universal.LibTessDotNet.MeshUtils;
using System.Collections;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Zones;

namespace Timeline.Patches.PoolPatch
{
    [HarmonyPatch(typeof(MarrowEntity), nameof(MarrowEntity.Awake))]
    public class MarrowEntityAwakePatch {
        public static Dictionary<int, float> marrowEntitiesInitTime = new Dictionary<int, float>();

        public static void Prefix(MarrowEntity __instance) {
            if (marrowEntitiesInitTime.ContainsKey(__instance.GetInstanceID())) {
                return;
            }

            marrowEntitiesInitTime.Add(__instance.GetInstanceID(), Time.realtimeSinceStartup);
        }

        public static bool IsBeforeRecorderStarted(MarrowEntity entity) {
            if (!entity) {
                return false;
            }
            if (marrowEntitiesInitTime.ContainsKey(entity.GetInstanceID())) {
                float marrowStartTime = marrowEntitiesInitTime[entity.GetInstanceID()];

                return marrowStartTime < WorldPlayer.latestRecordTimeSinceStartup;
            }

            return false;
        }

        public static void ClearMarrowEntities() {
            marrowEntitiesInitTime.Clear();
        }
    }

    [HarmonyPatch(typeof(MarrowEntity), nameof(MarrowEntity.OnCullResolve))]
    public class MarrowEntityDisablePatch
    {
        public static void Postfix(MarrowEntity __instance, InactiveStatus status, bool isInactive)
        {
            if (status.IsCulled())
            {
                if (MarrowEntityAwakePatch.marrowEntitiesInitTime.ContainsKey(__instance.GetInstanceID()))
                {
                    MarrowEntityAwakePatch.marrowEntitiesInitTime.Remove(__instance.GetInstanceID());
                }
            }
            else {
                if (!MarrowEntityAwakePatch.marrowEntitiesInitTime.ContainsKey(__instance.GetInstanceID()))
                {
                    MarrowEntityAwakePatch.marrowEntitiesInitTime.Add(__instance.GetInstanceID(), Time.realtimeSinceStartup);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Poolee), nameof(Poolee.OnEnable))]
    public class PooleeSpawnPatch
    {
        public static void Postfix(Poolee __instance) {

            if (WorldPlayer.recording && __instance.SpawnableCrate != null)
            {
                MelonCoroutines.Start(WaitAndSpawnPoolee(__instance));
            }
        }

        // Some things spawn under the rig and must be waited for
        private static System.Collections.IEnumerator WaitAndSpawnPoolee(Poolee __instance) {
            for (int i = 0; i < 20; i++) {
                yield return null;
            }

            if (IsBlacklisted(__instance.SpawnableCrate.Barcode.ID)) {
                yield break;
            }

            bool underPhysRig = __instance.GetComponentInParent<PhysicsRig>();

            // This is under the physics rig. Its probably ammo from the ammo belt. We only want that to spawn if we actually have
            // a rig being recorded. If not, the ammo would float when the player does not want anything signifying a rig was there.
            if (underPhysRig && WorldPlayer.currentRigmanagerRecorder == null)
            {
                yield break;
            }

            // This makes a new marrow entity recorder if it does not exist, and auto starts recording.
            MarrowEntityRecorder marrowEntityRecorder = RecordingUtils.GetMarrowEntityRecorderFromGameObject<MarrowEntityRecorder>(__instance.gameObject, true);

            // It couldn't make a recorder, because there was no MarrowEntity.
            if (marrowEntityRecorder == null)
            {
                // Simple spawn recorder!
                RegisterSimpleSpawn(__instance, __instance.SpawnableCrate.Barcode.ID, __instance.transform);
                yield break;
            }

            if (!MarrowEntityAwakePatch.IsBeforeRecorderStarted(marrowEntityRecorder.recordedEntity) || __instance.SpawnableCrate.Barcode.ID.Contains("fragment", StringComparison.CurrentCultureIgnoreCase)) {
                // Has no init time, so we set it
                if (marrowEntityRecorder.initTime < 1)
                {
                    marrowEntityRecorder.initTime = WorldPlayer.playHead;
                    TimelineLogger.Debug("Made POOLEE spawn recorder! FOR: " + __instance.SpawnableCrate.Barcode.ID + " at init time: " + marrowEntityRecorder.initTime);
                }
            }

            if (underPhysRig) {
                if (WorldPlayer.currentRigmanagerRecorder != null) {
                    marrowEntityRecorder.PinToAvailableActorBone(WorldPlayer.currentRigmanagerRecorder, HumanBodyBones.Hips);
                }
            }

            yield break;
        }

        private static void RegisterSimpleSpawn(Poolee poolee, string barcode, Transform transform) {
            // We definitely do not want to spawn in projectiles, its all an illusion after all.
            if (poolee.GetComponentInChildren<Projectile>()) {
                return;
            }

            TimelineLogger.Debug("Recorded SIMPLE SPAWN! " + barcode);

            SimpleSpawnRecorder simpleSpawnRecorder = new SimpleSpawnRecorder();
            simpleSpawnRecorder.barcode = barcode;
            simpleSpawnRecorder.position = transform.position;
            simpleSpawnRecorder.rotation = transform.rotation;
            simpleSpawnRecorder.scale = transform.localScale;

            if (WorldPlayer.currentRigmanagerRecorder != null) {
                simpleSpawnRecorder.associatedRecorders.Add(WorldPlayer.currentRigmanagerRecorder.recorderID);
            }
            
            ParticleSystem attemptedFoundSystem = poolee.GetComponentInChildren<ParticleSystem>();

            if (attemptedFoundSystem) {
                MelonCoroutines.Start(WaitAndSetParticleColor(simpleSpawnRecorder, attemptedFoundSystem));
            }

            simpleSpawnRecorder.initTime = WorldPlayer.playHead;

            TimelineMainClass.timelineHolder.worldPlayer.AddRecorderToRecord(simpleSpawnRecorder);

            
        }

        private static System.Collections.IEnumerator WaitAndSetParticleColor(SimpleSpawnRecorder simpleSpawnRecorder, ParticleSystem system) {
            for (int i = 0; i < 20; i++) {
                yield return null;
            }

            simpleSpawnRecorder.color = system.startColor;

            yield break;
        }

        private static bool IsBlacklisted(string barcode) {
            bool blacklisted = false;

            // This is some audio thing. Unnecessary.
            if (barcode == "c1534c5a-03e2-409b-a089-127541756469") {
                blacklisted = true;
            }

            // Spawn gun UI thing, we don't need it.
            if (barcode == "SLZ.BONELAB.Content.Spawnable.SpawnGunUI")
            {
                blacklisted = true;
            }

            // Obvious reasons
            if (barcode == MarrowSettings.RuntimeInstance.DefaultPlayerRig.Barcode.ID) {
                blacklisted = true;
            }

            

            return blacklisted;
        }
    }
}
