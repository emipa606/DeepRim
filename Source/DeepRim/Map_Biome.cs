using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepRim;

[HarmonyPatch(typeof(Map), "Biome")]
public static class Map_Biome
{
    private static void MapBiomePostfix(Map __instance, ref BiomeDef __result)
    {
        if (__instance.ParentHolder is UndergroundMapParent mapParent)
        {
            __result = BiomeDef.Named(mapParent.biome);
        }
    }
}