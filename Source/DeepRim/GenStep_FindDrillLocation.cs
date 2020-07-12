using System;
using Verse;

namespace DeepRim
{
	// Token: 0x02000003 RID: 3
	public class GenStep_FindDrillLocation : GenStep
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000017 RID: 23 RVA: 0x00002A64 File Offset: 0x00000C64
		public override int SeedPart
		{
			get
			{
				return 820815231;
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002A7B File Offset: 0x00000C7B
		public override void Generate(Map map, GenStepParams parms)
		{
			DeepProfiler.Start("RebuildAllRegions");
			map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
			DeepProfiler.End();
			MapGenerator.PlayerStartSpot = ((UndergroundMapParent)map.info.parent).holeLocation;
		}
	}
}
