using RimWorld;
using Verse;

namespace DeepRim;

[DefOf]
public static class ShaftThingDefOf
{
    public static ThingDef miningshaft;
    public static ThingDef undergroundlift;

    static ShaftThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
    }
}