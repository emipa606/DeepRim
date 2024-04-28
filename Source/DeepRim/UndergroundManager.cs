using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DeepRim;

public class UndergroundManager(Map map) : MapComponent(map)
{
    private const int targetversion = 1;

    private int activeLayers = -1;

    public bool AnyLayersPowered = true;
    public Dictionary<int, string> layerNames = new Dictionary<int, string>();

    public Dictionary<int, UndergroundMapParent> layersState = new Dictionary<int, UndergroundMapParent>();

    private List<int> list2;

    private List<UndergroundMapParent> list3;
    private List<int> list4;

    private List<string> list5;

    private int nextLayer;

    private int spawned;

    public int ActiveLayers
    {
        get
        {
            if (activeLayers == -1)
            {
                activeLayers = layersState.Count;
            }

            return activeLayers;
        }
        set => activeLayers = value;
    }

    public int NextLayer
    {
        get
        {
            if (nextLayer != 0)
            {
                return nextLayer;
            }

            DeepRimMod.LogMessage("nextlayer is 0, trying to find deepest layer");
            if (layersState.Any())
            {
                var deepest = 0;
                var enumerator = layersState.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    DeepRimMod.LogMessage($"Layer: {enumerator.Current}");
                    if (enumerator.Current.Key > nextLayer)
                    {
                        deepest = enumerator.Current.Key;
                    }
                }

                nextLayer = deepest + 1;
                DeepRimMod.LogMessage($"nextLayer is being set to: {nextLayer}");
                return nextLayer;
            }

            nextLayer = 1;
            return nextLayer;
        }
        set => nextLayer = value;
    }

    public void InitLayerNames()
    {
        DeepRimMod.LogMessage("Initializing layerNames variable");
        layerNames = new Dictionary<int, string>();
        foreach (var layer in layersState)
        {
            layerNames[layer.Key] = "";
        }
    }

    public string GetLayerName(int key)
    {
        if (layerNames == null)
        {
            InitLayerNames();
        }

        return layerNames != null && layerNames.TryGetValue(key, out var name) ? name : "";
    }

    public void InsertLayer(UndergroundMapParent mp)
    {
        DeepRimMod.LogMessage(
            $"Drilled new layer at depth {NextLayer} with ore density {DeepRimMod.instance.DeepRimSettings.OreDensity}");
        ActiveLayers++;
        layersState.Add(NextLayer, mp);
        mp.depth = NextLayer;
        NextLayer++;
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
        Scribe_Values.Look(ref activeLayers, "activeLayers");
        Scribe_Values.Look(ref nextLayer, "nextLayer");
        Scribe_Collections.Look(ref layersState, "layers", LookMode.Value, LookMode.Reference, ref list2,
            ref list3);
        Scribe_Collections.Look(ref layerNames, "layerNames", LookMode.Value, LookMode.Value, ref list4, ref list5);
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