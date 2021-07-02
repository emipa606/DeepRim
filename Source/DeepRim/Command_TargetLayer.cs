using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DeepRim
{
    // Token: 0x0200000A RID: 10
    public class Command_TargetLayer : Command_Action
    {
        // Token: 0x0400001F RID: 31
        public UndergroundManager manager;

        // Token: 0x04000020 RID: 32
        public Building_MiningShaft shaft;

        // Token: 0x06000039 RID: 57 RVA: 0x00003233 File Offset: 0x00001433
        public override void ProcessInput(Event ev)
        {
            Find.WindowStack.Add(MakeMenu());
        }

        // Token: 0x0600003A RID: 58 RVA: 0x00003248 File Offset: 0x00001448
        private FloatMenu MakeMenu()
        {
            var list = new List<FloatMenuOption>();
            if (shaft.CurMode != 1)
            {
                list.Add(new FloatMenuOption("New Layer", delegate
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
                        list.Add(new FloatMenuOption("Layer at Depth:" + pair.Key + "0m", delegate
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
                list.Add(new FloatMenuOption("Can't change target while drilling", null));
            }

            return new FloatMenu(list);
        }
    }
}