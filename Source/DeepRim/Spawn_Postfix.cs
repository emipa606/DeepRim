using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using DeepRim;
using System.Linq;

[HarmonyPatch(typeof(PawnsArrivalModeWorker_EdgeWalkIn), "Arrive")]
public static class PawnsArrivalModeWorker_EdgeWalkIn_Patch
{
    public static bool Prefix(ref List<Pawn> pawns, ref IncidentParms parms)
    {
        Map map = (Map)parms.target;
        var lift = map.listerBuildings.AllBuildingsColonistOfClass<Building_SpawnedLift>().FirstOrDefault();
        if (lift != null){
            Log.Message($"Found lift: {lift}. Looks like you're trying to spawn pawns underground! Fixing...");
            for (int i = 0; i < pawns.Count; i++)
            {
                Map parentMap = lift.parentDrill.Map;
                IntVec3 cell = CellFinder.RandomEdgeCell(parentMap);
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(cell, parentMap, 1000);
                GenSpawn.Spawn(pawns[i], loc, parentMap, parms.spawnRotation);
            }
        return false;
        }
        return true;
    }
}