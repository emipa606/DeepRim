using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DeepRim;

public class Building_SpawnedLift : Building
{
    public int depth;

    private CompPowerPlant m_Power;

    public Building_MiningShaft parentDrill;

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
        var returnString = "Deeprim.LayerDepth".Translate(depth);
        var baseString = base.GetInspectString();
        if (!string.IsNullOrEmpty(baseString))
        {
            returnString += $"\n{baseString}";
        }

        return returnString;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (surfaceMap == null)
        {
            yield break;
        }

        var bringUp = new Command_Action
        {
            action = BringUp,
            defaultLabel = "Deeprim.BringUp".Translate(),
            defaultDesc = "Deeprim.BringUpTT".Translate(),
            icon = Building_MiningShaft.UI_BringUp
        };
        yield return bringUp;
    }

    private void BringUp()
    {
        Messages.Message("Deeprim.BringingUp".Translate(), MessageTypeDefOf.PositiveEvent);
        var cells = this.OccupiedRect().Cells;
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
            }
        }
    }

    public override void Tick()
    {
        base.Tick();
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

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (surfaceMap == null)
        {
            surfaceMap = Find.Maps
                .FirstOrDefault(parentMap =>
                    parentMap.Tile == map.Tile && parentMap.Biome != BiomeDef.Named("Underground"));
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
                .GetForCell(convertedLocation, 5, ThingDef.Named("miningshaft")).FirstOrDefault();
        }

        m_Power = GetComp<CompPowerPlant>();

        if (m_Power == null)
        {
            return;
        }

        if (DeepRimMod.instance.DeepRimSettings.LowTechMode)
        {
            DeepRimMod.LogMessage($"{this} had powercomp when it should not, removing");
            comps.Remove(m_Power);
            m_Power = null;
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