using System.Collections.Generic;
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

        foreach (KeyValuePair<int, UndergroundMapParent> layer in shaft.UndergroundManager.layersState){
            Map map = layer.Value.Map;
            DeepRimMod.LogMessage($"Updating mining shaft parent map wealth using underground layer {map}");
            ___wealthFloorsOnly += map.wealthWatcher.WealthFloorsOnly;
            ___wealthBuildings += map.wealthWatcher.WealthBuildings;
            ___wealthPawns += map.wealthWatcher.WealthPawns;
            ___wealthItems += map.wealthWatcher.WealthItems;
        }
    }
}
