using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepRim;

public class Building_SpawnedLift : Building
{
    public int depth;

    public CompPowerPlant m_Power;

    public CompFlickable m_Flick;

    public Building_MiningShaft parentDrill;

    public Map surfaceMap;
    private bool UsesPower = true;

    public bool usesPower {
        get {
            
            return UsesPower;
        }
        set {
            UsesPower = value;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref depth, "depth");
        Scribe_Values.Look(ref UsesPower, "UsesPower");
        Scribe_References.Look(ref parentDrill, "parentDrill");
        Scribe_References.Look(ref surfaceMap, "surfaceMap");
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Deeprim.LayerDepth".Translate(depth));

        string targetLayerAt = parentDrill.drillNew ? "" : "Deeprim.TargetLayerAt".Translate(parentDrill.targetedLevel);
        if (targetLayerAt != ""){stringBuilder.AppendLine(targetLayerAt);}

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
            yield return new Command_Toggle
                {
                    icon = HarmonyPatches.UI_ToggleSendPower,
                    defaultLabel = "Deeprim.SendPowerToLayer".Translate(),
                    defaultDesc = "Deeprim.SendPowerToLayerTT".Translate(),
                    isActive = () => usesPower,
                    toggleAction = delegate { 
                        TogglePower();
                        if (usesPower){
                            parentDrill.UndergroundManager.ActiveLayers++;
                            }
                        else {
                            parentDrill.UndergroundManager.ActiveLayers--;
                            }
                        }
                };
            yield return new Command_TargetLayer(true)
        {
            shaft = parentDrill,
            manager = parentDrill.Map.components.Find(item => item is UndergroundManager) as UndergroundManager,
            action = delegate { },
            defaultLabel = "Deeprim.ChangeTarget".Translate(),
            defaultDesc = "Deeprim.ChangeTargetExistingTT".Translate(parentDrill.targetedLevel * 10),
            icon = HarmonyPatches.UI_Option
        };
        yield return new Command_Action
        {
            action = BringUp,
            defaultLabel = "Deeprim.BringUp".Translate(),
            defaultDesc = "Deeprim.BringUpTT".Translate(),
            icon = HarmonyPatches.UI_BringUp
        };
        if (parentDrill.targetedLevel != -1){
            yield return new Command_Action
            {
                action = SendDown,
                defaultLabel = "Deeprim.SendDown".Translate(),
                defaultDesc = "Deeprim.SendDownTT".Translate(parentDrill.targetedLevel*10),
                icon = HarmonyPatches.UI_Send
            };
        }
    }

    private void BringUp()
    {
        var cells = this.OccupiedRect().Cells;
        var anythingSent = false;
        foreach (var intVec in cells)
        {
            var thingList = intVec.GetThingList(Map);
            var convertedLocation = HarmonyPatches.ConvertParentDrillLocation(
                intVec, Map.Size, surfaceMap.Size);
            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < thingList.Count; index++)
            {
                var thing = thingList[index];
                if (thing is not Pawn && (thing is not ThingWithComps && thing == null || thing is Building))
                {
                    continue;
                }

                thing.DeSpawn();
                GenSpawn.Spawn(thing, convertedLocation, surfaceMap);
                anythingSent = true;
            }
        }

    

        if (anythingSent)
        {
            Messages.Message("Deeprim.BringingUp".Translate(), MessageTypeDefOf.PositiveEvent);
            if (!Event.current.control)
            {
                return;
            }
            Current.Game.CurrentMap = surfaceMap;
            Find.Selector.Select(parentDrill);

            return;
        }

        Messages.Message("Deeprim.NothingToSend".Translate(), MessageTypeDefOf.RejectInput);
    }

    private void SendDown(){
    {
        var targetLayer = parentDrill.UndergroundManager.layersState[parentDrill.targetedLevel];
        var cells = this.OccupiedRect().Cells;
        var anythingSent = false;
        foreach (var intVec in cells)
        {
            var thingList = intVec.GetThingList(Map);
            var convertedLocation = HarmonyPatches.ConvertParentDrillLocation(
                intVec, Map.Size, targetLayer.Map.Size);
            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < thingList.Count; index++)
            {
                var thing = thingList[index];
                if (thing is not Pawn && (thing is not ThingWithComps && thing == null || thing is Building))
                {
                    continue;
                }

                thing.DeSpawn();
                GenSpawn.Spawn(thing, convertedLocation, targetLayer.Map);
                anythingSent = true;
            }
        }

    

        if (anythingSent)
        {
            Messages.Message("Deeprim.BringingUp".Translate(), MessageTypeDefOf.PositiveEvent);
            if (!Event.current.control)
            {
                return;
            }
            Current.Game.CurrentMap = surfaceMap;
            Find.Selector.Select(parentDrill);

            return;
        }

        Messages.Message("Deeprim.NothingToSend".Translate(), MessageTypeDefOf.RejectInput);
    }
    }

    public override void Tick()
    {
        base.Tick();
        if (!Current.Game.Maps.Contains(surfaceMap)){
            Current.Game.DeinitAndRemoveMap_NewTemp(this.Map, true);
        }
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
            m_Power.Props.basePowerConsumption = parentDrill.PowerAvailable();
        }
    }
    public void TogglePower(){
        m_Flick.DoFlick();
        usesPower = m_Flick.SwitchIsOn;
        parentDrill.wantsUpdateElectricity = true;
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
            m_Power.Props.basePowerConsumption = parentDrill.PowerAvailable();
        }
        else
        {
            DeepRimMod.LogMessage($"Failed to find parent drill for {this}");
        }
    }
}