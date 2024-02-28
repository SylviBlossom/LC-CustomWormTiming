using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;

namespace CustomWormTiming;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance { get; private set; }
	public static new Config Config { get; private set; }
	public static new ManualLogSource Logger {  get; private set; }

    private void Awake()
    {
		Instance = this;
		Config = new(base.Config);
		Logger = base.Logger;

		var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
		harmony.PatchAll(typeof(Plugin));

		Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(SandWormAI), "EmergeFromGround", MethodType.Enumerator)]
	private static void SandWormAI_EmergeFromGround(ILContext il)
	{
		var cursor = new ILCursor(il);

		if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchNewobj<WaitForSeconds>()))
		{
			Logger.LogError("Failed IL hook for SandWormAI.EmergeFromGround");
			return;
		}

		cursor.Emit(OpCodes.Pop);
		cursor.Emit(OpCodes.Ldloc_1);
		cursor.EmitDelegate<Func<SandWormAI, float>>(self =>
		{
			var range = Config.Instance.EmergeDelayMax.Value - Config.Instance.EmergeDelayMin.Value;

			return Config.Instance.EmergeDelayMin.Value + (float)self.sandWormRandom.NextDouble() * range;
		});
	}
}