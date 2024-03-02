using RimWorld;
using UnityEngine;
using Verse;

namespace DeepRim;

public class PlaceWorker_AboveGround : PlaceWorker
{
    private bool ShiftIsHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        if (map.ParentHolder is UndergroundMapParent)
        {
            return "Deeprim.AboveGround".Translate();
        }

        if ((map.listerBuildings.ColonistsHaveBuilding(ShaftThingDefOf.miningshaft) ||
             map.listerBuildings.ColonistsHaveBuilding(ShaftThingDefOf.undergroundlift)) &&
            !ShiftIsHeld)
        {
            return "Deeprim.NoMoreShafts".Translate();
        }

        return true;
    }

    public override bool ForceAllowPlaceOver(BuildableDef otherDef)
    {
        return otherDef == ThingDefOf.SteamGeyser;
    }
}