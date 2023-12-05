using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;

namespace SCPSLAfkCheck
{
    public class Plugin
    {
        public static Plugin Singleton { get; private set; }
        public const string Version = "13.3.1";
        [PluginConfig] public Config Config;

        [PluginPriority(PluginAPI.Enums.LoadPriority.High)]
        [PluginEntryPoint("SCPSL AFK Checker", Version, ".", "Aster")]
        public void LoadPlugin()
        {
            Singleton = this;
            EventManager.RegisterEvents<EventHandler>(this);
            var handler = PluginHandler.Get(this);
            handler.LoadConfig(this, nameof(PluginConfig));
            FactoryManager.RegisterPlayerFactory(this, new AfkPlayerFactory());

            Log.Info($"Plugin {handler.PluginName} loaded.");
        }
    }
}