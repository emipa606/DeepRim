using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DeepRim;

public class Building_ShaftLiftParent : Building
{
    public int transferLevel = -1;

    private CompGlower GlowerComp
    {
        get
        {
            if (DeepRimMod.Instance.DeepRimSettings.LowTechMode)
            {
                if (field == null)
                {
                    return null;
                }

                var currentComps = (List<ThingComp>)DeepRimMod.CompsFieldInfo.GetValue(this);
                currentComps.Remove(field);
                DeepRimMod.CompsFieldInfo.SetValue(this, currentComps);

                return null;
            }

            field ??= GetComp<CompGlower>();

            if (field != null)
            {
                return field;
            }

            InitializeComps();
            field = GetComp<CompGlower>();

            return field;
        }
    }

    public CompFlickable FlickableComp
    {
        get
        {
            if (DeepRimMod.Instance.DeepRimSettings.LowTechMode)
            {
                if (field == null)
                {
                    return null;
                }

                var currentComps = (List<ThingComp>)DeepRimMod.CompsFieldInfo.GetValue(this);
                currentComps.Remove(field);
                DeepRimMod.CompsFieldInfo.SetValue(this, currentComps);

                return null;
            }

            field ??= GetComp<CompFlickable>();


            if (field != null)
            {
                return field;
            }

            InitializeComps();
            field = GetComp<CompFlickable>();

            return field;
        }
    }


    protected HashSet<ISlotGroupParent> NearbyStorages
    {
        get
        {
            if (field.Any() && !this.IsHashIntervalTick(GenTicks.TickRareInterval))
            {
                return field;
            }

            field = [];
            foreach (var cell in this.OccupiedRect().AdjacentCells)
            {
                var storages = cell.GetThingList(Map).Where(thing => thing is ISlotGroupParent);
                foreach (var thing in storages)
                {
                    field.Add((ISlotGroupParent)thing);
                }

                if (cell.GetZone(Map) is not ISlotGroupParent stockpile)
                {
                    continue;
                }

                field.Add(stockpile);
            }

            return field;
        }
    } = [];


    protected void Transfer(Building_ShaftLiftParent targetLift, List<ISlotGroupParent> connectedStorages)
    {
        foreach (var storage in connectedStorages)
        {
            var items = storage.GetSlotGroup().HeldThings;
            var itemsArray = items as Thing[] ?? items.ToArray();
            if (!itemsArray.Any())
            {
                DeepRimMod.LogMessage($"{storage} has no items");
                continue;
            }

            var itemList = itemsArray.ToList();
            DeepRimMod.LogMessage(
                $"Transferring {itemList.Count} items from {storage} by shaft {this} to layer at {transferLevel * 10}m");

            // ReSharper disable once ForCanBeConvertedToForeach, Things despawn, cannot use foreach
            for (var index = 0; index < itemList.Count; index++)
            {
                var thing = itemList[index];
                thing.DeSpawn();

                if (!targetLift.NearbyStorages.Any())
                {
                    GenSpawn.Spawn(thing, targetLift.RandomAdjacentCell8Way(), targetLift.Map);
                    continue;
                }

                var placed = false;
                foreach (var possibleStorage in targetLift.NearbyStorages)
                {
                    if (!possibleStorage.Accepts(thing))
                    {
                        continue;
                    }

                    foreach (var validStorageCell in possibleStorage.AllSlotCells()
                                 .Where(vec3 => vec3.IsValidStorageFor(targetLift.Map, thing)))
                    {
                        if (!GenPlace.TryPlaceThing(thing, validStorageCell, targetLift.Map, ThingPlaceMode.Direct))
                        {
                            continue;
                        }

                        placed = true;
                        break;
                    }

                    if (placed)
                    {
                        break;
                    }
                }

                if (!placed)
                {
                    GenSpawn.Spawn(thing, targetLift.RandomAdjacentCell8Way(), targetLift.Map);
                }
            }
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        _ = GlowerComp;
        _ = FlickableComp;
    }
}