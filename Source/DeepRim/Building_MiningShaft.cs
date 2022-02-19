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

    private static readonly Texture2D UI_Send = ContentFinder<Texture2D>.Get("UI/sendDown");

    public static readonly Texture2D UI_BringUp = ContentFinder<Texture2D>.Get("UI/bringUp");

    private static readonly Texture2D UI_Start = ContentFinder<Texture2D>.Get("UI/Start");

    private static readonly Texture2D UI_Pause = ContentFinder<Texture2D>.Get("UI/Pause");

    private static readonly Texture2D UI_Abandon = ContentFinder<Texture2D>.Get("UI/Abandon");

    private static readonly Texture2D UI_DrillDown;

    private static readonly Texture2D UI_DrillUp = ContentFinder<Texture2D>.Get("UI/drillup");

    private static readonly Texture2D UI_Option;

    public float ChargeLevel;

    private Thing connectedLift;

    public Map connectedMap;

    private UndergroundMapParent connectedMapParent;

    public bool drillNew = true;

    private CompPowerTrader m_Power;

    private int mode;

    public int targetedLevel;

    private int ticksCounter;

    static Building_MiningShaft()
    {
        UI_DrillDown = ContentFinder<Texture2D>.Get("UI/drilldown");
        UI_Option = ContentFinder<Texture2D>.Get("UI/optionsIcon");
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
        Scribe_Values.Look(ref targetedLevel, "targetedLevel");
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
            defaultLabel = "Change Target"
        };
        if (drillNew)
        {
            command.defaultDesc =
                "Toggle target between new layer and old layers. Currently, mining shaft is set to dig new layer.";
        }
        else
        {
            command.defaultDesc =
                "Toggle target between new layer and old layers. Currently, mining shaft is set to old layer, depth:" +
                targetedLevel;
        }

        command.icon = UI_Option;
        yield return command;
        if (mode == 0 && drillNew)
        {
            var command_ActionStart = new Command_Action
            {
                action = StartDrilling,
                defaultLabel = "Start Drilling",
                defaultDesc = drillNew
                    ? "Start drilling down to find a new suitable mining area."
                    : "Start drilling down towards existing mining area.",
                icon = UI_Start
            };

            yield return command_ActionStart;
        }
        else
        {
            if (mode == 1)
            {
                var command_ActionPause = new Command_Action
                {
                    action = PauseDrilling,
                    defaultLabel = "Pause Drilling",
                    defaultDesc = "Temporany pause drilling. Progress are kept.",
                    icon = UI_Pause
                };
                yield return command_ActionPause;
            }
            else
            {
                if (mode == 2 || !drillNew && mode != 3)
                {
                    var command_ActionAbandon = new Command_Action
                    {
                        action = PrepareToAbandon,
                        defaultLabel = "Abandon Layer",
                        defaultDesc =
                            "Abandon the layer. If there's no more shafts connected to it, all pawns and items in it is lost forever.",
                        icon = UI_Abandon
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
                            defaultLabel = "Confirm Abandon",
                            defaultDesc =
                                "This action is irreversible!!! If this is the only shaft to it, everything currently on that layer shall be lost forever, without any way of getting them back.",
                            icon = UI_Abandon
                        };
                        yield return command_ActionAbandon2;
                    }
                }
            }
        }

        if (!IsConnected)
        {
            yield break;
        }

        var send = new Command_Action
        {
            action = Send,
            defaultLabel = "Send Down",
            defaultDesc = "Send everything on the elavator down the shaft",
            icon = UI_Send
        };
        yield return send;
        var bringUp = new Command_Action
        {
            action = BringUp,
            defaultLabel = "Bring Up",
            defaultDesc = "Bring everything on the elavator up to the surface",
            icon = UI_BringUp
        };
        yield return bringUp;
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
        if (drillNew)
        {
            stringBuilder.AppendLine("Target: New layer");
        }
        else
        {
            stringBuilder.AppendLine(string.Concat(new object[]
            {
                "Target: Layer at depth ",
                targetedLevel,
                "0m"
            }));
        }

        if (mode < 2)
        {
            stringBuilder.AppendLine(string.Concat(new object[]
            {
                "Progress: ",
                Math.Round(ChargeLevel),
                "%"
            }));
            stringBuilder.Append(base.GetInspectString());
        }
        else
        {
            stringBuilder.AppendLine(string.Concat(new object[]
            {
                "Drilling complete. Depth: ",
                connectedMapParent.depth
            }));
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
    }

    private void StartDrilling()
    {
        mode = 1;
    }

    public void PauseDrilling()
    {
        mode = 0;
    }

    private void PrepareToAbandon()
    {
        mode = 3;
        Messages.Message("Click again to confirm. Once abandoned, everything in that layer is lost forever!",
            MessageTypeDefOf.RejectInput);
    }

    private void Abandon()
    {
        mode = 0;
        SyncConnectedMap();
        connectedMapParent?.AbandonLift(connectedLift);

        connectedLift.Destroy();
        var undergroundManager = Map.components.Find(item => item is UndergroundManager) as UndergroundManager;
        undergroundManager?.DestroyLayer(connectedMapParent);
        connectedMap = null;
        connectedMapParent = null;
        connectedLift = null;
        drillNew = true;
    }


    private void DrillNewLayer()
    {
        Messages.Message("Drilling complete", MessageTypeDefOf.PositiveEvent);
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

        connectedMap = MapGenerator.GenerateMap(mapSize, mapParent,
            mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs);
        Find.World.info.seedString = seedString;
        connectedLift =
            GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("undergroundlift"), Stuff),
                connectedMapParent.holeLocation, connectedMap);
        connectedLift.SetFaction(Faction.OfPlayer);
        var undergroundManager = Map.components.Find(item => item is UndergroundManager) as UndergroundManager;
        undergroundManager?.InsertLayer(connectedMapParent);
        if (connectedLift is Building_SpawnedLift lift)
        {
            lift.depth = connectedMapParent.depth;
            lift.surfaceMap = Map;
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
        var undergroundManager = Map.components.Find(item => item is UndergroundManager) as UndergroundManager;
        connectedMapParent = undergroundManager?.layersState[targetedLevel];
        connectedMap = undergroundManager?.layersState[targetedLevel]?.Map;
        var cellRect = this.OccupiedRect();
        var intVec = new IntVec3(cellRect.minX + 1, 0, cellRect.minZ + 1);
        connectedLift =
            GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("undergroundlift"), Stuff), intVec,
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
            Messages.Message("No power", MessageTypeDefOf.RejectInput);
            return;
        }

        Messages.Message("Sending down", MessageTypeDefOf.PositiveEvent);
        var cells = this.OccupiedRect().Cells;
        foreach (var intVec in cells)
        {
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
                GenSpawn.Spawn(thing, intVec, connectedMap);
            }
        }
    }

    private void BringUp()
    {
        if (m_Power is { PowerOn: false })
        {
            Messages.Message("No power", MessageTypeDefOf.RejectInput);
            return;
        }

        Messages.Message("Bringing Up", MessageTypeDefOf.PositiveEvent);
        var cells = connectedLift.OccupiedRect().Cells;
        foreach (var intVec in cells)
        {
            var thingList = intVec.GetThingList(connectedMap);
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
                GenSpawn.Spawn(thing, intVec, Map);
            }
        }
    }

    public void SyncConnectedMap()
    {
        var undergroundManager = Map.components.Find(item => item is UndergroundManager) as UndergroundManager;
        connectedMapParent = undergroundManager?.layersState[targetedLevel];
        connectedMap = undergroundManager?.layersState[targetedLevel]?.Map;
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

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            if (ChargeLevel < 100)
            {
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
            if (ChargeLevel < 100)
            {
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

        if (ChargeLevel < 100)
        {
            return;
        }

        ChargeLevel = 0;
        mode = 0;
        FinishedDrill();
    }
}