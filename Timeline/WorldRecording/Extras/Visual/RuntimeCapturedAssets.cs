using BoneLib;
using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Timeline.WorldRecording.Extras.Visual
{
    public class RuntimeCapturedAssets
    {
        public static GameObject bodyLog;
        public static GameObject rightHolster;
        public static GameObject leftHolster;
        public static GameObject ammoPouch;

        public static void CaptureAssets() {
            RigManager sceneRigmanager = Player.RigManager;
            leftHolster = sceneRigmanager.transform.Find("PhysicsRig/Spine/SideLf/prop_handGunHolster").gameObject;
            rightHolster = sceneRigmanager.transform.Find("PhysicsRig/Spine/SideRt/prop_handGunHolster").gameObject;

            // Do not let the name alarm you, both right and left variants of the belt are under this object!
            ammoPouch = sceneRigmanager.transform.Find("PhysicsRig/Pelvis/BeltLf1").gameObject;

            bodyLog = sceneRigmanager.GetComponentInChildren<PullCordDevice>().gameObject;
        }
    }
}
