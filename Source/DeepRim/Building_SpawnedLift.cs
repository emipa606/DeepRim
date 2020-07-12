using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace DeepRim
{
    // Token: 0x02000007 RID: 7
    public class Building_SpawnedLift : Building
    {
        // Token: 0x0600002B RID: 43 RVA: 0x00002EF9 File Offset: 0x000010F9
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.depth, "depth", 0, false);
        }

        // Token: 0x0600002C RID: 44 RVA: 0x00002F18 File Offset: 0x00001118
        public override string GetInspectString()
        {
            var returnString = $"Layer Depth: {depth}0m";
            var baseString = base.GetInspectString();
            if (!string.IsNullOrEmpty(baseString))
                returnString += $"\n{baseString}";
            return returnString;
        }

		public override IEnumerable<Gizmo> GetGizmos()
		{
			if (surfaceMap != null)
			{
				Command_Action bringUp = new Command_Action();
				bringUp.action = new Action(this.BringUp);
				bringUp.defaultLabel = "Bring Up";
				bringUp.defaultDesc = "Bring everything on the elavator up to the surface";
				bringUp.icon = Building_MiningShaft.UI_BringUp;
				yield return bringUp;
				bringUp = null;
				yield break;
			}
		}

		private void BringUp()
		{
			Messages.Message("Bringing Up", MessageTypeDefOf.PositiveEvent, true);
			IEnumerable<IntVec3> cells = this.OccupiedRect().Cells;
			foreach (IntVec3 intVec in cells)
			{
				List<Thing> thingList = intVec.GetThingList(this.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					Log.Warning(string.Concat(new object[]
					{
						"Test ",
						i,
						" ",
						thingList[i]
					}), false);
					bool flag2 = thingList[i] is Pawn || (thingList[i] is ThingWithComps && !(thingList[i] is Building));
					if (flag2)
					{
						Thing thing = thingList[i];
						thing.DeSpawn(DestroyMode.Vanish);
						GenSpawn.Spawn(thing, intVec, surfaceMap, WipeMode.Vanish);
					}
				}
			}
		}

		// Token: 0x04000019 RID: 25
		public int depth;

		public Map surfaceMap;
    }
}
