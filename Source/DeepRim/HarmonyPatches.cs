using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepRim;

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
    public static readonly Texture2D UISend = ContentFinder<Texture2D>.Get("UI/sendDown");

    public static readonly Texture2D UIBringUp = ContentFinder<Texture2D>.Get("UI/bringUp");

    public static readonly Texture2D UIStart = ContentFinder<Texture2D>.Get("UI/Start");

    public static readonly Texture2D UIPause = ContentFinder<Texture2D>.Get("UI/Pause");

    public static readonly Texture2D UIIncreasePower = ContentFinder<Texture2D>.Get("UI/IncreasePower");

    public static readonly Texture2D UIDecreasePower = ContentFinder<Texture2D>.Get("UI/DecreasePower");

    public static readonly Texture2D UIAbandon = ContentFinder<Texture2D>.Get("UI/Abandon");

    public static readonly Texture2D UIOption = ContentFinder<Texture2D>.Get("UI/optionsIcon");

    public static readonly Texture2D UIToggleSendPower = ContentFinder<Texture2D>.Get("UI/ToggleSendPower");

    public static readonly Texture2D UITransfer = ContentFinder<Texture2D>.Get("UI/transferIcon");

    public static readonly List<BiomeDef> PossibleBiomeDefs;

    static HarmonyPatches()
    {
        PossibleBiomeDefs = [UndergroundBiomeDefOf.Underground];
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

        new Harmony("com.deeprim.rimworld.mod").PatchAll(Assembly.GetExecutingAssembly());
        RefreshDrillTechLevel();
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

            if (DeepRimMod.Instance.DeepRimSettings.LowTechMode)
            {
                if (liftThingDef.HasComp(typeof(CompPowerPlant)))
                {
                    var powerComp =
                        shaftThingDef.comps.FirstOrDefault(properties =>
                            properties.GetType() == typeof(CompProperties_Power));
                    if (powerComp != null)
                    {
                        liftThingDef.comps.Remove(powerComp);
                    }
                }

                if (shaftThingDef.HasComp(typeof(CompPowerTrader)))
                {
                    var powerComp =
                        shaftThingDef.comps.FirstOrDefault(properties =>
                            properties.GetType() == typeof(CompProperties_Power));
                    if (powerComp != null)
                    {
                        shaftThingDef.comps.Remove(powerComp);
                    }
                }

                if (liftThingDef.HasComp(typeof(CompGlower)))
                {
                    var glowerComp =
                        liftThingDef.comps.FirstOrDefault(properties =>
                            properties.GetType() == typeof(CompProperties_Glower));
                    if (glowerComp != null)
                    {
                        liftThingDef.comps.Remove(glowerComp);
                    }
                }

                if (shaftThingDef.HasComp(typeof(CompGlower)))
                {
                    var glowerComp =
                        shaftThingDef.comps.FirstOrDefault(properties =>
                            properties.GetType() == typeof(CompProperties_Glower));
                    if (glowerComp != null)
                    {
                        shaftThingDef.comps.Remove(glowerComp);
                    }
                }

                shaftThingDef.description = "Deeprim.NoPowerDesc".Translate();
                shaftThingDef.costStuffCount = 300;
                shaftThingDef.costList = null;
                liftThingDef.costList = null;
                shaftThingDef.stuffCategories = [StuffCategoryDefOf.Metallic, StuffCategoryDefOf.Woody];
                liftThingDef.stuffCategories = [StuffCategoryDefOf.Metallic, StuffCategoryDefOf.Woody];
                shaftThingDef.graphicData.texPath = "Things/industrialmine";
                liftThingDef.graphicData.texPath = "Things/shaft";
                shaftThingDef.uiIcon = ContentFinder<Texture2D>.Get("Things/industrialmine");
                liftThingDef.uiIcon = ContentFinder<Texture2D>.Get("Things/shaft");
                DeepRimMod.InitMethodInfo.Invoke(shaftThingDef.graphicData, null);
                DeepRimMod.InitMethodInfo.Invoke(liftThingDef.graphicData, null);
                return;
            }

            if (!liftThingDef.HasComp(typeof(CompPowerPlant)))
            {
                var powerComp = new CompProperties_Power
                    { compClass = typeof(CompPowerPlant), transmitsPower = true };
                DeepRimMod.BasePowerConsumptionFieldInfo.SetValue(powerComp, 0);
                liftThingDef.comps.Add(powerComp);
            }

            if (!shaftThingDef.HasComp(typeof(CompPowerTrader)))
            {
                var powerComp = new CompProperties_Power
                    { compClass = typeof(CompPowerTrader), transmitsPower = true };
                DeepRimMod.BasePowerConsumptionFieldInfo.SetValue(powerComp, 1200);
                shaftThingDef.comps.Add(powerComp);
            }

            if (!liftThingDef.HasComp(typeof(CompGlower)))
            {
                var glowerComp = new CompProperties_Glower
                    { compClass = typeof(CompGlower), glowRadius = 4, glowColor = new ColorInt(255, 0, 0, 0) };
                liftThingDef.comps.Add(glowerComp);
            }

            if (!shaftThingDef.HasComp(typeof(CompGlower)))
            {
                var glowerComp = new CompProperties_Glower
                    { compClass = typeof(CompGlower), glowRadius = 4, glowColor = new ColorInt(255, 0, 0, 0) };
                shaftThingDef.comps.Add(glowerComp);
            }

            shaftThingDef.description = "Deeprim.PowerDesc".Translate();
            shaftThingDef.costStuffCount = 0;
            shaftThingDef.stuffCategories = null;
            liftThingDef.stuffCategories = null;
            shaftThingDef.costList =
            [
                new ThingDefCountClass(ThingDefOf.ComponentIndustrial, 5), new ThingDefCountClass(ThingDefOf.Steel, 245)
            ];
            shaftThingDef.graphicData.texPath = "Things/hightechmine";
            liftThingDef.graphicData.texPath = "Things/hightechshaft";
            shaftThingDef.uiIcon = ContentFinder<Texture2D>.Get("Things/hightechmine");
            liftThingDef.uiIcon = ContentFinder<Texture2D>.Get("Things/hightechshaft");
            DeepRimMod.InitMethodInfo.Invoke(shaftThingDef.graphicData, null);
            DeepRimMod.InitMethodInfo.Invoke(liftThingDef.graphicData, null);
        }
        catch (Exception exception)
        {
            Log.Message($"[DeepRim]: Failed to update the shaft def: {exception}");
        }
    }
}