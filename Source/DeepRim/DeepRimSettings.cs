using Verse;

namespace DeepRim;

/// <summary>
///     Definition of the deepRimSettings for the mod
/// </summary>
internal class DeepRimSettings : ModSettings
{
    public bool LowTechMode;
    public int SpawnedMapSize;
    public bool VerboseLogging;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref SpawnedMapSize, "SpawnedMapSize");
        Scribe_Values.Look(ref LowTechMode, "LowTechMode");
        Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
    }
}