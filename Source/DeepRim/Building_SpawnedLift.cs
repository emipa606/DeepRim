using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace DeepRim;

public class Building_SpawnedLift : Building_ShaftLiftParent
{
    public int depth;

    public Building_MiningShaft parentDrill;
    private CompPowerPlant powerComp;
    public bool PriorPowerState = true;

    public Map surfaceMap;

    public bool TemporaryOffState;


    public CompPowerPlant PowwerComp
    {
        get
        {
            if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
            {
                if (powerComp != null)
                {
                    comps.Remove(powerComp);
                }

                return null;
            }

            if (powerComp == null)
            {
                powerComp = GetComp<CompPowerPlant>();
            }

            if (powerComp != null)
            {
                return powerComp;
            }

            InitializeComps();
            powerComp = GetComp<CompPowerPlant>();

            return powerComp;
        }
    }

    public int TransferLevel
    {
        get
        {
            if (transferLevel == -1)
            {
                transferLevel = depth;
            }

            return transferLevel;
        }
        set => transferLevel = value;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref depth, "depth");
        Scribe_Values.Look(ref transferLevel, "transferLevel");
        Scribe_References.Look(ref parentDrill, "parentDrill");
        Scribe_References.Look(ref surfaceMap, "surfaceMap");
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Deeprim.LayerDepth".Translate(depth));
        string label;
        var name = parentDrill.UndergroundManager.GetLayerName(depth);
        if (name != "")
        {
            stringBuilder.AppendLine("Deeprim.LayerName".Translate(name));
        }

        name = parentDrill.UndergroundManager.GetLayerName(parentDrill.targetedLevel);
        if (parentDrill.drillNew)
        {
            var nextLayer = parentDrill.UndergroundManager != null ? parentDrill.UndergroundManager.NextLayer * 10 : 10;
            label = "Deeprim.TargetNewLayerAtDepth".Translate(nextLayer);
        }
        else if (parentDrill.targetedLevel == depth)
        {
            label = "Deeprim.TargetLayerThis".Translate();
        }
        else if (name != "")
        {
            label = "Deeprim.TargetLayerAtNamed".Translate(parentDrill.targetedLevel, name);
        }
        else
        {
            label = "Deeprim.TargetLayerAt".Translate(parentDrill.targetedLevel);
        }

        stringBuilder.AppendLine(label);

