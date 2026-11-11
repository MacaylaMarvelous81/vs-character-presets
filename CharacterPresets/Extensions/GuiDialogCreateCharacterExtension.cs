using System.Runtime.CompilerServices;
using Vintagestory.GameContent;

namespace CharacterPresets.Extensions;

public class GuiDialogCreateCharacterExtension
{
	private static readonly ConditionalWeakTable<
		GuiDialogCreateCharacter,
		GuiDialogCreateCharacterExtension
	> ExtensionTable = new();

	public int PresetTabDataInt;

	public static GuiDialogCreateCharacterExtension FromOriginal(GuiDialogCreateCharacter original)
	{
		return ExtensionTable.GetValue(original, key => new GuiDialogCreateCharacterExtension());
	}
}
