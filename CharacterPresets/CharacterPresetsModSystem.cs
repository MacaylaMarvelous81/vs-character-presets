using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using HarmonyLib;

namespace CharacterPresets;

public class CharacterPresetsModSystem : ModSystem
{
	private Harmony _harmony;

    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {
		if (!Harmony.HasAnyPatches(Mod.Info.ModID))
		{
			_harmony = new Harmony(Mod.Info.ModID);
			_harmony.PatchAll();
		}
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("characterpresets:hello"));
    }

}
