using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DeepRim;

[StaticConstructorOnStartup]
public class Building_MiningShaft : Building_ShaftLiftParent
{
    private const int updateEveryXTicks = 50;
    private const float baseExtraPower = 100;
    private const float idlePowerNeeded = 200;

    public float ChargeLevel;

    public Thing connectedLift;

    public Map connectedMap;

    private UndergroundMapParent connectedMapParent;

    private bool destroy;

    public bool drillNew = true;

    private float extraPower;
    private ThingComp m_Flick;
    private CompGlower m_Glow;

    public CompPowerTrader m_Power;

    private int mode;


    public int targetedLevel;

    private int ticksCounter;


    private UndergroundManager undergroundManager;


    public UndergroundManager UndergroundManager
    {
        get
        {
            if (undergroundManager == null)
            {
                undergroundManager = Map.components.Find(item => item is UndergroundManager) as UndergroundManager;
            }

            return undergroundManager;
        }
    }

    public float ConnectedMapMarketValue
    {
        get
        {
            float result;
            if (IsConnected)
            {
                result = Current.ProgramState != ProgramState.Playing ? 0f : connectedMap.wealthWatcher.WealthTotal;
            }
            else
            {
                result = 0f;
            }

            return result;
        }
    }

    public bool IsConnected => connectedMap != null && connectedMapParent != null && connectedLift != null;

    public int CurMode => mode;

