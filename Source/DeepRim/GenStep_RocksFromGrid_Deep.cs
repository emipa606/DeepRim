using RimWorld;
using Verse;

namespace DeepRim
{
    // Token: 0x02000004 RID: 4
    public class GenStep_RocksFromGrid_Deep : GenStep
    {
        // Token: 0x04000013 RID: 19
        private const int MinRoofedCellsPerGroup = 20;

        // Token: 0x17000006 RID: 6
        // (get) Token: 0x0600001A RID: 26 RVA: 0x00002AC0 File Offset: 0x00000CC0
        public override int SeedPart => 8204671;

        // Token: 0x0600001B RID: 27 RVA: 0x00002AD8 File Offset: 0x00000CD8
        public static ThingDef RockDefAt(IntVec3 c)
        {
            ThingDef thingDef = null;
            var num = -999999f;
            foreach (var rockNoise in RockNoises.rockNoises)
            {
                if (!(rockNoise.noise.GetValue(c) > num))
                {
                    continue;
                }

                thingDef = rockNoise.rockDef;
                num = rockNoise.noise.GetValue(c);
            }

            if (thingDef != null)
            {
                return thingDef;
            }

            Log.ErrorOnce("Did not get rock def to generate at " + c, 50812);
            thingDef = ThingDefOf.Sandstone;

            return thingDef;
        }

        // Token: 0x0600001C RID: 28 RVA: 0x00002B78 File Offset: 0x00000D78
        public override void Generate(Map map, GenStepParams parms)
        {
            map.regionAndRoomUpdater.Enabled = false;
            foreach (var intVec in map.AllCells)
            {
                var rockDefAt = GenStep_RocksFromGrid.RockDefAt(intVec);
                if (((UndergroundMapParent) map.info.parent).holeLocation.DistanceTo(intVec) > 5f)
                {
                    GenSpawn.Spawn(rockDefAt, intVec, map);
                }

                map.roofGrid.SetRoof(intVec, RoofDefOf.RoofRockThick);
            }

            var genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineable();
            var num = 16f;
            genStep_ScatterLumpsMineable.countPer10kCellsRange = new FloatRange(num, num);
            genStep_ScatterLumpsMineable.Generate(map, default);
            map.regionAndRoomUpdater.Enabled = true;
        }

        // Token: 0x0600001D RID: 29 RVA: 0x00002C5C File Offset: 0x00000E5C
        private bool IsNaturalRoofAt(IntVec3 c, Map map)
        {
            return c.Roofed(map) && c.GetRoof(map).isNatural;
        }
    }
}