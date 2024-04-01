using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DeepRim;

public class Command_TargetLayer(bool isUndergroundLift = false) : Command_Action
{
    public UndergroundManager manager;

    public Building_MiningShaft shaft;

    public override void ProcessInput(Event ev)
    {
        Find.WindowStack.Add(MakeMenu());
    }

    private FloatMenu MakeMenu()
    {
        var list = new List<FloatMenuOption>();
        if (shaft.CurMode != 1)
        {
            if(!isUndergroundLift){
                list.Add(new FloatMenuOption("Deeprim.NewLayer".Translate(), delegate
                {
                    shaft.targetedLevel = -1;
                    shaft.drillNew = true;
                    shaft.PauseDrilling();
                }));
            }
            using var enumerator = manager.layersState.OrderBy(x => x.Key).GetEnumerator();
            while (enumerator.MoveNext())
            {
                var pair = enumerator.Current;
                if (pair.Value != null)
                {
                    var name = manager.GetLayerName(pair.Key);
                    var label = name == "" ? "Deeprim.UnnamedLayer".Translate(pair.Key).ToString() : "Deeprim.LayerDepthNamed".Translate(pair.Key, manager.layerNames[pair.Key]).ToString();
                    list.Add(new FloatMenuOption(label, delegate
                    {
                        shaft.drillNew = false;
                        shaft.targetedLevel = pair.Key;
                        shaft.PauseDrilling();
                        shaft.SyncConnectedMap();
                    }));
                }
            }
        }
        else
        {
            list.Add(new FloatMenuOption("Deeprim.NotWhileDrilling".Translate(), null));
        }

        return new FloatMenu(list);
    }
}