using System.Collections.Generic;
using Verse;

namespace DeepRim
{
    // Token: 0x02000009 RID: 9
    public class UndergroundManager : MapComponent
    {
        // Token: 0x0400001A RID: 26
        private const int targetversion = 1;

        // Token: 0x0400001C RID: 28
        public Dictionary<int, UndergroundMapParent> layersState = new Dictionary<int, UndergroundMapParent>();

        // Token: 0x0400001D RID: 29
        private List<int> list2;

        // Token: 0x0400001E RID: 30
        private List<UndergroundMapParent> list3;

        // Token: 0x0400001B RID: 27
        private int spawned;

        // Token: 0x06000030 RID: 48 RVA: 0x00002FEB File Offset: 0x000011EB
        public UndergroundManager(Map map) : base(map)
        {
        }

        // Token: 0x06000031 RID: 49 RVA: 0x00003008 File Offset: 0x00001208
        public int GetNextEmptyLayer(int starting = 1)
        {
            var num = starting;
            while (layersState.ContainsKey(num))
            {
                num++;
            }

            return num;
        }

        // Token: 0x06000032 RID: 50 RVA: 0x00003038 File Offset: 0x00001238
        public int GetNextLayer(int starting = 1)
        {
            var num = starting;
            while (layersState.ContainsKey(num))
            {
                num++;
            }

            return num - 1;
        }

        // Token: 0x06000033 RID: 51 RVA: 0x00003068 File Offset: 0x00001268
        public void InsertLayer(UndergroundMapParent mp)
        {
            var nextEmptyLayer = GetNextEmptyLayer();
            layersState.Add(nextEmptyLayer, mp);
            mp.depth = nextEmptyLayer;
        }

        // Token: 0x06000034 RID: 52 RVA: 0x00003094 File Offset: 0x00001294
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

        // Token: 0x06000035 RID: 53 RVA: 0x00003158 File Offset: 0x00001358
        public void DestroyLayer(UndergroundMapParent layer)
        {
            var depth = layer.depth;
            if (depth == -1)
            {
                Log.Error("Destroyed layer doesn't have correct depth");
            }

            layersState.Remove(depth);
        }

        // Token: 0x06000036 RID: 54 RVA: 0x00003190 File Offset: 0x00001390
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref spawned, "spawned");
            Scribe_Collections.Look(ref layersState, "layers", LookMode.Value, LookMode.Reference, ref list2,
                ref list3);
            Scribe_References.Look(ref map, "map");
        }

        // Token: 0x06000037 RID: 55 RVA: 0x000031EC File Offset: 0x000013EC
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
}