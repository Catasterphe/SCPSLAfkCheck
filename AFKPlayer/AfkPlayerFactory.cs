using PluginAPI.Core.Factories;
using PluginAPI.Core.Interfaces;
using PluginAPI.Core;
using System;

namespace SCPSLAfkCheck
{
    public class AfkPlayerFactory : PlayerFactory
    {
        public override Type BaseType { get; } = typeof(AfkPlayer);
        public override Player Create(IGameComponent component) => new AfkPlayer(component);
    }
}
