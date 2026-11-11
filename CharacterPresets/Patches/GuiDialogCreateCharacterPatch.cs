using System.Collections.Generic;
using System.Reflection.Emit;
using CharacterPresets.Extensions;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace CharacterPresets.Patches;

[HarmonyPatch]
internal class GuiDialogCreateCharacterPatch
{
	[HarmonyTranspiler]
	[HarmonyPatch(typeof(GuiDialogCreateCharacter), "ComposeGuis")]
	private static IEnumerable<CodeInstruction> TranspileComposeGuis(
		IEnumerable<CodeInstruction> instructions
	)
	{
		int phase = 0;
		foreach (CodeInstruction instruction in instructions)
		{
			switch (phase)
			{
				case 0:
					if (
						instruction.StoresField(
							AccessTools.Field(typeof(GuiTab), nameof(GuiTab.DataInt))
						)
					)
					{
						phase++;
					}
					yield return instruction;
					break;
				case 1:
					if (instruction.IsStloc())
					{
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(
							OpCodes.Call,
							AccessTools.Method(
								typeof(GuiDialogCreateCharacterPatch),
								nameof(GuiDialogCreateCharacterPatch.ExtendTabs)
							)
						);
						yield return instruction;
						phase++;
					}
					else
					{
						yield return instruction;
					}
					break;
				case 2:
					if (
						instruction.Calls(
							AccessTools.Method(typeof(GuiComposer), nameof(GuiComposer.Compose))
						)
					)
					{
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Dup);

						yield return new CodeInstruction(OpCodes.Dup);
						yield return new CodeInstruction(
							OpCodes.Ldfld,
							AccessTools.Field(typeof(GuiDialogCreateCharacter), "capi")
						);

						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(
							OpCodes.Ldfld,
							AccessTools.Field(typeof(GuiDialogCreateCharacter), "curTab")
						);

						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(
							OpCodes.Ldfld,
							AccessTools.Field(typeof(GuiDialogCreateCharacter), "dlgHeight")
						);

						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(
							OpCodes.Ldfld,
							AccessTools.Field(typeof(GuiDialogCreateCharacter), "insetSlotBounds")
						);

						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(
							OpCodes.Ldfld,
							AccessTools.Field(typeof(GuiDialogCreateCharacter), "rows")
						);

						yield return new CodeInstruction(
							OpCodes.Call,
							AccessTools.Method(
								typeof(GuiDialogCreateCharacterPatch),
								nameof(GuiDialogCreateCharacterPatch.ExtendComposition)
							)
						);
						yield return new CodeInstruction(
							OpCodes.Stfld,
							AccessTools.Field(typeof(GuiDialogCreateCharacter), "insetSlotBounds")
						);

						yield return instruction;
						phase++;
					}
					else
					{
						yield return instruction;
					}
					break;
				case 3:
					yield return instruction;
					break;
			}
		}
	}

	private static GuiTab[] ExtendTabs(GuiTab[] tabs, GuiDialogCreateCharacter instance)
	{
		GuiTab[] newTabs = new GuiTab[tabs.Length + 1];
		tabs.CopyTo(newTabs, 0);
		// Assumes that tab DataInts are consecutive and start from 0.
		newTabs[tabs.Length] = new GuiTab { Name = "Presets", DataInt = tabs.Length };

		GuiDialogCreateCharacterExtension guiExtension =
			GuiDialogCreateCharacterExtension.FromOriginal(instance);
		guiExtension.PresetTabDataInt = tabs.Length;

		return newTabs;
	}

	private static ElementBounds ExtendComposition(
		GuiDialogCreateCharacter instance,
		ICoreClientAPI capi,
		int curTab,
		int dlgHeight,
		ElementBounds origInsetSlotBounds,
		int rows
	)
	{
		GuiDialogCreateCharacterExtension guiExtension =
			GuiDialogCreateCharacterExtension.FromOriginal(instance);
		GuiComposer createCharacterComposer = instance.Composers["createcharacter"];
		if (curTab == guiExtension.PresetTabDataInt)
		{
			// Based on the interface for class selection tab
			EntityBehaviorPlayerInventory playerInventory =
				capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
			playerInventory.hideClothing = instance.charNaked;

			EntityShapeRenderer essr =
				capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer;
			essr.TesselateShape();

			double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
			double slotsize = GuiElementPassiveItemSlot.unscaledSlotSize;
			double ypos = 10 + pad;

			ElementBounds leftColBounds = ElementBounds
				.Fixed(0, ypos, 0, dlgHeight - 47)
				.FixedGrow(2 * pad, 2 * pad);
			ElementBounds insetSlotBounds = ElementBounds
				.Fixed(0, ypos + 25, 265, leftColBounds.fixedHeight - 2 * pad + 10)
				.FixedRightOf(leftColBounds, 10);

			ElementBounds rightSlotBounds = ElementStdBounds
				.SlotGrid(EnumDialogArea.None, 0, ypos, 1, rows)
				.FixedGrow(2 * pad, 2 * pad)
				.FixedRightOf(insetSlotBounds, 10);
			ElementBounds prevButtonBounds = ElementBounds
				.Fixed(0, ypos + 25, 35, slotsize - 4)
				.WithFixedPadding(2)
				.FixedRightOf(insetSlotBounds, 20);
			ElementBounds centerTextBounds = ElementBounds
				.Fixed(0, ypos + 25, 200, slotsize - 4 - 8)
				.FixedRightOf(prevButtonBounds, 20);

			ElementBounds presetInset = centerTextBounds.ForkBoundingParent(4, 4, 4, 4);

			ElementBounds nextButtonBounds = ElementBounds
				.Fixed(0, ypos + 25, 35, slotsize - 4)
				.WithFixedPadding(2)
				.FixedRightOf(presetInset, 20);

			CairoFont font = CairoFont.WhiteMediumText();
			centerTextBounds.fixedY +=
				(centerTextBounds.fixedHeight - font.GetFontExtents().Height / RuntimeEnv.GUIScale)
				/ 2;

			ElementBounds newPresetNameBounds = ElementBounds
				.Fixed(0, 0, 380, 25)
				.FixedUnder(prevButtonBounds, 40)
				.FixedRightOf(insetSlotBounds, 20);

			ElementBounds newButtonBounds = ElementBounds
				.Fixed(0, dlgHeight - 60)
				.WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedPadding(12, 6);

			createCharacterComposer
				.AddInset(insetSlotBounds, 2)
				.AddIconButton("left", (on) => { }, prevButtonBounds.FlatCopy())
				.AddInset(presetInset, 2)
				.AddDynamicText(
					"",
					font.Clone().WithOrientation(EnumTextOrientation.Center),
					centerTextBounds,
					"presetName"
				)
				.AddIconButton("right", (on) => { }, nextButtonBounds.FlatCopy())
				// .AddRichtext("", CairoFont.WhiteDetailText(), charTextBounds, "characterDesc")
				.AddRichtext(
					"...or save this character with the following name:",
					CairoFont.WhiteDetailText(),
					ElementBounds
						.Fixed(0, 0, 480, 20)
						.FixedUnder(prevButtonBounds, 20)
						.FixedRightOf(insetSlotBounds, 20)
				)
				.AddTextInput(newPresetNameBounds, (text) => { }, key: "newPresetName")
				.AddSmallButton(
					"Create New Preset",
					() =>
					{
						return true;
					},
					ElementBounds
						.Fixed(0, 0)
						.FixedUnder(newPresetNameBounds, 20)
						.FixedRightOf(insetSlotBounds, 20)
						.WithFixedPadding(12, 6),
					EnumButtonStyle.Normal
				)
				.AddSmallButton(
					"Select Preset",
					() =>
					{
						return true;
					},
					ElementBounds
						.Fixed(0, dlgHeight - 25)
						.WithAlignment(EnumDialogArea.RightFixed)
						.WithFixedPadding(12, 6),
					EnumButtonStyle.Normal
				);

			return insetSlotBounds;
		}

		return origInsetSlotBounds;
	}
}
