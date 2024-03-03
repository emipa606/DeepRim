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

    public bool shouldBeDeleted;

    public bool shouldRiver = true;

    public override bool UseGenericEnterMapFloatMenuOption => false;

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

                Log.Message("There's still remaining shafts leading to layer.");
                return;
            }
        }

        Abandon();
    }

    public override void Abandon()
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