using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DeepRim;
using HarmonyLib;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(WealthWatcher), "ForceRecount")]
public static class WealthWatcherForceRecount_Patch
{
    public static void Postfix(Map ___map, ref float ___wealthItems, ref float ___wealthBuildings,ref float ___wealthPawns,ref float ___wealthFloorsOnly)
    {
        var shaft = ___map?.listerBuildings?.AllBuildingsColonistOfClass<Building_MiningShaft>()?.FirstOrDefault();
        if(shaft == null){
            DeepRimMod.LogWarn("Did not find any mineshafts in forced map wealth recount");
            return;
        }
        float DepthValueBase = (float)Math.Round((float)DeepRimMod.instance.DeepRimSettings.DepthValueBase / 100, 2);
        float DepthValueFalloff = (float)Math.Round((float)DeepRimMod.instance.DeepRimSettings.DepthValueFalloff / 100, 2);
        DeepRimMod.LogWarn($"Adding colony wealth from underground layers. Base % value is {DepthValueBase}, % fall-off per layer is {DepthValueFalloff}");
        foreach (KeyValuePair<int, UndergroundMapParent> layer in shaft.UndergroundManager.layersState){
            Map map = layer.Value.Map;
            float PercentAdjustment = DepthValueBase * (1 - (DepthValueFalloff * layer.Key));
            PercentAdjustment = PercentAdjustment >= 0 ? PercentAdjustment : 0;
            //EXAMPLE MATH if valueBase is 0.8, falloff is 0.1
            //For each layer, wealth percent added would be: [1: 0.72, 2: 0.8, 3: 0.7 ... 10: 0]
            DeepRimMod.LogMessage($"Adding layer value * {PercentAdjustment} for layer at depth {layer.Key}");
            ___wealthFloorsOnly += map.wealthWatcher.WealthFloorsOnly * PercentAdjustment;
            ___wealthBuildings += map.wealthWatcher.WealthBuildings * PercentAdjustment;
            ___wealthPawns += map.wealthWatcher.WealthPawns * PercentAdjustment;
            ___wealthItems += map.wealthWatcher.WealthItems * PercentAdjustment;
        }
    }
}
