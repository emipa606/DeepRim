using Mlie;
using UnityEngine;
using Verse;

namespace DeepRim;

[StaticConstructorOnStartup]
internal class DeepRimMod : Mod
{
    /// <summary>
    ///     The instance of the deepRimSettings to be read by the mod
    /// </summary>
    public static DeepRimMod instance;

    private static string currentVersion;


    /// <summary>
    ///     The private deepRimSettings
    /// </summary>
    public readonly DeepRimSettings DeepRimSettings;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public DeepRimMod(ModContentPack content) : base(content)
    {
        instance = this;

        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
        DeepRimSettings = GetSettings<DeepRimSettings>();
    }

    /// <summary>
    ///     The title for the mod-deepRimSettings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "DeepRim";
    }

    /// <summary>
    ///     The deepRimSettings-window
    ///     For more info: https://rimworldwiki.com/wiki/Modding_Tutorials/ModSettings
    /// </summary>
    /// <param name="rect"></param>
    public override void DoSettingsWindowContents(Rect rect)
    {
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(rect);

        if (Current.Game == null)
        {
            listing_Standard.CheckboxLabeled("Deeprim.Lowtech".Translate(), ref instance.DeepRimSettings.LowTechMode,
                "Deeprim.Lowtech.Tooltip".Translate());
        }
        else
        {
            listing_Standard.Label(
                instance.DeepRimSettings.LowTechMode
                    ? "Deeprim.LowtechInfo.Enabled".Translate()
                    : "Deeprim.LowtechInfo.Disabled".Translate(), -1,
                "Deeprim.LowtechInfo.Tooltip".Translate());
        }

        listing_Standard.Gap();
        listing_Standard.CheckboxLabeled("Deeprim.VerboseLogging".Translate(),
            ref instance.DeepRimSettings.VerboseLogging,
            "Deeprim.VerboseLogging.Tooltip".Translate());
        listing_Standard.Gap();
        listing_Standard.Label("Deeprim.MapSize".Translate());
        listing_Standard.Gap();
        foreach (var num in HarmonyPatches.mapSizes)
        {
            string label = "MapSizeDesc".Translate(num, num * num);
            switch (num)
            {
                case < 75:
                    label = $"{"Deeprim.Inherited".Translate()} - {"Deeprim.Same".Translate()}";
                    break;
                case < 200:
                    listing_Standard.Gap(10f);
                    label += $" - {"Deeprim.Incident".Translate()}";
                    break;
                case < 250:
                    listing_Standard.Gap(10f);
                    label += $" - {"MapSizeSmall".Translate()}";
                    break;
                case < 300:
                    listing_Standard.Gap(10f);
                    label += $" - {"MapSizeMedium".Translate()}";
                    break;
                case < 350:
                    listing_Standard.Gap(10f);
                    label += $" - {"MapSizeLarge".Translate()}";
                    break;
                default:
                    listing_Standard.Gap(10f);
                    label += $" - {"MapSizeExtreme".Translate()}";
                    break;
            }

            if (listing_Standard.RadioButton(label, instance.DeepRimSettings.SpawnedMapSize == num))
            {
                instance.DeepRimSettings.SpawnedMapSize = num;
            }
        }

        if (currentVersion != null)
        {
            listing_Standard.Gap();
            GUI.contentColor = Color.gray;
            listing_Standard.Label("Deeprim.CurrentModVersion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listing_Standard.End();
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        HarmonyPatches.RefreshDrillTechLevel();
    }

    public static void LogMessage(string message, bool force = false)
    {
        if (!force && !instance.DeepRimSettings.VerboseLogging)
        {
            return;
        }

        Log.Message($"[DeepRim]: {message}");
    }
}