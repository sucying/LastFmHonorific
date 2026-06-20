using Dalamud.Configuration;
using Dalamud.Plugin;
using LastFmHonorific.Activities;
using LastFmHonorific.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

namespace LastFmHonorific;

[Serializable]
public class Config : IPluginConfiguration
{
    [field: NonSerialized]
    private Lock _syncLock = new();

    [field: NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public int Version { get; set; }
    public bool Enabled { get; set; } = true;

    public string LastFmUsername { get; set; } = string.Empty;
    public string LastFmApiKey { get; set; } = string.Empty;

    public bool EnableDebugLogging { get; set; }
    public bool EnableNotifications { get; set; } = true;
    public bool IsHonorificSupporter { get; set; }

    public string ActiveConfigName { get; set; } = string.Empty;
    public List<ActivityConfig> ActivityConfigs { get; set; } = [];

    public Config() { }

    public Config(List<ActivityConfig> activityConfigs)
    {
        ActivityConfigs = activityConfigs;
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        _syncLock = new Lock();
    }

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        lock (_syncLock)
        {
            var interfaceToUse = _pluginInterface ?? Plugin.PluginInterface;
            interfaceToUse.SavePluginConfig(this);
        }
    }

    public T WithLock<T>(Func<T> action)
    {
        lock (_syncLock)
        {
            return action();
        }
    }

    public void WithLock(Action action)
    {
        lock (_syncLock)
        {
            action();
        }
    }

    /// <summary>
    /// Last.fm only needs a username (whose profile to read) and an API key
    /// (identifies our app to Last.fm) - no OAuth tokens, unlike Spotify.
    /// </summary>
    public bool HasLastFmCredentials()
        => WithLock(() => !string.IsNullOrWhiteSpace(LastFmUsername) && !string.IsNullOrWhiteSpace(LastFmApiKey));

    public bool Validate(out List<string> errors)
        => ConfigValidator.Validate(this, out errors);
}
