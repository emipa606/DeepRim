using HarmonyLib;
using Verse;

namespace DeepRim;

[HarmonyPatch(typeof(CompHeatPusherPowered), "ShouldPushHeatNow", MethodType.Getter)]
public static class CompHeatPusherPowered_ShouldPushHeatNow
{
    private static void Postfix(CompHeatPusherPowered __instance, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        if (__instance.parent is not Building_MiningShaft shaft)
        {
            return;
        }

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            __result = false;
            return;
        }

        if (shaft.CurMode == 1)
        {
            return;
        }

        __result = false;
    }
}