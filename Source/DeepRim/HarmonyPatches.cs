using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepRim
{
    // Token: 0x02000006 RID: 6
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        // Token: 0x04000018 RID: 24
        private static readonly Type patchType = typeof(HarmonyPatches);

        // Token: 0x06000027 RID: 39 RVA: 0x00002DF4 File Offset: 0x00000FF4
        static HarmonyPatches()
        {
            var harmonyInstance = new Harmony("com.deeprim.rimworld.mod");
            Log.Message("DeepRim: Adding Harmony patch ");
            harmonyInstance.Patch(AccessTools.Property(typeof(Thing), "MarketValue").GetGetMethod(false), null,
                new HarmonyMethod(patchType, "MarketValuePostfix"));
            harmonyInstance.Patch(AccessTools.Property(typeof(Map), "Biome").GetGetMethod(false), null,
                new HarmonyMethod(patchType, "MapBiomePostfix"));
        }

        // Token: 0x06000028 RID: 40 RVA: 0x00002E90 File Offset: 0x00001090
        private static void MarketValuePostfix(Thing __instance, ref float __result)
        {
            if (__instance is not Building_MiningShaft)
            {
                return;
            }

            var building_MiningShaft = (Building_MiningShaft) __instance;
            __result += building_MiningShaft.ConnectedMapMarketValue;
        }

        // Token: 0x06000029 RID: 41 RVA: 0x00002EC0 File Offset: 0x000010C0
        private static void MapBiomePostfix(Map __instance, ref BiomeDef __result)
        {
            if (__instance.ParentHolder is UndergroundMapParent)
            {
                __result = DefDatabase<BiomeDef>.GetNamed("Underground");
            }
        }
    }
}