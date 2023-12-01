using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DeepRim;

[StaticConstructorOnStartup]
public class Building_MiningShaft : Building
{
    private const int updateEveryXTicks = 50;

    private const float defaultPowerNeeded = 1200;
    private const float idlePowerNeeded = 200;


    public float ChargeLevel;

    private Thing connectedLift;

    public Map connectedMap;

    private UndergroundMapParent connectedMapParent;
    private HashSet<Building_Storage> connectedStorages = [];

    public bool drillNew = true;

    private float extraPower;

    private CompPowerTrader m_Power;

    private int mode;

    private HashSet<Building_Storage> nearbyStorages = [];

    public int targetedLevel;

    private int ticksCounter;

    public int transferLevel;

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

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var current in base.GetGizmos())
        {
            yield return current;
        }

        var command = new Command_TargetLayer
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

        yield return command;
        if (nearbyStorages.Any())
        {
            var transferCommand = new Command_TransferLayer
            {
                shaft = this,
                manager = Map.components.Find(item => item is UndergroundManager) as UndergroundManager,
                action = delegate { },
                defaultLabel = "Deeprim.ChangeTransferTarget".Translate(),
                defaultDesc = "Deeprim.ChangeTransferTargetTT".Translate(transferLevel * 10),
                icon = HarmonyPatches.UI_Transfer
            };
            yield return transferCommand;
        }

        switch (mode)
        {
            case 0 when drillNew:
            {
                var command_ActionStart = new Command_Action
                {
                    action = StartDrilling,
                    defaultLabel = "Deeprim.StartDrilling".Translate(),
                    defaultDesc = drillNew
                        ? "Deeprim.StartDrillingNewTT".Translate()
                        : "Deeprim.StartDrillingExistingTT".Translate(),
                    icon = HarmonyPatches.UI_Start
                };

                yield return command_ActionStart;
                break;
            }
            case 1:
            {
                var command_ActionPause = new Command_Action
                {
                    action = PauseDrilling,
                    defaultLabel = "Deeprim.PauseDrilling".Translate(),
                    defaultDesc = "Deeprim.PauseDrillingTT".Translate(),
                    icon = HarmonyPatches.UI_Pause
                };
                yield return command_ActionPause;
                break;
            }
            default:
            {
                if (mode == 2 || !drillNew && mode != 3)
                {
                    var command_ActionAbandon = new Command_Action
                    {
                        action = PrepareToAbandon,
                        defaultLabel = "Deeprim.Abandon".Translate(),
                        defaultDesc = "Deeprim.AbandonTT".Translate(),
                        icon = HarmonyPatches.UI_Abandon
                    };
                    yield return command_ActionAbandon;
                }
                else
                {
                    if (mode == 3)
                    {
                        var command_ActionAbandon2 = new Command_Action
                        {
                            action = Abandon,
                            defaultLabel = "Deeprim.ConfirmAbandon".Translate(),
                            defaultDesc = "Deeprim.ConfirmAbandonTT".Translate(),
                            icon = HarmonyPatches.UI_Abandon
                        };
                        yield return command_ActionAbandon2;
                    }
                }

                break;
            }
        }

        if (!IsConnected)
        {
            yield break;
        }

        var send = new Command_Action
        {
            action = Send,
            defaultLabel = "Deeprim.SendDown".Translate(),
            defaultDesc = "Deeprim.SendDownTT".Translate(connectedMapParent.depth * 10),
            icon = HarmonyPatches.UI_Send
        };
        yield return send;
        var bringUp = new Command_Action
        {
            action = BringUp,
            defaultLabel = "Deeprim.BringUp".Translate(),
            defaultDesc = "Deeprim.BringUpTopTT".Translate(connectedMapParent.depth * 10),
            icon = HarmonyPatches.UI_BringUp
        };
        yield return bringUp;

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            yield break;
        }

        if (extraPower > 0)
        {
            var decreasePower = new Command_Action
            {
                action = () => extraPower -= 100,
                defaultLabel = "Deeprim.DecreasePower".Translate(),
                defaultDesc = "Deeprim.DecreasePowerTT".Translate(extraPower - 100),
                icon = HarmonyPatches.UI_DecreasePower
            };
            yield return decreasePower;
        }

        var increasePower = new Command_Action
        {
            action = () => extraPower += 100,
            defaultLabel = "Deeprim.IncreasePower".Translate(),
            defaultDesc = "Deeprim.IncreasePowerTT".Translate(extraPower + 100),
            icon = HarmonyPatches.UI_IncreasePower
        };
        yield return increasePower;
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (IsConnected)
        {
            Abandon();
        }

        base.Destroy(mode);
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(drillNew
            ? "Deeprim.TargetNewLayer".Translate()
            : "Deeprim.TargetLayerAt".Translate(targetedLevel));

        if (transferLevel > 0 && nearbyStorages.Any())
        {
            stringBuilder.AppendLine("Deeprim.TransferTargetAt".Translate(transferLevel));
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
        if (def.HasComp(typeof(CompPowerTrader)))
        {
            m_Power = GetComp<CompPowerTrader>();
        }

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode && stuffInt == null)
        {
            stuffInt = ThingDefOf.WoodLog;
        }

        if (!DeepRimMod.instance.DeepRimSettings.LowTechMode && stuffInt != null)
        {
            stuffInt = null;
        }
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

        var currentActiveLayers = manager.layersState.Count;
        if (currentActiveLayers == 0)
        {
            return 0;
        }

        var powerAvailable = extraPower;
        if (mode != 1)
        {
            powerAvailable += defaultPowerNeeded - idlePowerNeeded;
        }

        if (powerAvailable < 0)
        {
            return 0;
        }

        return -(float)Math.Round(powerAvailable / currentActiveLayers);
    }

    private void PrepareToAbandon()
    {
        mode = 3;
        Messages.Message("Deeprim.ConfirmAbandonAgain".Translate(), MessageTypeDefOf.RejectInput);
    }

    private void Abandon()
    {
        mode = 0;
        SyncConnectedMap();
        connectedMapParent?.AbandonLift(connectedLift);

        connectedLift.Destroy();
        UndergroundManager?.DestroyLayer(connectedMapParent);
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
        if (DeepRimMod.instance.DeepRimSettings.SpawnedMapSize != 0)
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
            //default:
            //    if (Rand.Bool)
            //    {
            //        DeepRimMod.LogMessage("Generating caves");
            //        mapGenerator = DefDatabase<MapGeneratorDef>.GetNamedSilentFail("DeepCaveMap");
            //    }

            //    break;
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
        if (m_Power is { PowerOn: false })
        {
            Messages.Message("Deeprim.NoPower".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        var cells = this.OccupiedRect().Cells;
        var anythingSent = false;
        foreach (var intVec in cells)
        {
            var convertedLocation = HarmonyPatches.ConvertParentDrillLocation(
                intVec, Map.Size, connectedMap.Size);
            var thingList = intVec.GetThingList(Map);
            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < thingList.Count; index++)
            {
                var thing = thingList[index];
                if (thing is not Pawn &&
                    (thing is not ThingWithComps && thing == null || thing is Building))
                {
                    continue;
                }

                thing.DeSpawn();
                GenSpawn.Spawn(thing, convertedLocation, connectedMap);
                anythingSent = true;
            }
        }

        if (!anythingSent)
        {
            Messages.Message("Deeprim.NothingToSend".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        Messages.Message("Deeprim.SendingDown".Translate(), MessageTypeDefOf.PositiveEvent);
        if (!Event.current.control)
        {
            return;
        }

        Current.Game.CurrentMap = connectedMap;
        Find.Selector.Select(connectedLift);
    }

    private void Transfer(Map connectedTransferMap, IEnumerable<Building> transferLifts)
    {
        foreach (var storage in nearbyStorages)
        {
            var items = storage.GetSlotGroup().HeldThings;
            if (items == null || items.Any() == false)
            {
                DeepRimMod.LogMessage($"{storage} on surface has no items");
                continue;
            }

            var itemList = items.ToList();
            DeepRimMod.LogMessage($"{storage} has {itemList.Count} items, transfering underground");
            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < itemList.Count; index++)
            {
                var thing = itemList[index];
                thing.DeSpawn();
                GenSpawn.Spawn(thing, transferLifts.First().PositionHeld, connectedTransferMap);
            }
        }

        foreach (var storage in connectedStorages)
        {
            var items = storage.GetSlotGroup().HeldThings;
            if (items == null || items.Any() == false)
            {
                DeepRimMod.LogMessage($"{storage} in underground layer has no items");
                continue;
            }

            var itemList = items.ToList();
            DeepRimMod.LogMessage($"{storage} has {itemList.Count} items, transfering to surface");
            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < itemList.Count; index++)
            {
                var thing = itemList[index];
                thing.DeSpawn();
                GenSpawn.Spawn(thing, PositionHeld, Map);
            }
        }
    }

    private void BringUp()
    {
        if (m_Power is { PowerOn: false })
        {
            Messages.Message("Deeprim.NoPower".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        var cells = connectedLift.OccupiedRect().Cells;
        var anythingSent = false;
        foreach (var intVec in cells)
        {
            var thingList = intVec.GetThingList(connectedMap);
            var convertedLocation = HarmonyPatches.ConvertParentDrillLocation(
                intVec, Map.Size, connectedMap.Size);
            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < thingList.Count; index++)
            {
                var thing = thingList[index];
                if (thing is not Pawn &&
                    (thing is not ThingWithComps && thing is null || thing is Building))
                {
                    continue;
                }

                thing.DeSpawn();
                GenSpawn.Spawn(thing, convertedLocation, Map);
                anythingSent = true;
            }
        }

        if (!anythingSent)
        {
            Messages.Message("Deeprim.NothingToSend".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        Messages.Message("Deeprim.BringingUp".Translate(), MessageTypeDefOf.PositiveEvent);
    }

    public void SyncConnectedMap()
    {
        connectedMapParent = UndergroundManager?.layersState[targetedLevel];
        connectedMap = UndergroundManager?.layersState[targetedLevel]?.Map;
        connectedLift =
            (from Building_SpawnedLift lift in connectedMap?.listerBuildings.allBuildingsColonist
                where lift != null
                select lift).FirstOrDefault();
    }

    public override void Tick()
    {
        base.Tick();
        if (connectedLift != null && ((Building_SpawnedLift)connectedLift).surfaceMap == null)
        {
            ((Building_SpawnedLift)connectedLift).surfaceMap = Map;
        }

        if (!DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            m_Power.Props.basePowerConsumption = defaultPowerNeeded + extraPower;
        }

        if (GenTicks.TicksGame % GenTicks.TickRareInterval == 0)
        {
            nearbyStorages = [];
            foreach (var cell in this.OccupiedRect().AdjacentCells)
            {
                var building = cell.GetFirstBuilding(Map);
                switch (building)
                {
                    case null:
                        continue;
                    case Building_Storage storage:
                        nearbyStorages.Add(storage);
                        break;
                }
            }

            if (transferLevel > 0)
            {
                if (UndergroundManager?.layersState.ContainsKey(transferLevel) == true)
                {
                    var connectedTransferMap = UndergroundManager.layersState[transferLevel]?.Map;
                    if (connectedTransferMap != null)
                    {
                        connectedStorages = [];
                        var transferLifts =
                            connectedTransferMap.listerBuildings.AllBuildingsColonistOfDef(
                                ThingDef.Named("undergroundlift"));
                        if (!transferLifts.Any())
                        {
                            DeepRimMod.LogMessage("Found no spawned lift in targeted layer");
                        }
                        else
                        {
                            foreach (var buildingSpawnedLift in transferLifts)
                            {
                                var adjacentCells = buildingSpawnedLift?.OccupiedRect().AdjacentCells;
                                if (adjacentCells == null)
                                {
                                    continue;
                                }

                                foreach (var cell in adjacentCells)
                                {
                                    var building = cell.GetFirstBuilding(connectedTransferMap);
                                    switch (building)
                                    {
                                        case null:
                                            continue;
                                        case Building_Storage storage:
                                            connectedStorages.Add(storage);
                                            break;
                                    }
                                }
                            }


                            DeepRimMod.LogMessage($"Found {connectedStorages.Count} storages near underground shaft");

                            if (transferLevel > 0 && m_Power is not { PowerOn: false })
                            {
                                Transfer(connectedTransferMap, transferLifts);
                            }
                            else
                            {
                                DeepRimMod.LogMessage("Either shaft is not powered or no target-layer selected");
                            }
                        }
                    }
                    else
                    {
                        DeepRimMod.LogMessage("Found no connected map to transfer to");
                    }
                }
                else
                {
                    DeepRimMod.LogMessage("The selected target-layer does not exist");
                }
            }
        }

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

        if (m_Power.PowerOn)
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