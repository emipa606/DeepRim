using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace DeepRim
{
    // Token: 0x02000002 RID: 2
    [StaticConstructorOnStartup]
    public class Building_MiningShaft : Building
    {
        // Token: 0x04000009 RID: 9
        private const int updateEveryXTicks = 50;

        // Token: 0x04000001 RID: 1
        private static readonly Texture2D UI_Send = ContentFinder<Texture2D>.Get("UI/sendDown");

        // Token: 0x04000002 RID: 2
        public static readonly Texture2D UI_BringUp = ContentFinder<Texture2D>.Get("UI/bringUp");

        // Token: 0x04000003 RID: 3
        private static readonly Texture2D UI_Start = ContentFinder<Texture2D>.Get("UI/Start");

        // Token: 0x04000004 RID: 4
        private static readonly Texture2D UI_Pause = ContentFinder<Texture2D>.Get("UI/Pause");

        // Token: 0x04000005 RID: 5
        private static readonly Texture2D UI_Abandon = ContentFinder<Texture2D>.Get("UI/Abandon");

        // Token: 0x04000006 RID: 6
        private static readonly Texture2D UI_DrillDown;

        // Token: 0x04000007 RID: 7
        private static readonly Texture2D UI_DrillUp = ContentFinder<Texture2D>.Get("UI/drillup");

        // Token: 0x04000008 RID: 8
        private static readonly Texture2D UI_Option;

        // Token: 0x0400000F RID: 15
        private int ChargeLevel;

        // Token: 0x04000012 RID: 18
        private Thing connectedLift;

        // Token: 0x04000010 RID: 16
        public Map connectedMap;

        // Token: 0x04000011 RID: 17
        private UndergroundMapParent connectedMapParent;

        // Token: 0x0400000D RID: 13
        public bool drillNew = true;

        // Token: 0x0400000B RID: 11
        private CompPowerTrader m_Power;

        // Token: 0x0400000C RID: 12
        private int mode;

        // Token: 0x0400000E RID: 14
        public int targetedLevel;

        // Token: 0x0400000A RID: 10
        private int ticksCounter;

        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        static Building_MiningShaft()
        {
            UI_DrillDown = ContentFinder<Texture2D>.Get("UI/drilldown");
            UI_Option = ContentFinder<Texture2D>.Get("UI/optionsIcon");
        }

        // Token: 0x17000001 RID: 1
        // (get) Token: 0x06000011 RID: 17 RVA: 0x00002970 File Offset: 0x00000B70
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

        // Token: 0x17000002 RID: 2
        // (get) Token: 0x06000012 RID: 18 RVA: 0x000029C0 File Offset: 0x00000BC0
        public bool IsConnected => connectedMap != null && connectedMapParent != null && connectedLift != null;

        // Token: 0x17000003 RID: 3
        // (get) Token: 0x06000013 RID: 19 RVA: 0x000029F0 File Offset: 0x00000BF0
        public int CurMode => mode;

        // Token: 0x17000004 RID: 4
        // (get) Token: 0x06000014 RID: 20 RVA: 0x00002A08 File Offset: 0x00000C08
        public UndergroundMapParent LinkedMapParent => connectedMapParent;

        // Token: 0x06000002 RID: 2 RVA: 0x000020E0 File Offset: 0x000002E0
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

        // Token: 0x06000003 RID: 3 RVA: 0x00002177 File Offset: 0x00000377
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

        // Token: 0x06000004 RID: 4 RVA: 0x00002187 File Offset: 0x00000387
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (IsConnected)
            {
                Abandon();
            }

            base.Destroy(mode);
        }

        // Token: 0x06000005 RID: 5 RVA: 0x0000219C File Offset: 0x0000039C
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
                    ChargeLevel,
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

            return stringBuilder.ToString();
        }

        // Token: 0x06000006 RID: 6 RVA: 0x0000228C File Offset: 0x0000048C
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            m_Power = GetComp<CompPowerTrader>();
        }

        // Token: 0x06000007 RID: 7 RVA: 0x000022A4 File Offset: 0x000004A4
        private void StartDrilling()
        {
            mode = 1;
        }

        // Token: 0x06000008 RID: 8 RVA: 0x000022A4 File Offset: 0x000004A4
        public void PauseDrilling()
        {
            mode = 0;
        }

        // Token: 0x06000009 RID: 9 RVA: 0x000022AE File Offset: 0x000004AE
        private void PrepareToAbandon()
        {
            mode = 3;
            Messages.Message("Click again to confirm. Once abandoned, everything in that layer is lost forever!",
                MessageTypeDefOf.RejectInput);
        }

        // Token: 0x0600000A RID: 10 RVA: 0x000022CC File Offset: 0x000004CC
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

        // Token: 0x0600000B RID: 11 RVA: 0x00002324 File Offset: 0x00000524
        private void DrillNewLayer()
        {
            Messages.Message("Drilling complete", MessageTypeDefOf.PositiveEvent);
            var mapParent =
                (MapParent) WorldObjectMaker.MakeWorldObject(
                    DefDatabase<WorldObjectDef>.GetNamed("UndergroundMapParent"));
            mapParent.Tile = Tile;
            Find.WorldObjects.Add(mapParent);
            connectedMapParent = (UndergroundMapParent) mapParent;
            var cellRect = this.OccupiedRect();
            connectedMapParent.holeLocation = new IntVec3(cellRect.minX + 1, 0, cellRect.minZ + 1);
            var seedString = Find.World.info.seedString;
            Find.World.info.seedString = new Random().Next(0, 2147483646).ToString();
            connectedMap = MapGenerator.GenerateMap(Find.World.info.initialMapSize, mapParent,
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

        // Token: 0x0600000C RID: 12 RVA: 0x000024E0 File Offset: 0x000006E0
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

        // Token: 0x0600000D RID: 13 RVA: 0x0000250C File Offset: 0x0000070C
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

        // Token: 0x0600000E RID: 14 RVA: 0x0000261C File Offset: 0x0000081C
        private void Send()
        {
            if (!m_Power.PowerOn)
            {
                Messages.Message("No power", MessageTypeDefOf.RejectInput);
            }
            else
            {
                Messages.Message("Sending down", MessageTypeDefOf.PositiveEvent);
                var cells = this.OccupiedRect().Cells;
                foreach (var intVec in cells)
                {
                    var thingList = intVec.GetThingList(Map);
                    foreach (var thing1 in thingList)
                    {
                        if (thing1 is not Pawn &&
                            (thing1 is not ThingWithComps && thing1 == null || thing1 is Building))
                        {
                            continue;
                        }

                        var thing = thing1;
                        thing.DeSpawn();
                        GenSpawn.Spawn(thing, intVec, connectedMap);
                    }
                }
            }
        }

        // Token: 0x0600000F RID: 15 RVA: 0x00002748 File Offset: 0x00000948
        private void BringUp()
        {
            if (!m_Power.PowerOn)
            {
                Messages.Message("No power", MessageTypeDefOf.RejectInput);
            }
            else
            {
                Messages.Message("Bringing Up", MessageTypeDefOf.PositiveEvent);
                var cells = connectedLift.OccupiedRect().Cells;
                foreach (var intVec in cells)
                {
                    var thingList = intVec.GetThingList(connectedMap);
                    foreach (var thing1 in thingList)
                    {
                        //Log.Warning(string.Concat(new object[]
                        //{
                        //    "Test ",
                        //    i,
                        //    " ",
                        //    thingList[i],
                        //    " ",
                        //    thingList[i].GetType()
                        //}), false);
                        if (thing1 is not Pawn &&
                            (thing1 is not ThingWithComps && thing1 is not Thing || thing1 is Building))
                        {
                            continue;
                        }

                        var thing = thing1;
                        thing.DeSpawn();
                        GenSpawn.Spawn(thing, intVec, Map);
                    }
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

        // Token: 0x06000010 RID: 16 RVA: 0x000028B8 File Offset: 0x00000AB8
        public override void Tick()
        {
            base.Tick();
            if (connectedLift != null && ((Building_SpawnedLift) connectedLift).surfaceMap == null)
            {
                ((Building_SpawnedLift) connectedLift).surfaceMap = Map;
            }

            if (mode == 1)
            {
                ticksCounter++;
            }

            if (!m_Power.PowerOn)
            {
                return;
            }

            if (ticksCounter < updateEveryXTicks)
            {
                return;
            }

            MoteMaker.ThrowSmoke(DrawPos, Map, 1f);
            ticksCounter = 0;
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
}