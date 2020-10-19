using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace DeepRim
{
	// Token: 0x02000005 RID: 5
	public class UndergroundMapParent : MapParent
	{
		// Token: 0x0600001F RID: 31 RVA: 0x00002C88 File Offset: 0x00000E88
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref holeLocation, "holeLocation", default, false);
			Scribe_Values.Look(ref depth, "depth", -1, false);
			Scribe_Values.Look(ref shouldRiver, "shouldRiver", true, false);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002CDE File Offset: 0x00000EDE
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo current in base.GetGizmos())
			{
				yield return current;
			}
			yield break;
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000021 RID: 33 RVA: 0x00002CF0 File Offset: 0x00000EF0
		protected override bool UseGenericEnterMapFloatMenuOption
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002D04 File Offset: 0x00000F04
		public void AbandonLift(Thing lift)
		{
			lift.DeSpawn(DestroyMode.Vanish);
			foreach (Building building in Map.listerBuildings.allBuildingsColonist)
			{
				bool flag = building is Building_SpawnedLift;
				if (flag)
				{
					Log.Message("There's still remaining shafts leading to layer.", false);
					return;
				}
			}
			Abandon();
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002D8C File Offset: 0x00000F8C
		public void Abandon()
		{
			Log.Message("Utter destruction of a layer. GG. Never going to get it back now XDD", false);
			shouldBeDeleted = true;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002DA4 File Offset: 0x00000FA4
		public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
		{
			alsoRemoveWorldObject = false;
			bool result = false;
			bool flag = shouldBeDeleted;
			if (flag)
			{
				result = true;
				alsoRemoveWorldObject = true;
			}
			return result;
		}

		// Token: 0x04000014 RID: 20
		public bool shouldBeDeleted = false;

		// Token: 0x04000015 RID: 21
		public IntVec3 holeLocation;

		// Token: 0x04000016 RID: 22
		public int depth = -1;

		// Token: 0x04000017 RID: 23
		public bool shouldRiver = true;
	}
}
