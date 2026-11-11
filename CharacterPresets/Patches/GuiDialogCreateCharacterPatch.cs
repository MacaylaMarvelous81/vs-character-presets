using System;
using System.Collections.Generic;
using System.Linq;
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
		newTabs[tabs.Length] = new GuiTab
		{
			Name = Lang.Get("characterpresets:tab-presets"),
			DataInt = tabs.Length,
		};

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
			Dictionary<string, Dictionary<string, string>> presets;

			try
			{
				presets =
					capi.LoadModConfig<Dictionary<string, Dictionary<string, string>>>(
						"CharacterPresets-presets.json"
					) ?? new();
			}
			catch (Exception e)
			{
				createCharacterComposer
					.AddStaticText(
						$"An exception occured when loading the character presets: {e.Message}",
						CairoFont.WhiteSmallText(),
						ElementBounds.Fixed(0, 20, 717, 20)
					)
					.AddStaticText(
						e.StackTrace,
						CairoFont.WhiteSmallText(),
						ElementBounds.Fixed(0, 60, 717, dlgHeight - 80)
					);
				return ElementBounds.Empty;
			}

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

			ElementBounds presetDropDownBounds = ElementBounds
				.Fixed(0, ypos + 25, 400, slotsize - 4 - 8)
				.FixedRightOf(insetSlotBounds, 20);

			CairoFont font = CairoFont.WhiteMediumText();

			ElementBounds newPresetNameBounds = ElementBounds
				.Fixed(0, 0, 380, 25)
				.FixedUnder(presetDropDownBounds, 40)
				.FixedRightOf(insetSlotBounds, 20);

			ElementBounds newButtonBounds = ElementBounds
				.Fixed(0, dlgHeight - 60)
				.WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedPadding(12, 6);

			createCharacterComposer
				.AddInset(insetSlotBounds, 2)
				.AddDropDown(
					presets.Keys.Prepend("").ToArray(),
					presets.Keys.Prepend(Lang.Get("characterpresets:preset-none")).ToArray(),
					0,
					(code, selected) =>
					{
						if (code == "")
							return;

						AccessTools
							.Method(typeof(GuiDialogCreateCharacter), "OnRandomizeSkin")
							.Invoke(instance, [presets[code]]);
					},
					presetDropDownBounds,
					"presetDropDown"
				)
				.AddRichtext(
					Lang.Get("characterpresets:preset-save-text"),
					CairoFont.WhiteDetailText(),
					ElementBounds
						.Fixed(0, 0, 480, 20)
						.FixedUnder(presetDropDownBounds, 20)
						.FixedRightOf(insetSlotBounds, 20)
				)
				.AddTextInput(newPresetNameBounds, (text) => { }, key: "newPresetName")
				.AddButton(
					Lang.Get("characterpresets:preset-new"),
					() =>
					{
						string name = createCharacterComposer
							.GetTextInput("newPresetName")
							.GetText();

						if (name == "" || presets.ContainsKey(name))
							return true;

						EntityBehaviorExtraSkinnable skinMod =
							capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();

						Dictionary<string, string> parts = new Dictionary<string, string>();

						foreach (
							AppliedSkinnablePartVariant appliedPart in skinMod.AppliedSkinParts
						)
						{
							parts.Add(appliedPart.PartCode, appliedPart.Code);
						}

						presets.Add(name, parts);
						capi.StoreModConfig<Dictionary<string, Dictionary<string, string>>>(
							presets,
							"CharacterPresets-presets.json"
						);

						createCharacterComposer
							.GetDropDown("presetDropDown")
							.SetList(
								presets.Keys.Prepend("").ToArray(),
								presets
									.Keys.Prepend(Lang.Get("characterpresets:preset-none"))
									.ToArray()
							);
						createCharacterComposer
							.GetDropDown("presetDropDown")
							.SetSelectedValue(name);
						createCharacterComposer.GetTextInput("newPresetName").SetValue("");

						return true;
					},
					ElementBounds
						.Fixed(0, 0)
						.FixedUnder(newPresetNameBounds, 20)
						.FixedRightOf(insetSlotBounds, 20)
						.WithFixedPadding(12, 6),
					CairoFont.WhiteSmallText(),
					EnumButtonStyle.Small
				)
				.AddButton(
					Lang.Get("characterpresets:preset-delete"),
					() =>
					{
						string name = createCharacterComposer
							.GetDropDown("presetDropDown")
							.SelectedValue;

						if (name == "")
							return true;

						presets.Remove(name);
						createCharacterComposer
							.GetDropDown("presetDropDown")
							.SetList(
								presets.Keys.Prepend("").ToArray(),
								presets
									.Keys.Prepend(Lang.Get("characterpresets:preset-none"))
									.ToArray()
							);
						createCharacterComposer.GetDropDown("presetDropDown").SetSelectedValue("");

						return true;
					},
					ElementBounds
						.Fixed(0, dlgHeight - 25)
						.FixedRightOf(insetSlotBounds, 20)
						.WithFixedPadding(12, 6),
					CairoFont.WhiteSmallText(),
					EnumButtonStyle.Small
				)
				.AddSmallButton(
					Lang.Get("Confirm Skin"),
					() =>
						(bool)
							AccessTools
								.Method(typeof(GuiDialogCreateCharacter), "OnNext")
								.Invoke(instance, null),
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