    public UndergroundMapParent LinkedMapParent => connectedMapParent;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ChargeLevel, "ChargeLevel");
        Scribe_Values.Look(ref mode, "mode");
        Scribe_Values.Look(ref extraPower, "extraPower");
        Scribe_Values.Look(ref targetedLevel, "targetedLevel");
        Scribe_Values.Look(ref transferLevel, "transferLevel");
        Scribe_Values.Look(ref drillNew, "drillNew", true);
        Scribe_References.Look(ref connectedMap, "m_ConnectedMap");
        Scribe_References.Look(ref connectedMapParent, "m_ConnectedMapParent");
        Scribe_References.Look(ref connectedLift, "m_ConnectedLift");
    }

    public void tryReconfigureAll()
    {
        Log.Warning("Reset button was pressed!");
        Log.Warning("Iterating over maps to gather a list of buildings:");

        UndergroundManager?.layersState.Clear();
        var maps = Current.Game.maps;
        //Get lifts and shaft from maps
        foreach (var map in maps)
        {
            Log.Warning($"Current map: {map}");
            var lift = map.listerBuildings.AllBuildingsColonistOfClass<Building_SpawnedLift>().FirstOrDefault();
            if (lift != null)
            {
                lift.m_Flick.SwitchIsOn = true;
                if (!lift.m_Flick.SwitchIsOn)
                {
                    lift.m_Flick.DoFlick();
                }

                var mapParent = map.Parent as UndergroundMapParent;
                UndergroundManager?.layersState.Add(lift.depth, mapParent);
                Log.Warning($"Found lift: {lift}");
                continue;
            }

            Log.Warning("Map didn't contain an underground lift.");
        }

        if (UndergroundManager != null)
        {
            UndergroundManager.ActiveLayers = UndergroundManager.layersState.Count;
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var current in base.GetGizmos())
        {
            yield return current;
        }

        if (Prefs.DevMode)
        {
            yield return new Command_Action
            {
                action = tryReconfigureAll,
                defaultLabel = "Reset States",
                defaultDesc =
                    "This will attempt to reconnect all underground layers with one mineshaft. May fix some errors. Do not use if you have mineshafts on multiple maps."
            };
        }

        yield return new Command_TargetLayer
        {
            shaft = this,
            manager = Map.components.Find(item => item is UndergroundManager) as UndergroundManager,
            action = delegate { },
            defaultLabel = "Deeprim.ChangeTarget".Translate(),
            defaultDesc = drillNew
                ? "Deeprim.ChangeTargetNewTT".Translate()
                : "Deeprim.ChangeTargetExistingTT".Translate(targetedLevel * 10),
            icon = HarmonyPatches.UI_Option
        };

        if (NearbyStorages.Any())
        {
            yield return new Command_TransferLayer(this)
            {
                shaft = this,
                manager = Map.components.Find(item => item is UndergroundManager) as UndergroundManager,
                action = delegate { },
                defaultLabel = "Deeprim.ChangeTransferTarget".Translate(),
                defaultDesc = "Deeprim.ChangeTransferTargetTT".Translate(transferLevel * 10),
                icon = HarmonyPatches.UI_Transfer
            };
        }

        switch (mode)
        {
            case 0 when drillNew:
            {
                yield return new Command_Action
                {
                    action = StartDrilling,
                    defaultLabel = "Deeprim.StartDrilling".Translate(),
                    defaultDesc = drillNew
                        ? "Deeprim.StartDrillingNewTT".Translate()
                        : "Deeprim.StartDrillingExistingTT".Translate(),
                    icon = HarmonyPatches.UI_Start
                };
                break;
            }
            case 1:
            {
                yield return new Command_Action
                {
                    action = PauseDrilling,
                    defaultLabel = "Deeprim.PauseDrilling".Translate(),
                    defaultDesc = "Deeprim.PauseDrillingTT".Translate(),
                    icon = HarmonyPatches.UI_Pause
                };
                break;
            }
            default:
            {
                if (mode == 2 || !drillNew && mode != 3)
                {
                    yield return new Command_Action
                    {
                        action = PrepareToAbandon,
                        defaultLabel = "Deeprim.Abandon".Translate(),
                        defaultDesc = "Deeprim.AbandonTT".Translate(),
                        icon = HarmonyPatches.UI_Abandon
                    };
                }
                else
                {
                    if (mode == 3)
                    {
                        yield return new Command_Action
                        {
                            action = Abandon,
                            defaultLabel = "Deeprim.ConfirmAbandon".Translate(),
                            defaultDesc = "Deeprim.ConfirmAbandonTT".Translate(),
                            icon = HarmonyPatches.UI_Abandon
                        };
                    }
                }

                break;
            }
        }

        if (Prefs.DevMode && drillNew)
        {
            yield return new Command_Action
            {
                action = DrillNewLayer,
                defaultLabel = "DEV: Drill Now"
            };
        }

        if (!IsConnected || drillNew)
        {
            yield break;
        }

        yield return new Command_Action
        {
            action = Send,
            defaultLabel = "Deeprim.SendDown".Translate(),
            defaultDesc = "Deeprim.SendDownTT".Translate(connectedMapParent.depth * 10),
            icon = HarmonyPatches.UI_Send
        };

        yield return new Command_Action
        {
            action = BringUp,
            defaultLabel = "Deeprim.BringUp".Translate(),
            defaultDesc = "Deeprim.BringUpTopTT".Translate(connectedMapParent.depth * 10),
            icon = HarmonyPatches.UI_BringUp
        };

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            yield break;
        }

        if (m_Power?.PowerOn == true)
        {
            var lift = connectedLift as Building_SpawnedLift;
            yield return new Command_Toggle
            {
                icon = HarmonyPatches.UI_ToggleSendPower,
                defaultLabel = "Deeprim.SendPowerToLayer".Translate(),
                defaultDesc = "Deeprim.SendPowerToLayerTT".Translate(),
                isActive = () => lift != null && lift.m_Flick.SwitchIsOn,
                toggleAction = delegate
                {
                    lift?.TogglePower();
                    if (lift != null && lift.m_Flick.SwitchIsOn)
                    {
                        UndergroundManager.ActiveLayers++;
                        UndergroundManager.AnyLayersPowered = true;
                    }
                    else
                    {
                        UndergroundManager.ActiveLayers--;
                        if (UndergroundManager.ActiveLayers == 0)
                        {
                            UndergroundManager.AnyLayersPowered = false;
                        }
                    }
                }
            };
        }

        if (UndergroundManager.ActiveLayers <= 0)
        {
            yield break;
        }

        if (extraPower > 0)
        {
            yield return new Command_Action
            {
                action = () =>
                {
                    extraPower -= 100;
                    if (m_Power == null)
                    {
                        return;
                    }

                    m_Power.Props.basePowerConsumption = idlePowerNeeded + baseExtraPower + extraPower;
                    m_Power.SetUpPowerVars();
                },
                defaultLabel = "Deeprim.DecreasePower".Translate(),
                defaultDesc = "Deeprim.DecreasePowerTT".Translate(extraPower - 100),
                icon = HarmonyPatches.UI_DecreasePower
            };
        }

        yield return new Command_Action
        {
            action = () =>
            {
                extraPower += 100;
                if (m_Power == null)
                {
                    return;
                }

                m_Power.Props.basePowerConsumption = idlePowerNeeded + baseExtraPower + extraPower;
                m_Power.SetUpPowerVars();
            },
            defaultLabel = "Deeprim.IncreasePower".Translate(),
            defaultDesc = "Deeprim.IncreasePowerTT".Translate(extraPower + 100),
            icon = HarmonyPatches.UI_IncreasePower
        };
        yield return new Command_Action
        {
            icon = TexButton.Rename,
            defaultLabel = "Rename".Translate(),
            defaultDesc = "Deeprim.ChangeLayerNameAtDepth".Translate(targetedLevel),
            action = delegate
            {
                var manager = UndergroundManager;
                if (manager.layerNames.Count == 0 && manager.layersState.Count > 0)
                {
                    manager.InitLayerNames();
                }

                var dialog_RenameZone = new Dialog_RenameLayer((Building_SpawnedLift)connectedLift);
                Find.WindowStack.Add(dialog_RenameZone);
            }
        };
    }

    private void Abandon()
    {
        Abandon(false);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (UndergroundManager != null)
        {
            if (UndergroundManager.layersState.Any() && !destroy)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "Deeprim.AbandonAll".Translate(),
                    "Deeprim.AbandonAllNo".Translate(), null,
                    "Deeprim.AbandonAllYes".Translate(),
                    delegate
                    {
                        destroy = true;
                        Destroy(mode);
                    }));
                return;
            }

            foreach (var key in UndergroundManager.layersState.Keys.Reverse())
            {
                targetedLevel = key;
                Abandon(true);
            }

            UndergroundManager.NextLayer = 1;
            UndergroundManager.ActiveLayers = 0;
            UndergroundManager.AnyLayersPowered = false;
        }

        var originalValue = allowDestroyNonDestroyable;
        allowDestroyNonDestroyable = true;
        base.Destroy(mode);
        allowDestroyNonDestroyable = originalValue;
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        var nextLayer = UndergroundManager != null ? UndergroundManager.NextLayer * 10 : 10;
        string label;
        if (drillNew)
        {
            label = "Deeprim.TargetNewLayerAtDepth".Translate(nextLayer);
        }
        else if (UndergroundManager?.GetLayerName(targetedLevel) != "")
        {
            label = "Deeprim.TargetLayerAtNamed".Translate(targetedLevel,
                UndergroundManager?.GetLayerName(targetedLevel));
        }
        else
        {
            label = "Deeprim.TargetLayerAt".Translate(targetedLevel);
        }

        stringBuilder.AppendLine(label);

        if (NearbyStorages.Any())
        {
            var name = UndergroundManager?.GetLayerName(transferLevel);
            if (transferLevel == 0)
            {
                label = "Deeprim.TransferLevelNone".Translate();
            }
            else if (name != "")
            {
                label = "Deeprim.TransferTargetAtNamed".Translate(transferLevel, name);
            }
            else
            {
                label = "Deeprim.TransferTargetAt".Translate(transferLevel);
            }

            stringBuilder.AppendLine(label);
        }

        if (!DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            var powerSent = PowerAvailable();
            if (powerSent < 0)
            {
                stringBuilder.AppendLine("Deeprim.ExtraPowerSent".Translate(-powerSent));
            }
        }

        if (mode < 2)
        {
            stringBuilder.AppendLine("Deeprim.Progress".Translate(Math.Round(ChargeLevel)));
            stringBuilder.Append(base.GetInspectString());
        }
        else
        {
            stringBuilder.AppendLine("Deeprim.DrillingComplete".Translate(connectedMapParent.depth));
            stringBuilder.Append(base.GetInspectString());
        }

        return stringBuilder.ToString().Trim();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        m_Power = GetComp<CompPowerPlant>();
        m_Flick = GetComp<CompFlickable>();
        m_Glow = GetComp<CompGlower>();

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            if (m_Power != null)
            {
                DeepRimMod.LogMessage($"{this} had powercomp when it should not, removing");
                comps.Remove(m_Power);
            }

            if (m_Flick != null)
            {
                DeepRimMod.LogMessage($"{this} had flicker when it should not, removing");
                comps.Remove(m_Flick);
            }

            if (m_Glow != null)
            {
                DeepRimMod.LogMessage($"{this} had glower when it should not, removing");
                comps.Remove(m_Glow);
            }

            m_Power = null;
            m_Flick = null;
            m_Glow = null;

            if (stuffInt == null)
            {
                stuffInt = ThingDefOf.WoodLog;
            }

            return;
        }

        stuffInt = null;
    }

    private void StartDrilling()
    {
        mode = 1;
    }

    public void PauseDrilling()
    {
        mode = 0;
    }

    public float PowerAvailable()
    {
        if (m_Power?.PowerOn == false)
        {
            return 0;
        }

        if (Map.components.Find(item => item is UndergroundManager) is not UndergroundManager manager)
        {
            return 0;
        }

        if (manager.ActiveLayers == 0)
        {
            return 0;
        }

        var powerAvailable = 0f;
        if (mode != 1)
        {
            powerAvailable = baseExtraPower + extraPower;
        }

        if (powerAvailable < 0)
        {
            return 0;
        }

        return (float)Math.Round(powerAvailable / manager.ActiveLayers);
    }

    private void PrepareToAbandon()
    {
        mode = 3;
        Messages.Message("Deeprim.ConfirmAbandonAgain".Translate(), MessageTypeDefOf.RejectInput);
    }

    private void Abandon(bool force)
    {
        if (UndergroundManager == null)
        {
            return;
        }

        mode = 0;
        SyncConnectedMap();
        if (!DeepRimMod.instance.DeepRimSettings.LowTechMode && connectedLift is Building_SpawnedLift lift)
        {
            if (lift.m_Flick.SwitchIsOn)
            {
                UndergroundManager.ActiveLayers--;
                if (UndergroundManager.ActiveLayers == 0)
                {
                    UndergroundManager.AnyLayersPowered = false;
                }
            }
        }

        connectedMapParent?.AbandonLift(connectedLift, force);
        targetedLevel = -1;
        var originalValue = allowDestroyNonDestroyable;
        allowDestroyNonDestroyable = true;
        connectedLift?.Destroy();
        allowDestroyNonDestroyable = originalValue;
        UndergroundManager.DestroyLayer(connectedMapParent);
        connectedMap = null;
        connectedMapParent = null;
        connectedLift = null;
        drillNew = true;
    }

    private void DrillNewLayer()
    {
        Messages.Message("Deeprim.DrillingCompleteTT".Translate(), MessageTypeDefOf.PositiveEvent);
        var mapParent =
            (MapParent)WorldObjectMaker.MakeWorldObject(
                DefDatabase<WorldObjectDef>.GetNamed("UndergroundMapParent"));
        mapParent.Tile = Tile;
        Find.WorldObjects.Add(mapParent);
        connectedMapParent = (UndergroundMapParent)mapParent;
        var cellRect = this.OccupiedRect();
        var seedString = Find.World.info.seedString;
        Find.World.info.seedString = Rand.Range(0, 2147483646).ToString();
        var mapSize = Find.World.info.initialMapSize;
        if (DeepRimMod.instance.DeepRimSettings.SpawnedMapSize >= 50)
        {
            mapSize = new IntVec3(DeepRimMod.instance.DeepRimSettings.SpawnedMapSize, 1,
                DeepRimMod.instance.DeepRimSettings.SpawnedMapSize);
        }

        connectedMapParent.holeLocation = HarmonyPatches.ConvertParentDrillLocation(
            new IntVec3(cellRect.minX + 1, 0, cellRect.minZ + 1), Map.Size,
            mapSize);

        var mapGenerator = mapParent.MapGeneratorDef;
        var biomeToGenerate = HarmonyPatches.PossibleBiomeDefs.RandomElement();
        connectedMapParent.biome = biomeToGenerate;
        switch (biomeToGenerate.defName)
        {
            case "BMT_CrystalCaverns":
            case "BMT_EarthenDepths":
            case "BMT_FungalForest":
                DeepRimMod.LogMessage($"Generating {biomeToGenerate.defName}");
                mapGenerator = DefDatabase<MapGeneratorDef>.GetNamedSilentFail("Deep_BMT_Cavern");
                break;
            case "Cave":
                DeepRimMod.LogMessage("Generating DeepCave");
                mapGenerator = DefDatabase<MapGeneratorDef>.GetNamedSilentFail("DeepCave");
                break;
        }

        connectedMap = MapGenerator.GenerateMap(mapSize, mapParent, mapGenerator, mapParent.ExtraGenStepDefs);
        Find.World.info.seedString = seedString;
        connectedLift =
            GenSpawn.Spawn(ThingMaker.MakeThing(ShaftThingDefOf.undergroundlift, Stuff),
                connectedMapParent.holeLocation, connectedMap);
        connectedLift.SetFaction(Faction.OfPlayer);
        UndergroundManager?.InsertLayer(connectedMapParent);
        FloodFillerFog.FloodUnfog(connectedMapParent.holeLocation, connectedMap);
        if (connectedLift is Building_SpawnedLift lift)
        {
            lift.depth = connectedMapParent.depth;
            lift.surfaceMap = Map;
            lift.parentDrill = this;
            lift.RefreshComps();
        }
        else
        {
            Log.Warning(
                "Spawned lift isn't deeprim's lift. Someone's editing this mod! And doing it badly!!! Very badly.");
        }
    }

    private void FinishedDrill()
    {
        if (drillNew)
        {
            DrillNewLayer();
        }
        else
        {
            DrillToOldLayer();
        }
    }

    private void DrillToOldLayer()
    {
        connectedMapParent = UndergroundManager?.layersState[targetedLevel];
        connectedMap = UndergroundManager?.layersState[targetedLevel]?.Map;
        var cellRect = this.OccupiedRect();
        var intVec = new IntVec3(cellRect.minX + 1, 0, cellRect.minZ + 1);
        connectedLift =
            GenSpawn.Spawn(ThingMaker.MakeThing(ShaftThingDefOf.undergroundlift, Stuff), intVec,
                connectedMap);
        connectedLift.SetFaction(Faction.OfPlayer);
        FloodFillerFog.FloodUnfog(intVec, connectedMap);
        if (connectedLift is Building_SpawnedLift lift)
        {
            if (connectedMapParent != null)
            {
                lift.depth = connectedMapParent.depth;
            }
        }
        else
        {
            Log.Warning(
                "Spawned lift isn't deeprim's lift. Someone's editing this mod! And doing it badly!!! Very badly.");
        }
    }

    private void Send()
    {
        LiftUtils.StageSend(this);
    }

    private void BringUp()
    {
        if (m_Power is { PowerOn: false } && DeepRimMod.instance.DeepRimSettings.NoPowerPreventsLiftUse)
        {
            Messages.Message("Deeprim.NoPower".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        var lift = connectedLift as Building_SpawnedLift;
        LiftUtils.StageSend(lift, true);
    }

    public void SendFromStorages()
    {
        if (!NearbyStorages.Any())
        {
            return;
        }

        if (UndergroundManager?.layersState == null)
        {
            return;
        }

        if (transferLevel == 0)
        {
            return;
        }

        if (UndergroundManager?.layersState.ContainsKey(transferLevel) != true)
        {
            return;
        }

        Building_ShaftLiftParent targetLift = UndergroundManager.layersState[transferLevel]?.GetSpawnedLift();

        if (targetLift == null)
        {
            DeepRimMod.LogMessage("Found no spawned lift in targeted layer");
            return;
        }

        DeepRimMod.LogMessage($"Found {NearbyStorages.Count} storages to transfer items from");
        if (m_Power is not { PowerOn: false })
        {
            Transfer(targetLift, NearbyStorages.ToList());
        }
        else
        {
            DeepRimMod.LogMessage("Unpowered mining shaft refuses to send items");
        }
    }

    public void SyncConnectedMap()
    {
        connectedMapParent = UndergroundManager?.layersState[targetedLevel];
        connectedMap = UndergroundManager?.layersState[targetedLevel]?.Map;
        connectedLift = connectedMap?.listerBuildings.AllBuildingsColonistOfClass<Building_SpawnedLift>()
            .FirstOrDefault();
    }

    public void recountWealthSometimes()
    {
        if (Find.TickManager.TicksGame % 5000f == 0 && Current.ProgramState != ProgramState.MapInitializing)
        {
            Map.wealthWatcher.ForceRecount();
        }
    }

    public override void Tick()
    {
        base.Tick();
        if (connectedLift != null && ((Building_SpawnedLift)connectedLift).surfaceMap == null)
        {
            ((Building_SpawnedLift)connectedLift).surfaceMap = Map;
        }

        if (this.IsHashIntervalTick(GenTicks.TickRareInterval))
        {
            SendFromStorages();
        }

        if (!DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            //handle a case where the mod is updated in an existing save and ActiveLayers becomes 0 for some reason when it shouldn't be
            if (UndergroundManager.ActiveLayers == 0 && UndergroundManager.AnyLayersPowered)
            {
                DeepRimMod.LogWarn(
                    $"UndergroundManager.ActiveLayers was not initialized. Setting variable to {UndergroundManager.layersState.Count}.");
                UndergroundManager.ActiveLayers = UndergroundManager.layersState.Count;
                if (UndergroundManager.layersState.Count == 0)
                {
                    UndergroundManager.AnyLayersPowered = false;
                }
            }

            if (m_Power is { PowerOn: true } && UndergroundManager.ActiveLayers > 0
                                             && m_Power.Props.basePowerConsumption !=
                                             idlePowerNeeded + baseExtraPower + extraPower)
            {
                DeepRimMod.LogWarn("Updating power to ON state");
                m_Power.Props.basePowerConsumption = idlePowerNeeded + baseExtraPower + extraPower;
                m_Power.SetUpPowerVars();
            }
            else if (UndergroundManager.ActiveLayers < 1 && m_Power != null &&
                     m_Power.Props.basePowerConsumption != idlePowerNeeded)
            {
                DeepRimMod.LogWarn("Updating power to OFF state.");
                m_Power.Props.basePowerConsumption = idlePowerNeeded;
                m_Power.SetUpPowerVars();
            }
        }

        recountWealthSometimes();
        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            switch (ChargeLevel)
            {
                case < 100:
                    return;
                case < 200:
                    Messages.Message("Deeprim.GeneratingMap".Translate(), MessageTypeDefOf.PositiveEvent);
                    ChargeLevel = 200;
                    return;
            }

            ChargeLevel = 0;
            mode = 0;
            FinishedDrill();
            return;
        }

        if (mode == 1)
        {
            ticksCounter++;
        }

        if (ticksCounter < updateEveryXTicks)
        {
            return;
        }

        if (m_Power is { PowerOn: true })
        {
            FleckMaker.ThrowSmoke(DrawPos, Map, 1f);
            ticksCounter = 0;
        }
        else
        {
            switch (ChargeLevel)
            {
                case < 100:
                    return;
                case < 200:
                    Messages.Message("Deeprim.GeneratingMap".Translate(), MessageTypeDefOf.PositiveEvent);
                    ChargeLevel = 200;
                    return;
            }

            ChargeLevel = 0;
            mode = 0;
            FinishedDrill();
            return;
        }

        if (DebugSettings.unlimitedPower)
        {
            ChargeLevel += 20;
        }
        else
        {
            ChargeLevel++;
        }

        switch (ChargeLevel)
        {
            case < 100:
                return;
            case < 200:
                Messages.Message("Deeprim.GeneratingMap".Translate(), MessageTypeDefOf.PositiveEvent);
                ChargeLevel = 200;
                return;
        }

        ChargeLevel = 0;
        mode = 0;
        FinishedDrill();
    }
}