        if (NearbyStorages.Any())
        {
            name = parentDrill.UndergroundManager.GetLayerName(transferLevel);
            if (transferLevel == 0)
            {
                label = "Deeprim.TransferSurface".Translate();
            }
            else if (transferLevel == depth)
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

        yield return new Command_Action
        {
            icon = TexButton.Rename,
            defaultLabel = "Rename".Translate(),
            defaultDesc = "Deeprim.ChangeLayerName".Translate(),
            action = delegate
            {
                var manager = parentDrill.UndergroundManager;
                if (manager.layerNames.Count == 0 && manager.layersState.Count > 0)
                {
                    manager.InitLayerNames();
                }

                var dialog_RenameZone = new Dialog_RenameLayer(this);
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

        if (NearbyStorages.Any())
        {
            yield return new Command_TransferLayer(this)
            {
                lift = this,
                manager = parentDrill.Map.components.Find(item => item is UndergroundManager) as UndergroundManager,
                action = delegate { },
                defaultLabel = "Deeprim.ChangeTransferTarget".Translate(),
                defaultDesc = "Deeprim.ChangeTransferTargetTT".Translate(TransferLevel * 10),
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
        if (parentDrill.targetedLevel > 0 && parentDrill.targetedLevel != depth)
        {
            yield return new Command_Action
            {
                action = SendDown,
                defaultLabel = "Deeprim.SendDown".Translate(),
                defaultDesc = "Deeprim.SendDownTT".Translate(parentDrill.targetedLevel * 10),
                icon = HarmonyPatches.UI_Send
            };
        }


        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            yield break;
        }

        if (!TemporaryOffState)
        {
            yield return new Command_Toggle
            {
                icon = HarmonyPatches.UI_ToggleSendPower,
                defaultLabel = "Deeprim.SendPowerToLayer".Translate(),
                defaultDesc = "Deeprim.SendPowerToLayerTT".Translate(),
                isActive = () => FlickableComp?.SwitchIsOn is true,
                toggleAction = delegate
                {
                    TogglePower();
                    if (FlickableComp?.SwitchIsOn == true)
                    {
                        parentDrill.UndergroundManager.ActiveLayers++;
                        parentDrill.UndergroundManager.AnyLayersPowered = true;
                    }
                    else
                    {
                        parentDrill.UndergroundManager.ActiveLayers--;
                        if (parentDrill.UndergroundManager.ActiveLayers == 0)
                        {
                            parentDrill.UndergroundManager.AnyLayersPowered = false;
                        }
                    }
                }
            };
        }
    }

    public void BringUp()
    {
        LiftUtils.StageSend(this, true);
    }

    public void SendDown()
    {
        LiftUtils.StageSend(this);
    }

    public void SendFromStorages()
    {
        if (!NearbyStorages.Any())
        {
            return;
        }

        Building_ShaftLiftParent targetShaft;
        var manager = parentDrill.UndergroundManager;
        if (parentDrill.UndergroundManager?.layersState == null)
        {
            return;
        }

        if (TransferLevel == depth)
        {
            DeepRimMod.LogMessage("Underground lift transfer level is set to None");
            return;
        }

        if (TransferLevel == 0)
        {
            var transferShafts =
                parentDrill.Map?.listerBuildings.AllBuildingsColonistOfDef(ShaftThingDefOf.miningshaft);
            if (transferShafts != null && transferShafts.Any())
            {
                targetShaft = transferShafts.First() as Building_ShaftLiftParent;
                if (targetShaft == null)
                {
                    DeepRimMod.LogMessage("Parent mineshaft was not found. How did we get here?");
                    return;
                }
            }
            else
            {
                DeepRimMod.LogMessage("Parent mineshaft was not found. How did we get here?");
                return;
            }
        }
        else
        {
            if (manager?.layersState.ContainsKey(TransferLevel) == true)
            {
                targetShaft = manager?.layersState[TransferLevel]?.GetSpawnedLift();
                if (targetShaft == null)
                {
                    DeepRimMod.LogMessage("Found no spawned lift in targeted layer");
                    return;
                }
            }
            else
            {
                return;
            }
        }

        DeepRimMod.LogMessage($"Lift {this} Found {NearbyStorages.Count} storages to transfer items from");
        if (PowwerComp is not { PowerOn: false })
        {
            Transfer(targetShaft, NearbyStorages.ToList());
        }
        else
        {
            DeepRimMod.LogMessage($"Unpowered lift {this} refuses to send items");
        }
    }

    public override void Tick()
    {
        base.Tick();
        if (!Current.Game.Maps.Contains(surfaceMap))
        {
            Current.Game.DeinitAndRemoveMap(Map, true);
        }

        if (this.IsHashIntervalTick(GenTicks.TickRareInterval))
        {
            SendFromStorages();
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

        if (PowwerComp != null && parentDrill != null)
        {
            PowwerComp.Props.basePowerConsumption = 0 - parentDrill.PowerAvailable();
        }

        //DeepRimMod.LogWarn($"tempOffState: {TemporaryOffState}\nPrior Power: {PriorPowerState}\nFlick State: {m_Flick.SwitchIsOn}\nPower Available: {parentDrill.PowerAvailable()}");
        //Handle cutting power to the lifts if the parent loses power
        if (parentDrill != null && TemporaryOffState && parentDrill.PowwerComp is { PowerOn: true })
        {
            DeepRimMod.LogWarn($"Lift {this} is no longer disabled due to lack of power");
            TemporaryOffState = false;
            if (PriorPowerState && !FlickableComp.SwitchIsOn)
            {
                FlickableComp.DoFlick();
            }
        }
        else if (parentDrill != null && !TemporaryOffState && parentDrill.PowwerComp is not { PowerOn: true })
        {
            DeepRimMod.LogMessage($"Temporarily disabling lift {this} due to lack of power");
            TemporaryOffState = true;
            PriorPowerState = FlickableComp.SwitchIsOn;
            if (FlickableComp?.SwitchIsOn == true)
            {
                FlickableComp.DoFlick();
            }
        }
    }

    public void TogglePower()
    {
        FlickableComp.DoFlick();
        if (FlickableComp.SwitchIsOn)
        {
            parentDrill.UndergroundManager.AnyLayersPowered = true;
        }
        else if (parentDrill.UndergroundManager.ActiveLayers == 0)
        {
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

        _ = PowwerComp;

        switch (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            case true when stuffInt == null:
                stuffInt = ThingDefOf.WoodLog;
                break;
            case false when stuffInt != null:
                stuffInt = null;
                break;
        }

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            return;
        }

        if (parentDrill != null)
        {
            PowwerComp.Props.basePowerConsumption = 0 - parentDrill.PowerAvailable();
        }
        else
        {
            DeepRimMod.LogMessage($"Failed to find parent drill for {this}");
        }
    }
}