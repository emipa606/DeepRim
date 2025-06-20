using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DeepRim;

public class UndergroundMapParent : MapParent
{
    public BiomeDef biome = UndergroundBiomeDefOf.Underground;
    public int depth = -1;

    public IntVec3 holeLocation;

    private bool shouldBeDeleted;

    private bool shouldRiver = true;

    protected override bool UseGenericEnterMapFloatMenuOption => false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref holeLocation, "holeLocation");
        Scribe_Values.Look(ref depth, "depth", -1);
        Scribe_Values.Look(ref shouldRiver, "shouldRiver", true);
        Scribe_Defs.Look(ref biome, "biome");
        biome ??= UndergroundBiomeDefOf.Underground;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var current in base.GetGizmos())
        {
            yield return current;
        }
    }

    public Building_SpawnedLift GetSpawnedLift()
    {
        foreach (var building in Map.listerBuildings.allBuildingsColonist)
        {
            if (building is not Building_SpawnedLift spawnedLift)
            {
                continue;
            }

            return spawnedLift;
        }

        return null;
    }

    public void AbandonLift(Thing lift, bool force = false)
    {
        lift.DeSpawn();
        if (!force)
        {
            foreach (var building in Map.listerBuildings.allBuildingsColonist)
            {
                if (building is not Building_SpawnedLift)
                {
                    continue;
                }

                DeepRimMod.LogMessage("There's still remaining shafts leading to layer.");
                return;
            }
        }

        Abandon(false);
    }

    public override void Abandon(bool wasGravshipLaunch)
    {
        Log.Message("Utter destruction of a layer. GG. Never going to get it back now XDD");
        shouldBeDeleted = true;
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        if (!shouldBeDeleted)
        {
            return false;
        }

        alsoRemoveWorldObject = true;

        return true;
    }
}