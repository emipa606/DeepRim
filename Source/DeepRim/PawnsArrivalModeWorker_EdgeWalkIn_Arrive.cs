using System.Collections.Generic;
using System.Linq;
using DeepRim;
using HarmonyLib;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(PawnsArrivalModeWorker_EdgeWalkIn), nameof(PawnsArrivalModeWorker_EdgeWalkIn.Arrive))]
public static class PawnsArrivalModeWorker_EdgeWalkIn_Arrive
{
    public static bool Prefix(ref List<Pawn> pawns, ref IncidentParms parms)
    {
        var map = (Map)parms.target;
        var lift = map.listerBuildings.AllBuildingsColonistOfClass<Building_SpawnedLift>().FirstOrDefault();
        if (lift == null)
        {
            return true;
        }

        Log.Message($"Found lift: {lift}. Looks like you're trying to spawn pawns underground! Fixing...");
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < pawns.Count; i++)
        {
            var parentMap = lift.parentDrill.Map;
            var cell = CellFinder.RandomEdgeCell(parentMap);
            var loc = CellFinder.RandomClosewalkCellNear(cell, parentMap, 20);
            GenSpawn.Spawn(pawns[i], loc, parentMap, parms.spawnRotation);
        }

        return false;
    }
}