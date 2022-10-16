using System;
using System.Collections.Generic;
using System.Linq;
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
        harmonyInstance.Patch(
            AccessTools.Property(typeof(CompHeatPusherPowered), "ShouldPushHeatNow").GetGetMethod(true), null,
            new HarmonyMethod(patchType, "HeatPusherPoweredPostfix"));
        harmonyInstance.Patch(AccessTools.Property(typeof(Map), "Biome").GetGetMethod(false), null,
            new HarmonyMethod(patchType, "MapBiomePostfix"));
        RefreshDrillTechLevel();
    }

    private static void MarketValuePostfix(Thing __instance, ref float __result)
    {
        if (__instance is not Building_MiningShaft buildingMiningShaft)
        {
            return;
        }

        __result += buildingMiningShaft.ConnectedMapMarketValue;
    }

    private static void HeatPusherPoweredPostfix(CompHeatPusherPowered __instance, ref bool __result)
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
        return new IntVec3((int)Math.Floor(childSize.x * parentXPercent), parentLocation.y,
            (int)Math.Floor(childSize.z * parentZPercent));
    }

    public static void RefreshDrillTechLevel()
    {
        var shaftThingDef = DefDatabase<ThingDef>.GetNamedSilentFail("miningshaft");
        if (shaftThingDef == null)
        {
            return;
        }

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            if (shaftThingDef.HasComp(typeof(CompPowerTrader)))
            {
                var powerComp =
                    shaftThingDef.comps.First(properties => properties.GetType() == typeof(CompProperties_Power));
                shaftThingDef.comps.Remove(powerComp);
            }

            shaftThingDef.description = "Deeprim.NoPowerDesc".Translate();
            shaftThingDef.costStuffCount = 300;
            shaftThingDef.costList = null;
            return;
        }

        if (!shaftThingDef.HasComp(typeof(CompPowerTrader)))
        {
            var powerComp = new CompProperties_Power { compClass = typeof(CompPowerTrader) };
            typeof(CompProperties_Power).GetField("basePowerConsumption").SetValue(powerComp, 1200);
            shaftThingDef.comps.Add(powerComp);
        }

        shaftThingDef.description = "Deeprim.PowerDesc".Translate();
        shaftThingDef.costStuffCount = 145;
        shaftThingDef.costList = new List<ThingDefCountClass>(new List<ThingDefCountClass>
            { new ThingDefCountClass(ThingDefOf.ComponentIndustrial, 5) });
    }
}