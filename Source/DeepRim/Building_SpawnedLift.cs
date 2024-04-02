using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;

namespace DeepRim;

public class Building_SpawnedLift : Building
{
    public int depth;

    public CompPowerPlant m_Power;

    public CompFlickable m_Flick;

    public bool TemporaryOffState = false;
    public bool PriorPowerState = true;

    public Building_MiningShaft parentDrill;

    private HashSet<Building_Storage> nearbyStorages = [];

    private int transferLevel = -1;

    public int TransferLevel {
        get {
            if (transferLevel == -1){
                transferLevel = this.depth;
            }
            return transferLevel;
        }
        set {
            transferLevel = value;
        }
    }

    public Map surfaceMap;
 
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref depth, "depth");
        Scribe_References.Look(ref parentDrill, "parentDrill");
        Scribe_References.Look(ref surfaceMap, "surfaceMap");
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Deeprim.LayerDepth".Translate(depth));
        string label;
        string name = parentDrill.UndergroundManager.GetLayerName(this.depth);
        if (name != ""){
            stringBuilder.AppendLine("Deeprim.LayerName".Translate(name));
        }

        name = parentDrill.UndergroundManager.GetLayerName(parentDrill.targetedLevel);
        if (parentDrill.drillNew){
            int nextLayer = parentDrill.UndergroundManager != null ? parentDrill.UndergroundManager.NextLayer * 10 : 10;
            label = "Deeprim.TargetNewLayerAtDepth".Translate(nextLayer);
        }
        else if (parentDrill.targetedLevel == this.depth){
            label = "Deeprim.TargetLayerThis".Translate();
        }
        else if (name != ""){
            label = "Deeprim.TargetLayerAtNamed".Translate(parentDrill.targetedLevel, name);
        }
        else {
            label = "Deeprim.TargetLayerAt".Translate(parentDrill.targetedLevel);
        }
        stringBuilder.AppendLine(label);
        
        var storages = this.CellsAdjacent8WayAndInside().Where(vec3 => vec3.GetFirstThing<Building_Storage>(Map) != null);
        if (storages.Any())
        {
            name = parentDrill.UndergroundManager.GetLayerName(transferLevel);
            if (transferLevel == 0){
                label = "Deeprim.TransferSurface".Translate();
            }
            else if (transferLevel == this.depth){
                label = "Deeprim.TransferLevelNone".Translate();
            }
            else if (name != ""){
                label = "Deeprim.TransferTargetAtNamed".Translate(transferLevel, name);
            }
            else {
                label = "Deeprim.TransferTargetAt".Translate(transferLevel);
            }
            stringBuilder.AppendLine(label);
        }

        var baseString = base.GetInspectString();
        if (!string.IsNullOrEmpty(baseString))
        {
            stringBuilder.AppendLine(base.GetInspectString());
        }

        return stringBuilder.ToString().Trim();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (surfaceMap == null)
        {
            yield break;
        }
        if (!TemporaryOffState){
            yield return new Command_Toggle
                {
                    icon = HarmonyPatches.UI_ToggleSendPower,
                    defaultLabel = "Deeprim.SendPowerToLayer".Translate(),
                    defaultDesc = "Deeprim.SendPowerToLayerTT".Translate(),
                    isActive = () => m_Flick.SwitchIsOn,
                    toggleAction = delegate { 
                        TogglePower();
                        if (m_Flick.SwitchIsOn){
                            parentDrill.UndergroundManager.ActiveLayers++;
                            parentDrill.UndergroundManager.AnyLayersPowered = true;
                            }
                        else {
                            parentDrill.UndergroundManager.ActiveLayers--;
                            if (parentDrill.UndergroundManager.ActiveLayers == 0){
                                parentDrill.UndergroundManager.AnyLayersPowered = false;
                            }
                            }
                        }
                };
        }
        yield return new Command_Action(){
            icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone"),
            defaultLabel = "CommandRenameZoneLabel".Translate(),
            defaultDesc = "Deeprim.ChangeLayerName".Translate(),
            action = delegate
            {
                var manager = parentDrill.UndergroundManager;
                if (manager.layerNames.Count == 0 && manager.layersState.Count > 0){
                    manager.InitLayerNames();
                }
                Dialog_RenameLayer dialog_RenameZone = new Dialog_RenameLayer(this);
                Find.WindowStack.Add(dialog_RenameZone);
            }
        };
            yield return new Command_TargetLayer(this)
        {
            shaft = parentDrill,
            manager = parentDrill.Map.components.Find(item => item is UndergroundManager) as UndergroundManager,
            action = delegate { },
            defaultLabel = "Deeprim.ChangeTarget".Translate(),
            defaultDesc = "Deeprim.ChangeTargetExistingTT".Translate(parentDrill.targetedLevel * 10),
            icon = HarmonyPatches.UI_Option
        };
        var storages = this.CellsAdjacent8WayAndInside().Where(vec3 => vec3.GetFirstThing<Building_Storage>(Map) != null);
        if (storages.Any()){
            yield return new Command_TransferLayer(this)
            {
                lift = this,
                manager = parentDrill.Map.components.Find(item => item is UndergroundManager) as UndergroundManager,
                action = delegate { },
                defaultLabel = "Deeprim.ChangeTransferTarget".Translate(),
                defaultDesc = "Deeprim.ChangeTransferTargetTT".Translate(this.TransferLevel * 10),
                icon = HarmonyPatches.UI_Transfer

            };
        }
        yield return new Command_Action
        {
            action = BringUp,
            defaultLabel = "Deeprim.BringUp".Translate(),
            defaultDesc = "Deeprim.BringUpTT".Translate(),
            icon = HarmonyPatches.UI_BringUp
        };
        if (parentDrill.targetedLevel > 0 && parentDrill.targetedLevel != depth){
            yield return new Command_Action
            {
                action = SendDown,
                defaultLabel = "Deeprim.SendDown".Translate(),
                defaultDesc = "Deeprim.SendDownTT".Translate(parentDrill.targetedLevel*10),
                icon = HarmonyPatches.UI_Send
            };
        }
    }

    public void BringUp()
    {
        LiftUtils.StageSend(this, true);
        return;
    }

    public void SendDown(){
        LiftUtils.StageSend(this);
        return;
    }

    private void Transfer(Map targetMap, IntVec3 targetPosition, List<Building_Storage> connectedStorages)
    {
        foreach (var storage in connectedStorages)
        {
            var items = storage.GetSlotGroup().HeldThings;
            if (items == null || items.Any() == false)
            {
                DeepRimMod.LogMessage($"Storage {storage} by lift {this} has no items");
                continue;
            }

            var itemList = items.ToList();
            DeepRimMod.LogMessage($"Transferring {itemList.Count} items from {storage} by lift {this} to layer at {TransferLevel*10}m");
            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < itemList.Count; index++)
            {
                var thing = itemList[index];
                thing.DeSpawn();
                GenSpawn.Spawn(thing, targetPosition, targetMap);
            }
        }
    }

    public void SendFromStorages(){
            nearbyStorages = [];
            foreach (var cell in this.OccupiedRect().AdjacentCells)
            {
                var Storages = cell.GetThingList(Map).Where(thing => thing is Building_Storage);
                foreach (Building_Storage storage in Storages){
                    nearbyStorages.Add(storage);
                }
            }
            if (!nearbyStorages.Any()){return;}
            Map targetMap = null;
            var targetPosition = IntVec3.Invalid;
            var manager = parentDrill.UndergroundManager;
            if (parentDrill.UndergroundManager?.layersState != null)
            {
                if (TransferLevel != depth)
                {
                    if (TransferLevel == 0){
                        targetMap = parentDrill.Map;
                        var transferShafts = targetMap?.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("miningshaft"));
                        if (transferShafts != null && transferShafts.Any()){
                            targetPosition = transferShafts.First().Position;
                        }
                        else {
                            DeepRimMod.LogMessage("Parent mineshaft was not found. How did we get here?");
                        }
                    }
                    else {
                        if (manager?.layersState.ContainsKey(TransferLevel) == true)
                        {
                            targetMap = manager?.layersState[TransferLevel]?.Map;
                            var transferLifts =
                                targetMap?.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("undergroundlift"));
                            if (transferLifts != null && transferLifts.Any())
                            {
                                targetPosition = transferLifts.First().Position;
                            }
                            else
                            {
                                DeepRimMod.LogMessage("Found no spawned lift in targeted layer");
                            }
                        }
                    }
                }
                else {
                    DeepRimMod.LogMessage("Underground lift transfer level is set to None");
                    return;
                }
            }

            if (targetMap == null || targetPosition == IntVec3.Invalid || !nearbyStorages.Any())
            {
                return;
            }

            DeepRimMod.LogMessage($"Lift {this} Found {nearbyStorages.Count} storages to transfer items from");
            if (m_Power is not { PowerOn: false })
            {
                Transfer(targetMap, targetPosition, nearbyStorages.ToList());
            }
            else
            {
                DeepRimMod.LogMessage($"Unpowered lift {this} refuses to send items");
            }
    }

    public override void Tick()
    {
        base.Tick();
        if (!Current.Game.Maps.Contains(surfaceMap)){
            Current.Game.DeinitAndRemoveMap_NewTemp(this.Map, true);
        }
        if (GenTicks.TicksGame % GenTicks.TickRareInterval == 0)
        {
            this.SendFromStorages();
        }
        //Why every 78 ticks?
        if (GenTicks.TicksGame % 78 != 0)
        {
            return;
        }

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            return;
        }

        if (m_Power != null && parentDrill != null)
        {
            m_Power.Props.basePowerConsumption = 0 - parentDrill.PowerAvailable();
        }
        //DeepRimMod.LogWarn($"tempOffState: {TemporaryOffState}\nPrior Power: {PriorPowerState}\nFlick State: {m_Flick.SwitchIsOn}\nPower Available: {parentDrill.PowerAvailable()}");
        //Handle cutting power to the lifts if the parent loses power
        if (TemporaryOffState && parentDrill.m_Power.PowerOn){
            DeepRimMod.LogWarn($"Lift {this} is no longer disabled due to lack of power");
            TemporaryOffState = false;
            if (PriorPowerState && !m_Flick.SwitchIsOn){m_Flick.DoFlick();}
        }
        else if (!TemporaryOffState && !parentDrill.m_Power.PowerOn){
            DeepRimMod.LogMessage($"Temporarily disabling lift {this} due to lack of power");
            TemporaryOffState = true;
            PriorPowerState = m_Flick.SwitchIsOn;
            if (m_Flick.SwitchIsOn){
                m_Flick.DoFlick();
                }
            
        }
    }
    public void TogglePower(){
        m_Flick.DoFlick();
        if (m_Flick.SwitchIsOn){
            parentDrill.UndergroundManager.AnyLayersPowered = true;
            }
        else if (parentDrill.UndergroundManager.ActiveLayers == 0){
            parentDrill.UndergroundManager.AnyLayersPowered = false;
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (surfaceMap == null)
        {
            surfaceMap = Find.Maps
                .FirstOrDefault(parentMap =>
                    parentMap.Tile == map.Tile && parentMap.Biome != UndergroundBiomeDefOf.Underground);
        }

        if (surfaceMap == null)
        {
            return;
        }

        if (parentDrill == null)
        {
            var convertedLocation = HarmonyPatches.ConvertParentDrillLocation(
                Position, Map.Size, surfaceMap.Size);
            parentDrill = (Building_MiningShaft)surfaceMap.listerBuldingOfDefInProximity
                .GetForCell(convertedLocation, 5, ShaftThingDefOf.miningshaft).FirstOrDefault();
        }


        if (DeepRimMod.instance.DeepRimSettings.LowTechMode && stuffInt == null)
        {
            stuffInt = ThingDefOf.WoodLog;
        }

        if (!DeepRimMod.instance.DeepRimSettings.LowTechMode && stuffInt != null)
        {
            stuffInt = null;
        }

        m_Power = GetComp<CompPowerPlant>();
        m_Flick = GetComp<CompFlickable>();

        if (m_Power == null || m_Flick == null)
        {
            return;
        }
        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            DeepRimMod.LogMessage($"{this} had powercomp when it should not, removing");
            comps.Remove(m_Power);
            comps.Remove(m_Flick);
            m_Power = null;
            m_Flick = null;
            return;
        }

        if (parentDrill != null)
        {
            m_Power.Props.basePowerConsumption = 0 - parentDrill.PowerAvailable();
        }
        else
        {
            DeepRimMod.LogMessage($"Failed to find parent drill for {this}");
        }
    }
}