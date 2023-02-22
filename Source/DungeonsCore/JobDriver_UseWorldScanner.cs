using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DungeonsCore;

public class JobDriver_UseWorldScanner : JobDriver
{
    public Thing WorldScanner => pawn.CurJob.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var scannerComp = WorldScanner.TryGetComp<CompWorldScanner>();
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        var work = new Toil();
        work.tickAction = delegate
        {
            var actor = work.actor;
            _ = (Building)actor.CurJob.targetA.Thing;
            scannerComp.Used(actor);
            actor.skills.Learn(SkillDefOf.Intellectual, 0.035f);
            actor.GainComfortFromCellIfPossible(true);
        };
        work.AddFailCondition(() => !WorkGiver_PerformPingingOnWorldScanner.CanUseScanner(pawn, WorldScanner));
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        work.activeSkill = () => SkillDefOf.Intellectual;
        yield return work;
    }
}