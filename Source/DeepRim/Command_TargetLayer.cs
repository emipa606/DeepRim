using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DeepRim;

public class Command_TargetLayer(Building_SpawnedLift lift = null) : Command_Action
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
            if (lift == null)
            {
                list.Add(new FloatMenuOption("Deeprim.NewLayer".Translate(), delegate
                {
                    shaft.targetedLevel = -1;
                    shaft.drillNew = true;
                    shaft.PauseDrilling();
                }));
            }
            else
            {
                list.Add(new FloatMenuOption("Deeprim.Surface".Translate(), delegate { shaft.targetedLevel = 0; }));
            }

            using var enumerator = manager.layersState.OrderBy(x => x.Key).GetEnumerator();
            while (enumerator.MoveNext())
            {
                var pair = enumerator.Current;
                if (pair.Value == null)
                {
                    continue;
                }

                var name = manager.GetLayerName(pair.Key);
                string label;
                if (lift != null && lift?.depth == pair.Key)
                {
                    label = "Deeprim.TargetLayerAtThis".Translate(pair.Key);
                }
                else if (name != "")
                {
                    label = "Deeprim.LayerDepthNamed".Translate(pair.Key, manager.layerNames[pair.Key]);
                }
                else
                {
                    label = "Deeprim.UnnamedLayer".Translate(pair.Key);
                }

                list.Add(new FloatMenuOption(label, delegate
                {
                    shaft.drillNew = false;
                    shaft.targetedLevel = pair.Key;
                    shaft.PauseDrilling();
                    shaft.SyncConnectedMap();
                }));
            }
        }
        else
        {
            list.Add(new FloatMenuOption("Deeprim.NotWhileDrilling".Translate(), null));
        }

        return new FloatMenu(list);
    }
}