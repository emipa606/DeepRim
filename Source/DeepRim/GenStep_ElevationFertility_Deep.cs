using UnityEngine;
using Verse;
using Verse.Noise;

namespace DeepRim
{
    // Token: 0x0200000B RID: 11
    public class GenStep_ElevationFertility_Deep : GenStep
    {
        // Token: 0x04000021 RID: 33
        private const float ElevationFreq = 0.021f;

        // Token: 0x04000022 RID: 34
        private const float FertilityFreq = 0.021f;

        // Token: 0x04000023 RID: 35
        private const float EdgeMountainSpan = 0.42f;

        // Token: 0x17000008 RID: 8
        // (get) Token: 0x0600003C RID: 60 RVA: 0x00003388 File Offset: 0x00001588
        public override int SeedPart => 9285618;

        // Token: 0x0600003D RID: 61 RVA: 0x000033A0 File Offset: 0x000015A0
        public override void Generate(Map map, GenStepParams parms)
        {
            NoiseRenderer.renderSize = new IntVec2(map.Size.x, map.Size.z);
            ModuleBase moduleBase = new Perlin(0.0209999997168779, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue),
                QualityMode.High);
            moduleBase = new ScaleBias(0.5, 0.5, moduleBase);
            NoiseDebugUI.StoreNoiseRender(moduleBase, "elev base");
            var num = 1.8f;
            moduleBase = new Multiply(moduleBase, new Const(num));
            NoiseDebugUI.StoreNoiseRender(moduleBase, "elev world-factored");
            ModuleBase moduleBase2 = new DistFromAxis(map.Size.x * 0.42f);
            moduleBase2 = new Clamp(0.0, 1.0, moduleBase2);
            moduleBase2 = new Invert(moduleBase2);
            moduleBase2 = new ScaleBias(1.0, 1.0, moduleBase2);
            Rot4 random;
            do
            {
                random = Rot4.Random;
            } while (random == Find.World.CoastDirectionAt(map.Tile));

            if (random == Rot4.North)
            {
                moduleBase2 = new Rotate(0.0, 90.0, 0.0, moduleBase2);
                moduleBase2 = new Translate(0.0, 0.0, -(double) map.Size.z, moduleBase2);
            }
            else
            {
                if (random == Rot4.East)
                {
                    moduleBase2 = new Translate(-(double) map.Size.x, 0.0, 0.0, moduleBase2);
                }
                else
                {
                    if (random == Rot4.South)
                    {
                        moduleBase2 = new Rotate(0.0, 90.0, 0.0, moduleBase2);
                    }
                }
            }

            NoiseDebugUI.StoreNoiseRender(moduleBase2, "mountain");
            moduleBase = new Add(moduleBase, moduleBase2);
            NoiseDebugUI.StoreNoiseRender(moduleBase, "elev + mountain");
            var num2 = !map.TileInfo.WaterCovered ? 3.402823E+38f : 0f;
            var elevation = MapGenerator.Elevation;
            foreach (var intVec in map.AllCells)
            {
                elevation[intVec] = Mathf.Min(moduleBase.GetValue(intVec), num2);
            }

            var fertility = MapGenerator.Fertility;
            foreach (var c in map.AllCells)
            {
                fertility[c] = 0f;
            }
        }
    }
}