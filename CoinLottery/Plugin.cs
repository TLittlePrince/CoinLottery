using System;
using Exiled.API.Features;
using UnityEngine;

namespace CoinLottery
{
    public class Plugin: Plugin<Config>
    {
        public override string Author => "Little Prince";
        public override string Name => "CoinLottery";
        public override Version Version => new Version(1, 0, 0);

        private EventHandlers EventHandlers { get; set; }

        public override void OnEnabled()
        {
            EventHandlers = new EventHandlers(Config);
            Exiled.Events.Handlers.Player.FlippingCoin += EventHandlers.OnFlippingCoin;
            Exiled.Events.Handlers.Player.Spawned += EventHandlers.OnSpawned;
            Exiled.Events.Handlers.Player.Died += EventHandlers.OnDied;
            base.OnEnabled();
        }
        
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.FlippingCoin -= EventHandlers.OnFlippingCoin;
            Exiled.Events.Handlers.Player.Spawned -= EventHandlers.OnSpawned;
            Exiled.Events.Handlers.Player.Died -= EventHandlers.OnDied;
            EventHandlers = null;
            base.OnDisabled();
        }
    }
}