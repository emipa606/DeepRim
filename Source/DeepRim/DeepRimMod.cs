using System.Reflection.Emit;
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

    //References to the variables in DeepRimSettings
    public int MapSize {
        get => instance.DeepRimSettings.SpawnedMapSize;
        set => instance.DeepRimSettings.SpawnedMapSize = value;
        }
    public int OreDensity {
        get => instance.DeepRimSettings.OreDensity;
        set => instance.DeepRimSettings.OreDensity = value;
    }
        public int DepthValueBase {
        get => instance.DeepRimSettings.DepthValueBase;
        set => instance.DeepRimSettings.DepthValueBase = value;
    }
        public int DepthValueFalloff {
        get => instance.DeepRimSettings.DepthValueFalloff;
        set => instance.DeepRimSettings.DepthValueFalloff = value;
    }
    public bool NoPowerPreventsLiftUse {
        get => instance.DeepRimSettings.NoPowerPreventsLiftUse;
        set => instance.DeepRimSettings.NoPowerPreventsLiftUse = value;
    }
   
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
        listing_Standard.CheckboxLabeled("Deeprim.NoPowerPreventsLiftUse".Translate(), ref instance.DeepRimSettings.NoPowerPreventsLiftUse,
            "Deeprim.NoPowerPreventsLiftUseTT".Translate());

        listing_Standard.Gap();
        listing_Standard.CheckboxLabeled("Deeprim.VerboseLogging".Translate(),
            ref instance.DeepRimSettings.VerboseLogging,
            "Deeprim.VerboseLogging.Tooltip".Translate());
        listing_Standard.Gap(24);
        OreDensity = (int)listing_Standard.SliderLabeled("Deeprim.OreDensitySlider".Translate(OreDensity), OreDensity, 0, 500, 0.3f, "Deeprim.OreDensitySliderTT".Translate());
        listing_Standard.Gap();
        var label = MapSize >= 50 ? "Deeprim.MapSizeSlider".Translate(MapSize) : "Deeprim.MapSizeSlider".Translate("Deeprim.Inherited".Translate());
        MapSize = (int)listing_Standard.SliderLabeled(label, MapSize, 0, 500, 0.3f, "Deeprim.MapSizeSliderTT".Translate());
        label = "MapSizeDesc".Translate(MapSize, MapSize * MapSize);
        switch (MapSize)
        {
            case < 50:
                label = $"{"Deeprim.Inherited".Translate()} - {"Deeprim.Same".Translate()}";
                break;
            case < 75:
                label += $" - {"Deeprim.Little".Translate()}";
                break;
            case < 150:
                label += $" - {"Deeprim.Incident".Translate()}";
                break;
            case < 200:
                label += $" - {"MapSizeSmall".Translate()}";
                break;
            case < 250:
                label += $" - {"MapSizeMedium".Translate()}";
                break;
            case <= 350:
                label += $" - {"MapSizeLarge".Translate()}";
                break;
            case > 350:
                label += $" - {"MapSizeExtreme".Translate()}";
                break;
        }
        listing_Standard.Label(label);
        listing_Standard.Gap();
        DepthValueBase = (int)listing_Standard.SliderLabeled("Deeprim.DepthValueBaseSlider".Translate(DepthValueBase), DepthValueBase, 0, 100, 0.3f, "Deeprim.DepthValueBaseSliderTT".Translate());
        listing_Standard.Gap();
        DepthValueFalloff = (int)listing_Standard.SliderLabeled("Deeprim.DepthValueFalloffSlider".Translate(DepthValueFalloff), DepthValueFalloff, 0, 100, 0.3f, "Deeprim.DepthValueFalloffSliderTT".Translate());
        listing_Standard.Gap();

        var resetPlace = listing_Standard.GetRect(25f);
        if (Widgets.ButtonText(resetPlace.RightHalf().RightHalf().RightHalf(), "Deeprim.Reset".Translate()))
        {
            Reset();
        }
        listing_Standard.Gap();

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

    public static void LogWarn(string message, bool force = false){
        if (!force && !instance.DeepRimSettings.VerboseLogging)
        {
            return;
        }

        Log.Warning($"[DeepRim]: {message}");
    }

    public void Reset()
    {
        instance.DeepRimSettings.OreDensity = 16;
        DepthValueBase = 100;
        DepthValueFalloff = 0;
        OreDensity = 16;
        MapSize = 75;
        NoPowerPreventsLiftUse = true;
    }
}