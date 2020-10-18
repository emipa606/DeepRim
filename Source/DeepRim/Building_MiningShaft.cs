using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DeepRim
{
    // Token: 0x02000002 RID: 2
    [StaticConstructorOnStartup]
    public class Building_MiningShaft : Building
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        static Building_MiningShaft()
        {
            Building_MiningShaft.UI_DrillDown = ContentFinder<Texture2D>.Get("UI/drilldown", true);
            Building_MiningShaft.UI_Option = ContentFinder<Texture2D>.Get("UI/optionsIcon", true);
        }

        // Token: 0x06000002 RID: 2 RVA: 0x000020E0 File Offset: 0x000002E0
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref ChargeLevel, "ChargeLevel", 0, false);
            Scribe_Values.Look<int>(ref mode, "mode", 0, false);
            Scribe_Values.Look<int>(ref targetedLevel, "targetedLevel", 0, false);
            Scribe_Values.Look<bool>(ref drillNew, "drillNew", true, false);
            Scribe_References.Look<Map>(ref connectedMap, "m_ConnectedMap", false);
            Scribe_References.Look<UndergroundMapParent>(ref connectedMapParent, "m_ConnectedMapParent", false);
            Scribe_References.Look<Thing>(ref connectedLift, "m_ConnectedLift", false);
        }

        // Token: 0x06000003 RID: 3 RVA: 0x00002177 File Offset: 0x00000377
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo current in base.GetGizmos())
            {
                yield return current;
            }
            Command_TargetLayer command = new Command_TargetLayer();
            command.shaft = this;
            command.manager = (base.Map.components.Find((MapComponent item) => item is UndergroundManager) as UndergroundManager);
            command.action = delegate ()
            {
            };
            command.defaultLabel = "Change Target";
            bool flag = drillNew;
            if (flag)
            {
                command.defaultDesc = "Toggle target between new layer and old layers. Currently, mining shaft is set to dig new layer.";
            }
            else
            {
                command.defaultDesc = "Toggle target between new layer and old layers. Currently, mining shaft is set to old layer, depth:" + targetedLevel;
            }
            command.icon = Building_MiningShaft.UI_Option;
            yield return command;
            bool flag2 = mode == 0;
            if (flag2 && flag)
            {
                Command_Action command_ActionStart = new Command_Action();
                command_ActionStart.action = new Action(StartDrilling);
                command_ActionStart.defaultLabel = "Start Drilling";
                bool flag3 = drillNew;
                if (flag3)
                {
                    command_ActionStart.defaultDesc = "Start drilling down to find a new suitable mining area.";
                }
                else
                {
                    command_ActionStart.defaultDesc = "Start drilling down towards existing mining area.";
                }
                command_ActionStart.icon = Building_MiningShaft.UI_Start;
                yield return command_ActionStart;
                command_ActionStart = null;
            }
            else
            {
                bool flag4 = mode == 1;
                if (flag4)
                {
                    Command_Action command_ActionPause = new Command_Action();
                    command_ActionPause.action = new Action(PauseDrilling);
                    command_ActionPause.defaultLabel = "Pause Drilling";
                    command_ActionPause.defaultDesc = "Temporany pause drilling. Progress are kept.";
                    command_ActionPause.icon = Building_MiningShaft.UI_Pause;
                    yield return command_ActionPause;
                    command_ActionPause = null;
                }
                else
                {
                    bool flag5 = mode == 2;
                    if (flag5 || (!flag && mode != 3))
                    {
                        Command_Action command_ActionAbandon = new Command_Action();
                        command_ActionAbandon.action = new Action(PrepareToAbandon);
                        command_ActionAbandon.defaultLabel = "Abandon Layer";
                        command_ActionAbandon.defaultDesc = "Abandon the layer. If there's no more shafts connected to it, all pawns and items in it is lost forever.";
                        command_ActionAbandon.icon = Building_MiningShaft.UI_Abandon;
                        yield return command_ActionAbandon;
                        command_ActionAbandon = null;
                    }
                    else
                    {
                        bool flag6 = mode == 3;
                        if (flag6)
                        {
                            Command_Action command_ActionAbandon2 = new Command_Action();
                            command_ActionAbandon2.action = new Action(Abandon);
                            command_ActionAbandon2.defaultLabel = "Confirm Abandon";
                            command_ActionAbandon2.defaultDesc = "This action is irreversible!!! If this is the only shaft to it, everything currently on that layer shall be lost forever, without any way of getting them back.";
                            command_ActionAbandon2.icon = Building_MiningShaft.UI_Abandon;
                            yield return command_ActionAbandon2;
                            command_ActionAbandon2 = null;
                        }
                    }
                }
            }
            bool isConnected = this.isConnected;
            if (isConnected)
            {
                Command_Action send = new Command_Action();
                send.action = new Action(Send);
                send.defaultLabel = "Send Down";
                send.defaultDesc = "Send everything on the elavator down the shaft";
                send.icon = Building_MiningShaft.UI_Send;
                yield return send;
                Command_Action bringUp = new Command_Action();
                bringUp.action = new Action(BringUp);
                bringUp.defaultLabel = "Bring Up";
                bringUp.defaultDesc = "Bring everything on the elavator up to the surface";
                bringUp.icon = Building_MiningShaft.UI_BringUp;
                yield return bringUp;
                send = null;
                bringUp = null;
            }
            yield break;
        }

        // Token: 0x06000004 RID: 4 RVA: 0x00002187 File Offset: 0x00000387
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (isConnected)
                Abandon();
            base.Destroy(mode);
        }

        // Token: 0x06000005 RID: 5 RVA: 0x0000219C File Offset: 0x0000039C
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool flag = drillNew;
            if (flag)
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
            bool flag2 = mode < 2;
            if (flag2)
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
            m_Power = base.GetComp<CompPowerTrader>();
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
            Messages.Message("Click again to confirm. Once abandoned, everything in that layer is lost forever!", MessageTypeDefOf.RejectInput, true);
        }

        // Token: 0x0600000A RID: 10 RVA: 0x000022CC File Offset: 0x000004CC
        private void Abandon()
        {
            mode = 0;
            drillNew = true;
            bool flag = connectedMapParent != null;
            if (flag)
            {
                connectedMapParent.abandonLift(connectedLift);
            }
            connectedLift.Destroy(DestroyMode.Vanish);
            connectedMap = null;
            connectedMapParent = null;
            connectedLift = null;
        }

        // Token: 0x0600000B RID: 11 RVA: 0x00002324 File Offset: 0x00000524
        private void DrillNewLayer()
        {
            Messages.Message("Drilling complete", MessageTypeDefOf.PositiveEvent, true);
            MapParent mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("UndergroundMapParent", true));
            mapParent.Tile = base.Tile;
            Find.WorldObjects.Add(mapParent);
            connectedMapParent = (UndergroundMapParent)mapParent;
            CellRect cellRect = this.OccupiedRect();
            connectedMapParent.holeLocation = new IntVec3(cellRect.minX + 1, 0, cellRect.minZ + 1);
            string seedString = Find.World.info.seedString;
            Find.World.info.seedString = new System.Random().Next(0, 2147483646).ToString();
            connectedMap = MapGenerator.GenerateMap(Find.World.info.initialMapSize, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs, null);
            Find.World.info.seedString = seedString;
            connectedLift = GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("undergroundlift", true), base.Stuff), connectedMapParent.holeLocation, connectedMap, WipeMode.Vanish);
            connectedLift.SetFaction(Faction.OfPlayer, null);
            UndergroundManager undergroundManager = base.Map.components.Find((MapComponent item) => item is UndergroundManager) as UndergroundManager;
            undergroundManager.insertLayer(connectedMapParent);
            bool flag = connectedLift is Building_SpawnedLift;
            if (flag)
            {
                ((Building_SpawnedLift)connectedLift).depth = connectedMapParent.depth;
                ((Building_SpawnedLift)connectedLift).surfaceMap = Map;
            }
            else
            {
                Log.Warning("Spawned lift isn't deeprim's lift. Someone's editing this mod! And doing it badly!!! Very badly.", false);
            }
        }

        // Token: 0x0600000C RID: 12 RVA: 0x000024E0 File Offset: 0x000006E0
        private void FinishedDrill()
        {
            bool flag = drillNew;
            if (flag)
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
            UndergroundManager undergroundManager = base.Map.components.Find((MapComponent item) => item is UndergroundManager) as UndergroundManager;
            UndergroundMapParent undergroundMapParent = undergroundManager.layersState[targetedLevel];
            connectedMapParent = undergroundMapParent;
            connectedMap = undergroundMapParent.Map;
            CellRect cellRect = this.OccupiedRect();
            IntVec3 intVec = new IntVec3(cellRect.minX + 1, 0, cellRect.minZ + 1);
            connectedLift = GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("undergroundlift", true), base.Stuff), intVec, connectedMap, WipeMode.Vanish);
            connectedLift.SetFaction(Faction.OfPlayer, null);
            FloodFillerFog.FloodUnfog(intVec, connectedMap);
            bool flag = connectedLift is Building_SpawnedLift;
            if (flag)
            {
                ((Building_SpawnedLift)connectedLift).depth = connectedMapParent.depth;
            }
            else
            {
                Log.Warning("Spawned lift isn't deeprim's lift. Someone's editing this mod! And doing it badly!!! Very badly.", false);
            }
        }

        // Token: 0x0600000E RID: 14 RVA: 0x0000261C File Offset: 0x0000081C
        private void Send()
        {
            bool flag = !m_Power.PowerOn;
            if (flag)
            {
                Messages.Message("No power", MessageTypeDefOf.RejectInput, true);
            }
            else
            {
                Messages.Message("Sending down", MessageTypeDefOf.PositiveEvent, true);
                IEnumerable<IntVec3> cells = this.OccupiedRect().Cells;
                foreach (IntVec3 intVec in cells)
                {
                    List<Thing> thingList = intVec.GetThingList(base.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        bool flag2 = thingList[i] is Pawn || ((thingList[i] is ThingWithComps || thingList[i] is Thing) && !(thingList[i] is Building));
                        if (flag2)
                        {
                            Thing thing = thingList[i];
                            thing.DeSpawn(DestroyMode.Vanish);
                            GenSpawn.Spawn(thing, intVec, connectedMap, WipeMode.Vanish);
                        }
                    }
                }
            }
        }

        // Token: 0x0600000F RID: 15 RVA: 0x00002748 File Offset: 0x00000948
        private void BringUp()
        {
            bool flag = !m_Power.PowerOn;
            if (flag)
            {
                Messages.Message("No power", MessageTypeDefOf.RejectInput, true);
            }
            else
            {
                Messages.Message("Bringing Up", MessageTypeDefOf.PositiveEvent, true);
                IEnumerable<IntVec3> cells = connectedLift.OccupiedRect().Cells;
                foreach (IntVec3 intVec in cells)
                {
                    List<Thing> thingList = intVec.GetThingList(connectedMap);
                    for (int i = 0; i < thingList.Count; i++)
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
                        bool flag2 = thingList[i] is Pawn || ((thingList[i] is ThingWithComps || thingList[i] is Thing) && !(thingList[i] is Building));
                        if (flag2)
                        {
                            Thing thing = thingList[i];
                            thing.DeSpawn(DestroyMode.Vanish);
                            GenSpawn.Spawn(thing, intVec, base.Map, WipeMode.Vanish);
                        }
                    }
                }
            }
        }

        public void SyncConnectedMap()
        {
            UndergroundManager undergroundManager = base.Map.components.Find((MapComponent item) => item is UndergroundManager) as UndergroundManager;
            UndergroundMapParent undergroundMapParent = undergroundManager.layersState[targetedLevel];
            connectedMapParent = undergroundMapParent;
            connectedMap = undergroundMapParent.Map;
        }

        // Token: 0x06000010 RID: 16 RVA: 0x000028B8 File Offset: 0x00000AB8
        public override void Tick()
        {
            base.Tick();
            if (connectedLift != null && ((Building_SpawnedLift)connectedLift).surfaceMap == null)
            {
                ((Building_SpawnedLift)connectedLift).surfaceMap = Map;
            }
            bool flag = mode == 1;
            if (flag)
            {
                ticksCounter++;
            }
            bool flag2 = !m_Power.PowerOn;
            if (!flag2)
            {
                bool flag3 = ticksCounter >= 50;
                if (flag3)
                {
                    MoteMaker.ThrowSmoke(DrawPos, base.Map, 1f);
                    ticksCounter = 0;
                    bool unlimitedPower = DebugSettings.unlimitedPower;
                    if (unlimitedPower)
                    {
                        ChargeLevel += 20;
                    }
                    else
                    {
                        ChargeLevel++;
                    }
                    bool flag4 = ChargeLevel >= 100;
                    if (flag4)
                    {
                        ChargeLevel = 0;
                        mode = 0;
                        FinishedDrill();
                    }
                }
            }
        }

        // Token: 0x17000001 RID: 1
        // (get) Token: 0x06000011 RID: 17 RVA: 0x00002970 File Offset: 0x00000B70
        public float ConnectedMapMarketValue
        {
            get
            {
                bool isConnected = this.isConnected;
                float result;
                if (isConnected)
                {
                    bool flag = Current.ProgramState != ProgramState.Playing;
                    if (flag)
                    {
                        result = 0f;
                    }
                    else
                    {
                        result = connectedMap.wealthWatcher.WealthTotal;
                    }
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
        public bool isConnected
        {
            get
            {
                return connectedMap != null && connectedMapParent != null && connectedLift != null;
            }
        }

        // Token: 0x17000003 RID: 3
        // (get) Token: 0x06000013 RID: 19 RVA: 0x000029F0 File Offset: 0x00000BF0
        public int curMode
        {
            get
            {
                return mode;
            }
        }

        // Token: 0x17000004 RID: 4
        // (get) Token: 0x06000014 RID: 20 RVA: 0x00002A08 File Offset: 0x00000C08
        public UndergroundMapParent linkedMapParent
        {
            get
            {
                return connectedMapParent;
            }
        }

        // Token: 0x04000001 RID: 1
        private static Texture2D UI_Send = ContentFinder<Texture2D>.Get("UI/sendDown", true);

        // Token: 0x04000002 RID: 2
        public static Texture2D UI_BringUp = ContentFinder<Texture2D>.Get("UI/bringUp", true);

        // Token: 0x04000003 RID: 3
        private static Texture2D UI_Start = ContentFinder<Texture2D>.Get("UI/Start", true);

        // Token: 0x04000004 RID: 4
        private static Texture2D UI_Pause = ContentFinder<Texture2D>.Get("UI/Pause", true);

        // Token: 0x04000005 RID: 5
        private static Texture2D UI_Abandon = ContentFinder<Texture2D>.Get("UI/Abandon", true);

        // Token: 0x04000006 RID: 6
        private static Texture2D UI_DrillDown;

        // Token: 0x04000007 RID: 7
        private static Texture2D UI_DrillUp = ContentFinder<Texture2D>.Get("UI/drillup", true);

        // Token: 0x04000008 RID: 8
        private static Texture2D UI_Option;

        // Token: 0x04000009 RID: 9
        private const int updateEveryXTicks = 50;

        // Token: 0x0400000A RID: 10
        private int ticksCounter = 0;

        // Token: 0x0400000B RID: 11
        private CompPowerTrader m_Power;

        // Token: 0x0400000C RID: 12
        private int mode = 0;

        // Token: 0x0400000D RID: 13
        public bool drillNew = true;

        // Token: 0x0400000E RID: 14
        public int targetedLevel = 0;

        // Token: 0x0400000F RID: 15
        private int ChargeLevel;

        // Token: 0x04000010 RID: 16
        public Map connectedMap = null;

        // Token: 0x04000011 RID: 17
        private UndergroundMapParent connectedMapParent = null;

        // Token: 0x04000012 RID: 18
        private Thing connectedLift = null;
    }
}
