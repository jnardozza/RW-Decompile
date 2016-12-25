using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class JobDriver_Hunt : JobDriver
	{
		private const TargetIndex VictimInd = TargetIndex.A;

		private const TargetIndex CorpseInd = TargetIndex.A;

		private const TargetIndex StoreCellInd = TargetIndex.B;

		private const int MaxHuntTicks = 5000;

		private int jobStartTick = -1;

		public Pawn Victim
		{
			get
			{
				Corpse corpse = this.Corpse;
				if (corpse != null)
				{
					return corpse.innerPawn;
				}
				return (Pawn)base.CurJob.GetTarget(TargetIndex.A).Thing;
			}
		}

		private Corpse Corpse
		{
			get
			{
				return base.CurJob.GetTarget(TargetIndex.A).Thing as Corpse;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.LookValue<int>(ref this.jobStartTick, "jobStartTick", 0, false);
		}

		public override string GetReport()
		{
			return base.CurJob.def.reportString.Replace("TargetA", this.Victim.LabelShort);
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(delegate
			{
				if (!this.<>f__this.CurJob.ignoreDesignations)
				{
					Pawn victim = this.<>f__this.Victim;
					if (victim != null && !victim.Dead && Find.DesignationManager.DesignationOn(victim, DesignationDefOf.Hunt) == null)
					{
						return true;
					}
				}
				return false;
			});
			yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
			yield return new Toil
			{
				initAction = delegate
				{
					this.<>f__this.jobStartTick = Find.TickManager.TicksGame;
				}
			};
			yield return Toils_Combat.TrySetJobToUseAttackVerb();
			Toil startCollectCorpse = this.StartCollectCorpseToil();
			Toil gotoCastPos = Toils_Combat.GotoCastPosition(TargetIndex.A, true).JumpIfDespawnedOrNull(TargetIndex.A, startCollectCorpse).FailOn(() => Find.TickManager.TicksGame > this.<>f__this.jobStartTick + 5000);
			yield return gotoCastPos;
			Toil moveIfCannotHit = Toils_Jump.JumpIfTargetNotHittable(TargetIndex.A, gotoCastPos);
			yield return moveIfCannotHit;
			yield return Toils_Jump.JumpIfTargetDownedDistant(TargetIndex.A, gotoCastPos);
			yield return Toils_Combat.CastVerb(TargetIndex.A, false).JumpIfDespawnedOrNull(TargetIndex.A, startCollectCorpse).FailOn(() => Find.TickManager.TicksGame > this.<>f__this.jobStartTick + 5000);
			yield return Toils_Jump.JumpIfTargetDespawnedOrNull(TargetIndex.A, startCollectCorpse);
			yield return Toils_Jump.Jump(moveIfCannotHit);
			yield return startCollectCorpse;
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			yield return Toils_Haul.StartCarryThing(TargetIndex.A);
			Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
			yield return carryToCell;
			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, true);
		}

		private Toil StartCollectCorpseToil()
		{
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				if (this.Victim == null)
				{
					toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
					return;
				}
				TaleRecorder.RecordTale(TaleDefOf.Hunted, new object[]
				{
					this.pawn,
					this.Victim
				});
				Corpse corpse = HuntJobUtility.TryFindCorpse(this.Victim);
				if (corpse == null || !this.pawn.CanReserveAndReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly, 1))
				{
					this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
					return;
				}
				corpse.SetForbidden(false, true);
				IntVec3 vec;
				if (StoreUtility.TryFindBestBetterStoreCellFor(corpse, this.pawn, StoragePriority.Unstored, this.pawn.Faction, out vec, true))
				{
					Find.Reservations.Reserve(this.pawn, corpse, 1);
					Find.Reservations.Reserve(this.pawn, vec, 1);
					this.pawn.CurJob.SetTarget(TargetIndex.B, vec);
					this.pawn.CurJob.SetTarget(TargetIndex.A, corpse);
					this.pawn.CurJob.maxNumToCarry = 1;
					this.pawn.CurJob.haulMode = HaulMode.ToCellStorage;
					return;
				}
				this.pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
			};
			return toil;
		}
	}
}