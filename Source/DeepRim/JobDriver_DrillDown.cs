using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace DeepRim;

internal class JobDriver_DrillDown : JobDriver
{
    private Building_MiningShaft MiningShaft => (Building_MiningShaft)TargetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() =>
        {
            if (MiningShaft.CurMode == 1)
            {
                return false;
            }

            return true;
        });
        yield return Toils_Reserve.Reserve(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
            .FailOnDespawnedNullOrForbidden(TargetIndex.A);

        var mine = new Toil();
        mine.WithEffect(EffecterDefOf.Drill, TargetIndex.A);
        mine.WithProgressBar(TargetIndex.A, () => MiningShaft.ChargeLevel / 100f);
        mine.tickAction = delegate
        {
            var mineActor = mine.actor;
            var chargeIncrease = mineActor.GetStatValue(StatDefOf.MiningSpeed) / 1000;
            MiningShaft.ChargeLevel += chargeIncrease;
            //Log.Message(
            //    $"Increasing charge by {chargeIncrease} with mining skill {mineActor.GetStatValue(StatDefOf.MiningSpeed)}");
            mineActor.skills.Learn(SkillDefOf.Mining, 0.125f);
            if (!(MiningShaft.ChargeLevel >= 100f))
            {
                return;
            }

            if (MiningShaft.CurMode == 1)
            {
                return;
            }

            EndJobWith(JobCondition.Succeeded);
            mineActor.records.Increment(RecordDefOf.CellsMined);
        };
        mine.WithEffect(TargetThingA.def.repairEffect, TargetIndex.A);
        mine.defaultCompleteMode = ToilCompleteMode.Never;
        yield return mine;
    }
}