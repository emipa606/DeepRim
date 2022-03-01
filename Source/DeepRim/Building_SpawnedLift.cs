using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepRim;

public class Building_SpawnedLift : Building
{
    public int depth;

    public Map surfaceMap;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref depth, "depth");
    }

    public override string GetInspectString()
    {
        var returnString = $"Layer Depth: {depth}0m";
        var baseString = base.GetInspectString();
        if (!string.IsNullOrEmpty(baseString))
        {
            returnString += $"\n{baseString}";
        }

        return returnString;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (surfaceMap == null)
        {
            yield break;
        }

        var bringUp = new Command_Action
        {
            action = BringUp,
            defaultLabel = "Bring Up",
            defaultDesc = "Bring everything on the elevator up to the surface",
            icon = Building_MiningShaft.UI_BringUp
        };
        yield return bringUp;
    }

    private void BringUp()
    {
        Messages.Message("Bringing Up", MessageTypeDefOf.PositiveEvent);
        var cells = this.OccupiedRect().Cells;
        foreach (var intVec in cells)
        {
            var thingList = intVec.GetThingList(Map);
            var convertedLocation = HarmonyPatches.ConvertParentDrillLocation(
                intVec, Map.Size, surfaceMap.Size);
            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < thingList.Count; index++)
            {
                var thing = thingList[index];
                if (thing is not Pawn && (thing is not ThingWithComps && thing == null || thing is Building))
                {
                    continue;
                }

                thing.DeSpawn();
                GenSpawn.Spawn(thing, convertedLocation, surfaceMap);
            }
        }
    }
}