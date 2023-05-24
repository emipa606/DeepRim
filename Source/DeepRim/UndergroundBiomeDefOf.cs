using RimWorld;

namespace DeepRim;

[DefOf]
public static class UndergroundBiomeDefOf
{
    public static BiomeDef Underground;

    static UndergroundBiomeDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BiomeDefOf));
    }
}