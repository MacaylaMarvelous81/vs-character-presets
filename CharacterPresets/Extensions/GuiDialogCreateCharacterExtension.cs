using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace CharacterPresets.Extensions;

public class GuiDialogCreateCharacterExtension
{
	private static readonly ConditionalWeakTable<
		GuiDialogCreateCharacter,
		GuiDialogCreateCharacterExtension
	> ExtensionTable = new();

	public string[] PresetValueList =>
		this.Presets?.Keys.Prepend("").ToArray() ?? new string[] { "" };
	public string[] PresetNameList =>
		this.Presets?.Keys.Prepend(Lang.Get("characterpresets:preset-none")).ToArray()
		?? new string[] { Lang.Get("characterpresets:preset-none") };

	public int PresetTabDataInt;
	public IDictionary<string, IDictionary<string, string>>? Presets;
	public Exception? PresetStorageException;

	public static GuiDialogCreateCharacterExtension FromOriginal(GuiDialogCreateCharacter original)
	{
		return ExtensionTable.GetValue(original, key => new GuiDialogCreateCharacterExtension());
	}
}
