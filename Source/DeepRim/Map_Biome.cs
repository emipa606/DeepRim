using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepRim;

[HarmonyPatch(typeof(Map), "Biome", MethodType.Getter)]
public static class Map_Biome
{
    private static void Postfix(Map __instance, ref BiomeDef __result)
    {
        if (__instance.ParentHolder is UndergroundMapParent mapParent)
        {
            __result = mapParent.biome;
        }
    }
}