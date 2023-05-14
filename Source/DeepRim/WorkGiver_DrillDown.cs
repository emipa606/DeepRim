using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepRim;

public class WorkGiver_DrillDown : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ShaftThingDefOf.miningshaft);

    public override PathEndMode PathEndMode => PathEndMode.OnCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ShaftThingDefOf.miningshaft);
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        var allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
        foreach (var buildings in allBuildingsColonist)
        {
            if (buildings.def != ShaftThingDefOf.miningshaft)
            {
                continue;
            }

            if (buildings.def.HasComp(typeof(CompPowerTrader)) &&
                ((Building_MiningShaft)buildings).GetComp<CompPowerTrader>().PowerOn)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t.def.HasComp(typeof(CompPowerTrader)) && ((Building_MiningShaft)t).GetComp<CompPowerTrader>().PowerOn)
        {
            return false;
        }

        if (t.Faction != pawn.Faction)
        {
            return false;
        }

        if (t is not Building building)
        {
            return false;
        }

        if (building.IsForbidden(pawn))
        {
            return false;
        }

        if (!pawn.CanReserve(building))
        {
            return false;
        }

        var miningShaft = (Building_MiningShaft)building;
        if (building.IsBurning())
        {
            return false;
        }

        return miningShaft.CurMode == 1;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return new Job(JobDefOf_DrillDown.OperateDeepMiner, t, 1500, true);
    }
}