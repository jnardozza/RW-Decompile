using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class JobGiver_PackFood : ThinkNode_JobGiver
	{
		private const float MaxInvNutritionToConsiderLookingForFood = 0.4f;

		private const float MinFinalInvNutritionToPickUp = 0.8f;

		private const float MinNutritionPerColonistToDo = 1.5f;

		public const FoodPreferability MinFoodPreferability = FoodPreferability.MealAwful;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn.inventory == null)
			{
				return null;
			}
			float invNutrition = this.GetInventoryPackableFoodNutrition(pawn);
			if (invNutrition > 0.4f)
			{
				return null;
			}
			if (pawn.Map.resourceCounter.TotalHumanEdibleNutrition < (float)pawn.Map.mapPawns.ColonistsSpawnedCount * 1.5f)
			{
				return null;
			}
			Thing thing = GenClosest.ClosestThing_Regionwise_ReachablePrioritized(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 20f, delegate(Thing t)
			{
				if (!this.IsGoodPackableFoodFor(t, pawn) || t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, false) || !t.IsSociallyProper(pawn))
				{
					return false;
				}
				float num3 = invNutrition + t.GetStatValue(StatDefOf.Nutrition, true) * (float)t.stackCount;
				if (num3 < 0.8f)
				{
					return false;
				}
				List<ThoughtDef> list = FoodUtility.ThoughtsFromIngesting(pawn, t, FoodUtility.GetFinalIngestibleDef(t, false));
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].stages[0].baseMoodEffect < 0f)
					{
						return false;
					}
				}
				return true;
			}, (Thing x) => FoodUtility.FoodOptimality(pawn, x, FoodUtility.GetFinalIngestibleDef(x, false), 0f, false), 24, 30);
			if (thing == null)
			{
				return null;
			}
			float num = pawn.needs.food.MaxLevel - invNutrition;
			int num2 = Mathf.FloorToInt(num / thing.GetStatValue(StatDefOf.Nutrition, true));
			num2 = Mathf.Min(num2, thing.stackCount);
			num2 = Mathf.Max(num2, 1);
			return new Job(JobDefOf.TakeInventory, thing)
			{
				count = num2
			};
		}

		private float GetInventoryPackableFoodNutrition(Pawn pawn)
		{
			ThingOwner<Thing> innerContainer = pawn.inventory.innerContainer;
			float num = 0f;
			for (int i = 0; i < innerContainer.Count; i++)
			{
				if (this.IsGoodPackableFoodFor(innerContainer[i], pawn))
				{
					num += innerContainer[i].GetStatValue(StatDefOf.Nutrition, true) * (float)innerContainer[i].stackCount;
				}
			}
			return num;
		}

		private bool IsGoodPackableFoodFor(Thing food, Pawn forPawn)
		{
			return food.def.IsNutritionGivingIngestible && food.def.EverHaulable && food.def.ingestible.preferability >= FoodPreferability.MealAwful && forPawn.WillEat(food, null);
		}
	}
}
