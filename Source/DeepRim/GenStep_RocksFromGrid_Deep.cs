using RimWorld;
using Verse;

namespace DeepRim;

public class GenStep_RocksFromGrid_Deep : GenStep
{
    private const int MinRoofedCellsPerGroup = 20;

    public override int SeedPart => 8204671;

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

    public override void Generate(Map map, GenStepParams parms)
    {
        map.regionAndRoomUpdater.Enabled = false;
        foreach (var intVec in map.AllCells)
        {
            var rockDefAt = GenStep_RocksFromGrid.RockDefAt(intVec);
            if (((UndergroundMapParent)map.info.parent).holeLocation.DistanceTo(intVec) > 5f)
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

    private bool IsNaturalRoofAt(IntVec3 c, Map map)
    {
        return c.Roofed(map) && c.GetRoof(map).isNatural;
    }
}