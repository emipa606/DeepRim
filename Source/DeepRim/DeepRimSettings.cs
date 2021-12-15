using Verse;

namespace DeepRim;

/// <summary>
///     Definition of the deepRimSettings for the mod
/// </summary>
internal class DeepRimSettings : ModSettings
{
    public int SpawnedMapSize;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref SpawnedMapSize, "SpawnedMapSize");
    }
}