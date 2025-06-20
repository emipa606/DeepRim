using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DeepRim;

public class Command_TransferLayer(Building building) : Command_Action
{
    public Building_SpawnedLift lift;
    public UndergroundManager manager;

    public Building_MiningShaft shaft;

    public override void ProcessInput(Event ev)
    {
        Find.WindowStack.Add(makeMenu());
    }

    private FloatMenu makeMenu()
    {
        switch (building)
        {
            case Building_MiningShaft:
            {
                var list = new List<FloatMenuOption>
                {
                    new("Deeprim.None".Translate(), delegate { shaft.transferLevel = 0; })
                };
                var enumerator = manager.layersState.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;
                    if (pair.Value == null)
                    {
                        continue;
                    }

                    var name = manager.GetLayerName(pair.Key);
                    var label = name == ""
                        ? "Deeprim.UnnamedLayer".Translate(pair.Key).ToString()
                        : "Deeprim.LayerDepthNamed".Translate(pair.Key, manager.layerNames[pair.Key]).ToString();
                    list.Add(new FloatMenuOption(label,
                        delegate
                        {
                            shaft.transferLevel = pair.Key;
                            var spawnedLift = pair.Value.GetSpawnedLift();
                            if (spawnedLift is { TransferLevel: 0 })
                            {
                                spawnedLift.TransferLevel = pair.Key;
                            }
                        }));
                }

                return new FloatMenu(list);
            }
            case Building_SpawnedLift:
            {
                var list = new List<FloatMenuOption>
                {
                    new("Deeprim.None".Translate(), delegate { lift.TransferLevel = lift.depth; }),
                    new("Deeprim.Surface".Translate(), delegate
                    {
                        lift.TransferLevel = 0;
                        if (lift.parentDrill.transferLevel == lift.depth)
                        {
                            lift.parentDrill.transferLevel = 0;
                        }
                    })
                };
                var enumerator = manager.layersState.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;
                    if (pair.Value == null || pair.Key == lift.depth)
                    {
                        continue;
                    }

                    var name = manager.GetLayerName(pair.Key);
                    var label = name == ""
                        ? "Deeprim.UnnamedLayer".Translate(pair.Key).ToString()
                        : "Deeprim.LayerDepthNamed".Translate(pair.Key, manager.layerNames[pair.Key]).ToString();
                    list.Add(new FloatMenuOption(label,
                        delegate
                        {
                            lift.TransferLevel = pair.Key;
                            var spawnedLift = pair.Value.GetSpawnedLift();
                            if (spawnedLift != null && spawnedLift.TransferLevel == lift.depth)
                            {
                                spawnedLift.TransferLevel = pair.Key;
                            }
                        }));
                }

                return new FloatMenu(list);
            }
            default:
                DeepRimMod.LogWarn("Transfer float menu did not recognize the building type");
                return null;
        }
    }
}