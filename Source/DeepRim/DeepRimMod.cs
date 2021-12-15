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


    /// <summary>
    ///     The private deepRimSettings
    /// </summary>
    private DeepRimSettings deepRimSettings;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public DeepRimMod(ModContentPack content) : base(content)
    {
        instance = this;
    }

    /// <summary>
    ///     The instance-deepRimSettings for the mod
    /// </summary>
    internal DeepRimSettings DeepRimSettings
    {
        get
        {
            if (deepRimSettings == null)
            {
                deepRimSettings = GetSettings<DeepRimSettings>();
            }

            return deepRimSettings;
        }
        set => deepRimSettings = value;
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
        listing_Standard.Label("Deeprim.MapSize".Translate());
        listing_Standard.Gap();
        foreach (var num in HarmonyPatches.mapSizes)
        {
            string label = "MapSizeDesc".Translate(num, num * num);
            switch (num)
            {
                case < 75:
                    listing_Standard.Label("Deeprim.Inherited".Translate());
                    label = "Deeprim.Same".Translate();
                    break;
                case < 200:
                    listing_Standard.Gap(10f);
                    listing_Standard.Label("Deeprim.Incident".Translate());
                    break;
                case < 250:
                    listing_Standard.Gap(10f);
                    listing_Standard.Label("MapSizeSmall".Translate());
                    break;
                case < 300:
                    listing_Standard.Gap(10f);
                    listing_Standard.Label("MapSizeMedium".Translate());
                    break;
                case < 350:
                    listing_Standard.Gap(10f);
                    listing_Standard.Label("MapSizeLarge".Translate());
                    break;
                default:
                    listing_Standard.Gap(10f);
                    listing_Standard.Label("MapSizeExtreme".Translate());
                    break;
            }

            if (listing_Standard.RadioButton(label, instance.DeepRimSettings.SpawnedMapSize == num))
            {
                instance.DeepRimSettings.SpawnedMapSize = num;
            }
        }

        listing_Standard.End();
    }
}