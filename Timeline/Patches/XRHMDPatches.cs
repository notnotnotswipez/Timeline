using HarmonyLib;
using Il2CppSLZ.Marrow.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timeline.Patches
{

    [HarmonyPatch(typeof(XRHMD), nameof(XRHMD.IsUserPresent), MethodType.Getter)]
    public class XRHMDUserPresentPatch
    {
        public static void Postfix(ref bool __result) {
            __result = true;
        }
    }
}
