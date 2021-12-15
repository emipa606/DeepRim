using Verse;

namespace DeepRim;

public class GenStep_FindDrillLocation : GenStep
{
    public override int SeedPart => 820815231;

    public override void Generate(Map map, GenStepParams parms)
    {
        DeepProfiler.Start("RebuildAllRegions");
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        DeepProfiler.End();
        MapGenerator.PlayerStartSpot = ((UndergroundMapParent)map.info.parent).holeLocation;
    }
}