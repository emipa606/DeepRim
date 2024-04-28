using HarmonyLib;
using Verse;

namespace DeepRim;

[HarmonyPatch(typeof(Thing), nameof(Thing.MarketValue), MethodType.Getter)]
public static class Thing_MarketValue
{
    private static void Postfix(Thing __instance, ref float __result)
    {
        if (__instance is not Building_MiningShaft buildingMiningShaft)
        {
            return;
        }

        __result += buildingMiningShaft.ConnectedMapMarketValue;
    }
}