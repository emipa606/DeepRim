using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepRim;

public class Command_TransferLayer(Building building) : Command_Action
{
    public UndergroundManager manager;

    public Building_MiningShaft shaft;

    public Building_SpawnedLift lift;

    public override void ProcessInput(Event ev)
    {
        Find.WindowStack.Add(MakeMenu());
    }

    private FloatMenu MakeMenu()
    {
        switch (building)
        {
            case Building_MiningShaft:
            {
                var list = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Deeprim.None".Translate(), delegate { shaft.transferLevel = 0; })
                };
                var enumerator = manager.layersState.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;
                    if (pair.Value != null)
                    {
                        list.Add(new FloatMenuOption("Deeprim.SelectLayerAt".Translate(pair.Key),
                            delegate { shaft.transferLevel = pair.Key; }));

                    }
                }
                return new FloatMenu(list);
            }
            case Building_SpawnedLift:
            {
                var list = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Deeprim.None".Translate(), delegate { lift.TransferLevel = lift.depth; }),
                    new FloatMenuOption("Deeprim.Surface".Translate(), delegate { lift.TransferLevel = 0; })
                };
                var enumerator = manager.layersState.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;
                    if (pair.Value != null && pair.Key != lift.depth)
                    {
                        list.Add(new FloatMenuOption("Deeprim.SelectLayerAt".Translate(pair.Key),
                            delegate { lift.TransferLevel = pair.Key; }));
                    }
                }
                return new FloatMenu(list);
            }
            default:
                DeepRimMod.LogWarn("Transfer float menu did not recognize the building type");
                return null;
        }
    }
}