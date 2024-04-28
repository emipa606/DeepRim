using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepRim;

public class LiftUtils
{
    private static readonly List<ThingCategory> invalidCategories =
    [
        ThingCategory.None, ThingCategory.Ethereal, ThingCategory.Filth, ThingCategory.Gas,
        ThingCategory.Mote, ThingCategory.Projectile
    ];

    public static IntVec3 ConvertPosRelativeToLift(IntVec3 shaftCenter, IntVec3 itemPos, IntVec3 targetCenter)
    {
        return targetCenter - (shaftCenter - itemPos);
    }

    public static void Send(Map oMap, IntVec3 oPos, Map tMap, IntVec3 tPos, bool bringUp)
    {
        var anythingSent = false;
        _ = tMap;
        var cells = CellRect.CenteredOn(oPos, 1);
        foreach (var cell in cells)
        {
            var thingList = cell.GetThingList(oMap).Where(
                thing => thing != null &&
                         (thing is Pawn ||
                          thing is not Building &&
                          thing is not Blueprint &&
                          !invalidCategories.Contains(thing.def.category)
                         )
            ).ToList();
            var convertedLocation = ConvertPosRelativeToLift(oPos, cell, tPos);
            foreach (var thing in thingList)
            {
                if (thing is not Pawn && (thing is not ThingWithComps && thing == null || thing is Building ||
                                          thing is Blueprint))
                {
                    continue;
                }

                thing.DeSpawn();
                GenSpawn.Spawn(thing, convertedLocation, tMap);
                anythingSent = true;
            }
        }

        if (anythingSent)
        {
            var msg = bringUp ? "Deeprim.BringingUp".Translate() : "Deeprim.SendingDown".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent);
            if (!Event.current.control)
            {
                return;
            }

            Current.Game.CurrentMap = tMap;
            Find.Selector.Select(tPos);

            return;
        }

        Messages.Message("Deeprim.NothingToSend".Translate(), MessageTypeDefOf.RejectInput);
    }

    public static void StageSend(Building sender, bool bringUp = false)
    {
        Map oMap;
        Map tMap;
        IntVec3 oPos;
        IntVec3 tPos;

        switch (sender)
        {
            case Building_MiningShaft shaft:
                if (shaft.m_Power.PowerOn == false && DeepRimMod.instance.NoPowerPreventsLiftUse)
                {
                    Messages.Message("Deeprim.NoPower".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                oMap = shaft.Map;
                tMap = shaft.connectedLift.Map;
                oPos = shaft.Position;
                tPos = shaft.connectedLift.Position;
                DeepRimMod.LogMessage(
                    $"Mineshaft wants to send items.\noMap: {oMap}\ntMap: {tMap}\noPos: {oPos}\ntPos: {tPos}");
                Send(oMap, oPos, tMap, tPos, bringUp);
                break;

            case Building_SpawnedLift lift:
                if (lift.m_Power.PowerOn == false && DeepRimMod.instance.NoPowerPreventsLiftUse)
                {
                    Messages.Message("Deeprim.NoPower".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                oMap = lift.Map;
                oPos = lift.Position;
                if (bringUp)
                {
                    tMap = lift.parentDrill.Map;
                    tPos = lift.parentDrill.Position;
                }
                else
                {
                    tMap = lift.parentDrill.connectedLift.Map;
                    tPos = lift.parentDrill.connectedLift.Position;
                }

                DeepRimMod.LogMessage(
                    $"Underground Lift wants to send items.\noMap: {oMap}\ntMap: {tMap}\noPos: {oPos}\ntPos: {tPos}");
                Send(oMap, oPos, tMap, tPos, bringUp);
                break;
        }
    }
}