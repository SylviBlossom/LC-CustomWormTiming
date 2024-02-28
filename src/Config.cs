using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using System;
using System.Runtime.Serialization;
using Unity.Netcode;

namespace CustomWormTiming;

[DataContract]
public class Config : SyncedConfig<Config>
{
	[DataMember] public SyncedEntry<float> EmergeDelayMin { get; private set; }
	[DataMember] public SyncedEntry<float> EmergeDelayMax { get; private set; }

	public Config(ConfigFile cfg) : base(PluginInfo.PLUGIN_GUID)
	{
		ConfigManager.Register(this);

		EmergeDelayMin = cfg.BindSyncedEntry("General", "EmergeDelayMin", 2f / 3f, "Minimum seconds for the worm to emerge. (Vanilla is 0.33..)");
		EmergeDelayMax = cfg.BindSyncedEntry("General", "EmergeDelayMax", 6f / 3f, "Maximum seconds for the worm to emerge. (Vanilla is 2)");

		EmergeDelayMin.SettingChanged += OnSettingChanged;
		EmergeDelayMax.SettingChanged += OnSettingChanged;
	}

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

				OnRequestSync(clientId, default);
			}
		}
		catch (Exception e)
		{
			Plugin.Logger.LogInfo($"Error occurred syncing config after modification: {e}");
		}
	}
}
