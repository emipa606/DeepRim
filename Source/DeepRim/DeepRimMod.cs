using System.Reflection;
using HarmonyLib;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepRim;

[StaticConstructorOnStartup]
internal class DeepRimMod : Mod
{
    /// <summary>
    ///     The instance of the deepRimSettings to be read by the mod
    /// </summary>
    public static DeepRimMod Instance;

    private static string currentVersion;


    public static readonly FieldInfo CompsFieldInfo = AccessTools.Field(typeof(ThingWithComps), "comps");
    public static readonly FieldInfo StuffIntFieldInfo = AccessTools.Field(typeof(Thing), "stuffInt");
    public static readonly FieldInfo MapFieldInfo = AccessTools.Field(typeof(MapComponent), "map");

    public static readonly FieldInfo BasePowerConsumptionFieldInfo =
        AccessTools.Field(typeof(CompProperties_Power), "basePowerConsumption");

    public static readonly MethodInfo InitMethodInfo = AccessTools.Method(typeof(GraphicData), "Init");

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
        Instance = this;

        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
        DeepRimSettings = GetSettings<DeepRimSettings>();
    }

    //References to the variables in DeepRimSettings
    private static int MapSize
    {
        get => Instance.DeepRimSettings.SpawnedMapSize;
        set => Instance.DeepRimSettings.SpawnedMapSize = value;
    }

    private static int OreDensity
    {
        get => Instance.DeepRimSettings.OreDensity;
        set => Instance.DeepRimSettings.OreDensity = value;
    }

    private static int DepthValueBase
    {
        get => Instance.DeepRimSettings.DepthValueBase;
        set => Instance.DeepRimSettings.DepthValueBase = value;
    }

    private static int DepthValueFalloff
    {
        get => Instance.DeepRimSettings.DepthValueFalloff;
        set => Instance.DeepRimSettings.DepthValueFalloff = value;
    }

    public static bool NoPowerPreventsLiftUse
    {
        get => !Instance.DeepRimSettings.LowTechMode && Instance.DeepRimSettings.NoPowerPreventsLiftUse;
        private set => Instance.DeepRimSettings.NoPowerPreventsLiftUse = value;
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
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(rect);

        if (Current.Game == null)
        {
            listingStandard.CheckboxLabeled("Deeprim.Lowtech".Translate(), ref Instance.DeepRimSettings.LowTechMode,
                "Deeprim.Lowtech.Tooltip".Translate());
        }
        else
        {
            listingStandard.Label(
                Instance.DeepRimSettings.LowTechMode
                    ? "Deeprim.LowtechInfo.Enabled".Translate()
                    : "Deeprim.LowtechInfo.Disabled".Translate(), -1,
                "Deeprim.LowtechInfo.Tooltip".Translate());
        }

        if (!Instance.DeepRimSettings.LowTechMode)
        {
            listingStandard.CheckboxLabeled("Deeprim.NoPowerPreventsLiftUse".Translate(),
                ref Instance.DeepRimSettings.NoPowerPreventsLiftUse,
                "Deeprim.NoPowerPreventsLiftUseTT".Translate());
        }

        listingStandard.Gap();
        listingStandard.CheckboxLabeled("Deeprim.VerboseLogging".Translate(),
            ref Instance.DeepRimSettings.VerboseLogging,
            "Deeprim.VerboseLogging.Tooltip".Translate());
        listingStandard.Gap(24);
        OreDensity = (int)listingStandard.SliderLabeled("Deeprim.OreDensitySlider".Translate(OreDensity), OreDensity,
            0, 500, 0.3f, "Deeprim.OreDensitySliderTT".Translate());
        listingStandard.Gap();
        var label = MapSize >= 50
            ? "Deeprim.MapSizeSlider".Translate(MapSize)
            : "Deeprim.MapSizeSlider".Translate("Deeprim.Inherited".Translate());
        MapSize = (int)listingStandard.SliderLabeled(label, MapSize, 0, 500, 0.3f,
            "Deeprim.MapSizeSliderTT".Translate());
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

        listingStandard.Label(label);
        listingStandard.Gap();
        DepthValueBase = (int)listingStandard.SliderLabeled("Deeprim.DepthValueBaseSlider".Translate(DepthValueBase),
            DepthValueBase, 0, 100, 0.3f, "Deeprim.DepthValueBaseSliderTT".Translate());
        listingStandard.Gap();
        DepthValueFalloff = (int)listingStandard.SliderLabeled(
            "Deeprim.DepthValueFalloffSlider".Translate(DepthValueFalloff), DepthValueFalloff, 0, 100, 0.3f,
            "Deeprim.DepthValueFalloffSliderTT".Translate());
        listingStandard.Gap();

        var resetPlace = listingStandard.GetRect(25f);
        if (Widgets.ButtonText(resetPlace.RightHalf().RightHalf().RightHalf(), "Deeprim.Reset".Translate()))
        {
            reset();
        }

        listingStandard.Gap();

        if (currentVersion != null)
        {
            listingStandard.Gap();
            GUI.contentColor = Color.gray;
            listingStandard.Label("Deeprim.CurrentModVersion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listingStandard.End();
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        HarmonyPatches.RefreshDrillTechLevel();
    }

    public static void LogMessage(string message, bool force = false)
    {
        if (!force && !Instance.DeepRimSettings.VerboseLogging)
        {
            return;
        }

        Log.Message($"[DeepRim]: {message}");
    }

    public static void LogWarn(string message, bool force = false)
    {
        if (!force && !Instance.DeepRimSettings.VerboseLogging)
        {
            return;
        }

        Log.Warning($"[DeepRim]: {message}");
    }

    private static void reset()
    {
        Instance.DeepRimSettings.OreDensity = 16;
        DepthValueBase = 100;
        DepthValueFalloff = 0;
        OreDensity = 16;
        MapSize = 75;
        NoPowerPreventsLiftUse = true;
    }
}