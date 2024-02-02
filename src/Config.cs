using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Netcode;

namespace CustomWormTiming;

[DataContract]
public class Config : SyncedInstance<Config>
{
	[DataMember] public SyncedEntry<float> EmergeDelayMin { get; private set; }
	[DataMember] public SyncedEntry<float> EmergeDelayMax { get; private set; }

	public Config(ConfigFile cfg)
	{
		InitInstance(this);

		EmergeDelayMin = cfg.BindSyncedEntry("General", "EmergeDelayMin", 2f / 3f, "Minimum seconds for the worm to emerge. (Vanilla is 0.33..)");
		EmergeDelayMax = cfg.BindSyncedEntry("General", "EmergeDelayMax", 6f / 3f, "Maximum seconds for the worm to emerge. (Vanilla is 2)");

		EmergeDelayMin.SettingChanged += OnSettingChanged;
		EmergeDelayMax.SettingChanged += OnSettingChanged;
	}

	#region Syncing Methods

	private void OnSettingChanged(object sender, EventArgs _)
	{
		if (!IsHost) return;

		try
		{
			foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				if (NetworkManager.Singleton.LocalClientId == clientId)
				{
					continue;
				}

				SyncWithClient(clientId);
			}
		}
		catch (Exception e)
		{
			Plugin.Logger.LogInfo($"Error occurred syncing config after modification: {e}");
		}
	}

	internal static void RequestSync()
	{
		if (!IsClient) return;

		using FastBufferWriter stream = new(IntSize, Allocator.Temp);

		// Method `OnRequestSync` will then get called on host.
		stream.SendMessage($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync");
	}

	internal static void OnRequestSync(ulong clientId, FastBufferReader _)
	{
		SyncWithClient(clientId);
	}

	internal static void SyncWithClient(ulong clientId)
	{
		if (!IsHost) return;

		byte[] array = SerializeToBytes(Instance);
		int value = array.Length;

		using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

		try
		{
			stream.WriteValueSafe(in value, default);
			stream.WriteBytesSafe(array);

			stream.SendMessage($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", clientId);
		}
		catch (Exception e)
		{
			Plugin.Logger.LogError($"Error occurred syncing config with client: {clientId}\n{e}");
		}
	}

	internal static void OnReceiveSync(ulong _, FastBufferReader reader)
	{
		if (!reader.TryBeginRead(IntSize))
		{
			Plugin.Logger.LogError("Config sync error: Could not begin reading buffer.");
			return;
		}

		reader.ReadValueSafe(out int val, default);
		if (!reader.TryBeginRead(val))
		{
			Plugin.Logger.LogError("Config sync error: Host could not sync.");
			return;
		}

		byte[] data = new byte[val];
		reader.ReadBytesSafe(ref data, val);

		try
		{
			SyncInstance(data);
		}
		catch (Exception e)
		{
			Plugin.Logger.LogError($"Error syncing config instance!\n{e}");
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
	public static void InitializeLocalPlayer()
	{
		if (IsHost)
		{
			MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync", OnRequestSync);
			Synced = true;

			return;
		}

		Synced = false;
		MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", OnReceiveSync);
		RequestSync();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
	public static void PlayerLeave()
	{
		RevertSync();
	}

	#endregion
}
