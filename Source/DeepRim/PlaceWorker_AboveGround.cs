using RimWorld;
using Verse;

namespace DeepRim
{
    // Token: 0x02000008 RID: 8
    public class PlaceWorker_AboveGround : PlaceWorker
	{
		// Token: 0x0600002D RID: 45 RVA: 0x00002F90 File Offset: 0x00001190
		public virtual AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
		{
			bool flag = map.ParentHolder is UndergroundMapParent;
			AcceptanceReport result;
			if (flag)
			{
				result = "Must be placed above ground.";
			}
			else
			{
				result = true;
			}
			return result;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002FC8 File Offset: 0x000011C8
		public override bool ForceAllowPlaceOver(BuildableDef otherDef)
		{
			return otherDef == ThingDefOf.SteamGeyser;
		}
	}
}
