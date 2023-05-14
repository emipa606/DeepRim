using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
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


    public static readonly Texture2D UI_Send = ContentFinder<Texture2D>.Get("UI/sendDown");

    public static readonly Texture2D UI_BringUp = ContentFinder<Texture2D>.Get("UI/bringUp");

    public static readonly Texture2D UI_Start = ContentFinder<Texture2D>.Get("UI/Start");

    public static readonly Texture2D UI_Pause = ContentFinder<Texture2D>.Get("UI/Pause");

    public static readonly Texture2D UI_IncreasePower = ContentFinder<Texture2D>.Get("UI/IncreasePower");

    public static readonly Texture2D UI_DecreasePower = ContentFinder<Texture2D>.Get("UI/DecreasePower");

    public static readonly Texture2D UI_Abandon = ContentFinder<Texture2D>.Get("UI/Abandon");

    public static readonly Texture2D UI_DrillDown = ContentFinder<Texture2D>.Get("UI/drilldown");

    public static readonly Texture2D UI_DrillUp = ContentFinder<Texture2D>.Get("UI/drillup");

    public static readonly Texture2D UI_Option = ContentFinder<Texture2D>.Get("UI/optionsIcon");

    public static readonly Texture2D UI_Transfer = ContentFinder<Texture2D>.Get("UI/transferIcon");

    public static List<BiomeDef> PossibleBiomeDefs;

    static HarmonyPatches()
    {
        PossibleBiomeDefs = new List<BiomeDef>
        {
            UndergroundBiomeDefOf.Underground
        };
        if (DefDatabase<BiomeDef>.GetNamedSilentFail("BMT_CrystalCaverns") != null)
        {
            PossibleBiomeDefs.Add(BiomeDef.Named("BMT_CrystalCaverns"));
        }

        if (DefDatabase<BiomeDef>.GetNamedSilentFail("BMT_EarthenDepths") != null)
        {
            PossibleBiomeDefs.Add(BiomeDef.Named("BMT_EarthenDepths"));
        }

        if (DefDatabase<BiomeDef>.GetNamedSilentFail("BMT_FungalForest") != null)
        {
            PossibleBiomeDefs.Add(BiomeDef.Named("BMT_FungalForest"));
        }

        if (DefDatabase<BiomeDef>.GetNamedSilentFail("Cave") != null)
        {
            PossibleBiomeDefs.Add(BiomeDef.Named("Cave"));
        }

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
        if (__instance.ParentHolder is UndergroundMapParent mapParent)
        {
            __result = BiomeDef.Named(mapParent.biome);
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
        try
        {
            var shaftThingDef = ShaftThingDefOf.miningshaft;
            if (shaftThingDef == null)
            {
                return;
            }

            var liftThingDef = ShaftThingDefOf.undergroundlift;
            if (liftThingDef == null)
            {
                return;
            }

            if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
            {
                if (liftThingDef.HasComp(typeof(CompPowerPlant)))
                {
                    var powerComp =
                        shaftThingDef.comps.First(properties => properties.GetType() == typeof(CompProperties_Power));
                    liftThingDef.comps.Remove(powerComp);
                }

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

            if (!liftThingDef.HasComp(typeof(CompPowerPlant)))
            {
                var powerComp = new CompProperties_Power
                    { compClass = typeof(CompPowerPlant), basePowerConsumption = 0, transmitsPower = true };
                liftThingDef.comps.Add(powerComp);
            }

            if (!shaftThingDef.HasComp(typeof(CompPowerTrader)))
            {
                var powerComp = new CompProperties_Power
                    { compClass = typeof(CompPowerTrader), basePowerConsumption = 1200 };
                shaftThingDef.comps.Add(powerComp);
            }

            shaftThingDef.description = "Deeprim.PowerDesc".Translate();
            shaftThingDef.costStuffCount = 145;
            shaftThingDef.costList = new List<ThingDefCountClass>
                { new ThingDefCountClass(ThingDefOf.ComponentIndustrial, 5) };
        }
        catch (Exception exception)
        {
            Log.Message($"[DeepRim]: Failed to update the shaft def: {exception}");
        }
    }
}

[DefOf]
public static class ShaftThingDefOf
{
    public static ThingDef miningshaft;
    public static ThingDef undergroundlift;

    static ShaftThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
    }
}

[DefOf]
public static class UndergroundBiomeDefOf
{
    public static BiomeDef Underground;

    static UndergroundBiomeDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BiomeDefOf));
    }
}