using RimWorld;
using Verse;

namespace DeepRim;

public class PlaceWorker_AboveGround : PlaceWorker
{
    public virtual AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null)
    {
        AcceptanceReport result;
        if (map.ParentHolder is UndergroundMapParent)
        {
            result = "Deeprim.AboveGround".Translate();
        }
        else
        {
            result = true;
        }

        return result;
    }

    public override bool ForceAllowPlaceOver(BuildableDef otherDef)
    {
        return otherDef == ThingDefOf.SteamGeyser;
    }
}