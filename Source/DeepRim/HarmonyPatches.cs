using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepRim;

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
    private static readonly Type patchType = typeof(HarmonyPatches);


    public static readonly int[] mapSizes =
    {
        0,
        75,
        200,
        225,
        250,
        275,
        300,
        325
    };

    static HarmonyPatches()
    {
        var harmonyInstance = new Harmony("com.deeprim.rimworld.mod");
        Log.Message("DeepRim: Adding Harmony patch ");
        harmonyInstance.Patch(AccessTools.Property(typeof(Thing), "MarketValue").GetGetMethod(false), null,
            new HarmonyMethod(patchType, "MarketValuePostfix"));
        harmonyInstance.Patch(AccessTools.Property(typeof(Map), "Biome").GetGetMethod(false), null,
            new HarmonyMethod(patchType, "MapBiomePostfix"));
    }

    private static void MarketValuePostfix(Thing __instance, ref float __result)
    {
        if (__instance is not Building_MiningShaft buildingMiningShaft)
        {
            return;
        }

        __result += buildingMiningShaft.ConnectedMapMarketValue;
    }

    private static void MapBiomePostfix(Map __instance, ref BiomeDef __result)
    {
        if (__instance.ParentHolder is UndergroundMapParent)
        {
            __result = DefDatabase<BiomeDef>.GetNamed("Underground");
        }
    }

    public static IntVec3 ConvertParentDrillLocation(IntVec3 parentLocation, IntVec3 parentSize, IntVec3 childSize)
    {
        var parentXPercent = (float)parentLocation.x / parentSize.x;
        var parentZPercent = (float)parentLocation.z / parentSize.z;
        return new IntVec3((int)Math.Round(childSize.x * parentXPercent), parentLocation.y,
            (int)Math.Round(childSize.z * parentZPercent));
    }
}