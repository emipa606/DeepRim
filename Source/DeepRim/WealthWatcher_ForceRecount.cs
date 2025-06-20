using System;
using System.Linq;
using DeepRim;
using HarmonyLib;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.ForceRecount))]
public static class WealthWatcher_ForceRecount
{
    public static void Postfix(Map ___map, ref float ___wealthItems, ref float ___wealthBuildings,
        ref float ___wealthPawns, ref float ___wealthFloorsOnly)
    {
        var shaft = ___map?.listerBuildings?.AllBuildingsColonistOfClass<Building_MiningShaft>()?.FirstOrDefault();
        if (shaft == null)
        {
            DeepRimMod.LogWarn("Did not find any mineshafts in forced map wealth recount");
            return;
        }

        if (Current.ProgramState == ProgramState.MapInitializing)
        {
            DeepRimMod.LogMessage("Skipping wealth recount as map is still initializing");
            return;
        }

        var depthValueBase = (float)Math.Round((float)DeepRimMod.Instance.DeepRimSettings.DepthValueBase / 100, 2);
        var depthValueFalloff =
            (float)Math.Round((float)DeepRimMod.Instance.DeepRimSettings.DepthValueFalloff / 100, 2);
        DeepRimMod.LogWarn(
            $"Adding colony wealth from underground layers. Base % value is {depthValueBase}, % fall-off per layer is {depthValueFalloff}");
        foreach (var layer in shaft.UndergroundManager.layersState)
        {
            var map = layer.Value.Map;
            var percentAdjustment = depthValueBase * (1 - (depthValueFalloff * layer.Key));
            percentAdjustment = percentAdjustment >= 0 ? percentAdjustment : 0;
            DeepRimMod.LogMessage($"Adding layer value * {percentAdjustment} for layer at depth {layer.Key}");
            ___wealthFloorsOnly += map.wealthWatcher.WealthFloorsOnly * percentAdjustment;
            ___wealthBuildings += map.wealthWatcher.WealthBuildings * percentAdjustment;
            ___wealthPawns += map.wealthWatcher.WealthPawns * percentAdjustment;
            ___wealthItems += map.wealthWatcher.WealthItems * percentAdjustment;
        }
    }
}