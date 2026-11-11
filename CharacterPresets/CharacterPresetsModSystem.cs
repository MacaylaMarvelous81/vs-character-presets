using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CharacterPresets;

public class CharacterPresetsModSystem : ModSystem
{
	private Harmony _harmony;

	public override void Start(ICoreAPI api)
	{
		if (!Harmony.HasAnyPatches(Mod.Info.ModID))
		{
			_harmony = new Harmony(Mod.Info.ModID);
			_harmony.PatchAll();
		}
	}
}
