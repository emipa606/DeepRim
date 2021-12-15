using System.Collections.Generic;
using Verse;

namespace DeepRim;

public class UndergroundManager : MapComponent
{
    private const int targetversion = 1;

    public Dictionary<int, UndergroundMapParent> layersState = new Dictionary<int, UndergroundMapParent>();

    private List<int> list2;

    private List<UndergroundMapParent> list3;

    private int spawned;

    public UndergroundManager(Map map) : base(map)
    {
    }

    public int GetNextEmptyLayer(int starting = 1)
    {
        var num = starting;
        while (layersState.ContainsKey(num))
        {
            num++;
        }

        return num;
    }

    public int GetNextLayer(int starting = 1)
    {
        var num = starting;
        while (layersState.ContainsKey(num))
        {
            num++;
        }

        return num - 1;
    }

    public void InsertLayer(UndergroundMapParent mp)
    {
        var nextEmptyLayer = GetNextEmptyLayer();
        layersState.Add(nextEmptyLayer, mp);
        mp.depth = nextEmptyLayer;
    }

    public void PinAllUnderground()
    {
        var num = 1;
        foreach (var building in map.listerBuildings.allBuildingsColonist)
        {
            Building_MiningShaft building_MiningShaft;
            if ((building_MiningShaft = building as Building_MiningShaft) == null ||
                !building_MiningShaft.IsConnected)
            {
                continue;
            }

            var linkedMapParent = building_MiningShaft.LinkedMapParent;
            if (linkedMapParent.depth != -1)
            {
                continue;
            }

            while (layersState.ContainsKey(num))
            {
                num++;
            }

            layersState.Add(num, linkedMapParent);
            linkedMapParent.depth = num;
        }
    }

    public void DestroyLayer(UndergroundMapParent layer)
    {
        var depth = layer.depth;
        if (depth == -1)
        {
            Log.Error("Destroyed layer doesn't have correct depth");
        }

        layersState.Remove(depth);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref spawned, "spawned");
        Scribe_Collections.Look(ref layersState, "layers", LookMode.Value, LookMode.Reference, ref list2,
            ref list3);
        Scribe_References.Look(ref map, "map");
    }

    public override void MapComponentTick()
    {
        if (1 == spawned)
        {
            return;
        }

        if (spawned != 0)
        {
            return;
        }

        PinAllUnderground();
        spawned = 1;
    }
}