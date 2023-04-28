using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DeepRim;

public class Command_TargetLayer : Command_Action
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
            list.Add(new FloatMenuOption("Deeprim.NewLayer".Translate(), delegate
            {
                shaft.drillNew = true;
                shaft.PauseDrilling();
            }));
            using var enumerator = manager.layersState.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var pair = enumerator.Current;
                if (pair.Value != null)
                {
                    list.Add(new FloatMenuOption("Deeprim.SelectLayerAt".Translate(pair.Key), delegate
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