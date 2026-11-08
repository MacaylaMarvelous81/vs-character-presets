using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace CharacterPresets.Patches;

[HarmonyPatch]
internal class GuiDialogCreateCharacterPatch
{
	[HarmonyTranspiler]
	[HarmonyPatch(typeof(GuiDialogCreateCharacter), "ComposeGuis")]
	private static IEnumerable<CodeInstruction> TranspileAddTabs(
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
					yield return instruction;
					break;
			}
		}
	}

	private static GuiTab[] ExtendTabs(GuiTab[] tabs)
	{
		GuiTab[] newTabs = new GuiTab[tabs.Length + 1];
		tabs.CopyTo(newTabs, 0);
		newTabs[tabs.Length] = new GuiTab { Name = "Presets", DataInt = tabs.Length };
		return newTabs;
	}
}
