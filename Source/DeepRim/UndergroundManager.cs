using System;
using System.Collections.Generic;
using Verse;

namespace DeepRim
{
	// Token: 0x02000009 RID: 9
	public class UndergroundManager : MapComponent
	{
		// Token: 0x06000030 RID: 48 RVA: 0x00002FEB File Offset: 0x000011EB
		public UndergroundManager(Map map) : base(map)
		{
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003008 File Offset: 0x00001208
		public int getNextEmptyLayer(int starting = 1)
		{
			int num = starting;
			while (layersState.ContainsKey(num))
			{
				num++;
			}
			return num;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003038 File Offset: 0x00001238
		public int getNextLayer(int starting = 1)
		{
			int num = starting;
			while (layersState.ContainsKey(num))
			{
				num++;
			}
			return num - 1;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003068 File Offset: 0x00001268
		public void insertLayer(UndergroundMapParent mp)
		{
			int nextEmptyLayer = getNextEmptyLayer(1);
			layersState.Add(nextEmptyLayer, mp);
			mp.depth = nextEmptyLayer;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003094 File Offset: 0x00001294
		public void pinAllUnderground()
		{
			int num = 1;
			foreach (Building building in map.listerBuildings.allBuildingsColonist)
			{
				Building_MiningShaft building_MiningShaft;
				bool flag = (building_MiningShaft = (building as Building_MiningShaft)) != null && building_MiningShaft.isConnected;
				if (flag)
				{
					UndergroundMapParent linkedMapParent = building_MiningShaft.linkedMapParent;
					bool flag2 = linkedMapParent.depth == -1;
					if (flag2)
					{
						while (layersState.ContainsKey(num))
						{
							num++;
						}
						layersState.Add(num, linkedMapParent);
						linkedMapParent.depth = num;
					}
				}
			}
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003158 File Offset: 0x00001358
		public void destroyLayer(UndergroundMapParent layer)
		{
			int depth = layer.depth;
			bool flag = depth == -1;
			if (flag)
			{
				Log.Error("Destroyed layer doesn't have correct depth", false);
			}
			layersState[depth] = null;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003190 File Offset: 0x00001390
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref spawned, "spawned", 0, false);
			Scribe_Collections.Look<int, UndergroundMapParent>(ref layersState, "layers", LookMode.Value, LookMode.Reference, ref list2, ref list3);
			Scribe_References.Look<Map>(ref map, "map", false);
		}

		// Token: 0x06000037 RID: 55 RVA: 0x000031EC File Offset: 0x000013EC
		public override void MapComponentTick()
		{
			bool flag = 1 != spawned;
			if (flag)
			{
				bool flag2 = spawned == 0;
				if (flag2)
				{
					pinAllUnderground();
					spawned = 1;
				}
			}
		}

		// Token: 0x0400001A RID: 26
		private const int targetversion = 1;

		// Token: 0x0400001B RID: 27
		private int spawned = 0;

		// Token: 0x0400001C RID: 28
		public Dictionary<int, UndergroundMapParent> layersState = new Dictionary<int, UndergroundMapParent>();

		// Token: 0x0400001D RID: 29
		private List<int> list2;

		// Token: 0x0400001E RID: 30
		private List<UndergroundMapParent> list3;
	}
}
