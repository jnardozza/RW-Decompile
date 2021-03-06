using System;
using System.Text;
using Verse;

namespace RimWorld.Planet
{
	public static class CaravanAbandonOrBanishUtility
	{
		public static void TryAbandonOrBanishViaInterface(Thing t, Caravan caravan)
		{
			Pawn p = t as Pawn;
			if (p != null)
			{
				if (!caravan.PawnsListForReading.Any((Pawn x) => x != p && caravan.IsOwner(x)))
				{
					Messages.Message("MessageCantBanishLastColonist".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
					return;
				}
				PawnBanishUtility.ShowBanishPawnConfirmationDialog(p);
			}
			else
			{
				Dialog_MessageBox window = Dialog_MessageBox.CreateConfirmation("ConfirmAbandonItemDialog".Translate(t.Label, t), delegate
				{
					Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, t);
					if (ownerOf == null)
					{
						Log.Error("Could not find owner of " + t, false);
						return;
					}
					ownerOf.inventory.innerContainer.Remove(t);
					t.Destroy(DestroyMode.Vanish);
					caravan.RecacheImmobilizedNow();
					caravan.RecacheDaysWorthOfFood();
				}, true, null);
				Find.WindowStack.Add(window);
			}
		}

		public static void TryAbandonOrBanishViaInterface(TransferableImmutable t, Caravan caravan)
		{
			Pawn pawn = t.AnyThing as Pawn;
			if (pawn != null)
			{
				CaravanAbandonOrBanishUtility.TryAbandonOrBanishViaInterface(pawn, caravan);
			}
			else
			{
				Dialog_MessageBox window = Dialog_MessageBox.CreateConfirmation("ConfirmAbandonItemDialog".Translate(t.LabelWithTotalStackCount), delegate
				{
					for (int i = 0; i < t.things.Count; i++)
					{
						Thing thing = t.things[i];
						Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, thing);
						if (ownerOf == null)
						{
							Log.Error("Could not find owner of " + thing, false);
							return;
						}
						ownerOf.inventory.innerContainer.Remove(thing);
						thing.Destroy(DestroyMode.Vanish);
					}
					caravan.RecacheImmobilizedNow();
					caravan.RecacheDaysWorthOfFood();
				}, true, null);
				Find.WindowStack.Add(window);
			}
		}

		public static void TryAbandonSpecificCountViaInterface(Thing t, Caravan caravan)
		{
			Find.WindowStack.Add(new Dialog_Slider("AbandonSliderText".Translate(t.LabelNoCount), 1, t.stackCount, delegate(int x)
			{
				Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, t);
				if (ownerOf == null)
				{
					Log.Error("Could not find owner of " + t, false);
					return;
				}
				if (x >= t.stackCount)
				{
					ownerOf.inventory.innerContainer.Remove(t);
					t.Destroy(DestroyMode.Vanish);
				}
				else
				{
					t.SplitOff(x).Destroy(DestroyMode.Vanish);
				}
				caravan.RecacheImmobilizedNow();
				caravan.RecacheDaysWorthOfFood();
			}, -2147483648));
		}

		public static void TryAbandonSpecificCountViaInterface(TransferableImmutable t, Caravan caravan)
		{
			Find.WindowStack.Add(new Dialog_Slider("AbandonSliderText".Translate(t.Label), 1, t.TotalStackCount, delegate(int x)
			{
				int num = x;
				for (int i = 0; i < t.things.Count; i++)
				{
					if (num <= 0)
					{
						break;
					}
					Thing thing = t.things[i];
					Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, thing);
					if (ownerOf == null)
					{
						Log.Error("Could not find owner of " + thing, false);
						return;
					}
					if (num >= thing.stackCount)
					{
						num -= thing.stackCount;
						ownerOf.inventory.innerContainer.Remove(thing);
						thing.Destroy(DestroyMode.Vanish);
					}
					else
					{
						thing.SplitOff(num).Destroy(DestroyMode.Vanish);
						num = 0;
					}
				}
				caravan.RecacheImmobilizedNow();
				caravan.RecacheDaysWorthOfFood();
			}, -2147483648));
		}

		public static string GetAbandonOrBanishButtonTooltip(Thing t, bool abandonSpecificCount)
		{
			Pawn pawn = t as Pawn;
			if (pawn != null)
			{
				return PawnBanishUtility.GetBanishButtonTip(pawn);
			}
			return CaravanAbandonOrBanishUtility.GetAbandonItemButtonTooltip(t.stackCount, abandonSpecificCount);
		}

		public static string GetAbandonOrBanishButtonTooltip(TransferableImmutable t, bool abandonSpecificCount)
		{
			Pawn pawn = t.AnyThing as Pawn;
			if (pawn != null)
			{
				return PawnBanishUtility.GetBanishButtonTip(pawn);
			}
			return CaravanAbandonOrBanishUtility.GetAbandonItemButtonTooltip(t.TotalStackCount, abandonSpecificCount);
		}

		private static string GetAbandonItemButtonTooltip(int currentStackCount, bool abandonSpecificCount)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (currentStackCount == 1)
			{
				stringBuilder.AppendLine("AbandonTip".Translate());
			}
			else if (abandonSpecificCount)
			{
				stringBuilder.AppendLine("AbandonSpecificCountTip".Translate());
			}
			else
			{
				stringBuilder.AppendLine("AbandonAllTip".Translate());
			}
			stringBuilder.AppendLine();
			stringBuilder.Append("AbandonItemTipExtraText".Translate());
			return stringBuilder.ToString();
		}
	}
}
