using Verse;

namespace DeepRim;

/// <summary>
///     Definition of the deepRimSettings for the mod
/// </summary>
internal class DeepRimSettings : ModSettings
{
    public int DepthValueBase = 100;
    public int DepthValueFalloff;
    public bool LowTechMode;
    public bool NoPowerPreventsLiftUse = true;
    public int OreDensity = 16;
    public int SpawnedMapSize;
    public bool VerboseLogging;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OreDensity, "OreDensity", 16);
        Scribe_Values.Look(ref SpawnedMapSize, "SpawnedMapSize");
        Scribe_Values.Look(ref DepthValueBase, "DepthValueBase", 1);
        Scribe_Values.Look(ref DepthValueFalloff, "DepthValueFalloff");
        Scribe_Values.Look(ref NoPowerPreventsLiftUse, "NoPowerPreventsLiftUse", true);
        Scribe_Values.Look(ref LowTechMode, "LowTechMode");
        Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
    }
}