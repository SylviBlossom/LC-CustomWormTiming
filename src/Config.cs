using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using System.Runtime.Serialization;

namespace CustomWormTiming;

[DataContract]
public class Config : SyncedConfig2<Config>
{
	[SyncedEntryField] public SyncedEntry<float> EmergeDelayMin;
	[SyncedEntryField] public SyncedEntry<float> EmergeDelayMax;

	public Config(ConfigFile cfg) : base(PluginInfo.PLUGIN_GUID)
	{
		EmergeDelayMin = cfg.BindSyncedEntry("General", "EmergeDelayMin", 1f, "Minimum seconds for the worm to emerge. (Vanilla is 1)");
		EmergeDelayMax = cfg.BindSyncedEntry("General", "EmergeDelayMax", 2f, "Maximum seconds for the worm to emerge. (Vanilla is 2)");

		ConfigManager.Register(this);
	}
}
