using Verse;

namespace DeepRim;

/// <summary>
///     Definition of the deepRimSettings for the mod
/// </summary>
internal class DeepRimSettings : ModSettings
{
    public bool LowTechMode;
    public int SpawnedMapSize = 0;
    public bool VerboseLogging;
    public int OreDensity = 16;
    public int DepthValueBase = 100;
    public int DepthValueFalloff = 0;
    public bool NoPowerPreventsLiftUse = true;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OreDensity, "OreDensity", 16);
        Scribe_Values.Look(ref SpawnedMapSize, "SpawnedMapSize", 0);
        Scribe_Values.Look(ref DepthValueBase, "DepthValueBase", 1);
        Scribe_Values.Look(ref DepthValueFalloff, "DepthValueFalloff", 0);
        Scribe_Values.Look(ref NoPowerPreventsLiftUse, "NoPowerPreventsLiftUse", true);
        Scribe_Values.Look(ref LowTechMode, "LowTechMode", false);
        Scribe_Values.Look(ref VerboseLogging, "VerboseLogging", false);
    }
}