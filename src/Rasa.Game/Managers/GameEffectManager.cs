namespace Rasa.Managers
{
    using Packets.MapChannel.Server;
    using Structures;

    public class GameEffectManager
    {
        private static GameEffectManager _instance;
        private static readonly object InstanceLock = new object();
        public static GameEffectManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new GameEffectManager();
                    }
                }

                return _instance;
            }
        }

        private GameEffectManager()
        {
        }

        public void AddToList(Actor actor, GameEffect gameEffect)
        {
            actor.ActiveEffects.Add(gameEffect.EffectId, gameEffect);
        }

        public void AttachSprint(MapChannelClient mapClient, Actor actor, int effectLevel, int duration)
        {
            mapClient.MapChannel.CurrentEffectId++; // generate new effectId
            var effectId = mapClient.MapChannel.CurrentEffectId;
            // effectId -> The id used to identify the effect when sending/receiving effect related data (similiar to entityId, just for effects)
            // typeId -> The id used to lookup the effect class and animation
            // level -> The sub id of the effect, some effects have multiple levels (especially the ones linked with player abilities)
            // create effect struct
            var gameEffect = new GameEffect();
            // setup struct
            gameEffect.Duration = duration; // 5 seconds (test)
            gameEffect.EffectTime = 0; // reset timer
            gameEffect.TypeId = 247;    // EFFECT_TYPE_SPRINT;
            gameEffect.EffectId = effectId;
            gameEffect.EffectLevel = effectLevel;
            // add to list
            AddToList(actor, gameEffect);
            mapClient.Player.Client.CellSendPacket(mapClient, actor.EntityId, new GameEffectAttachedPacket
            {
                EffectTypeId = gameEffect.TypeId,
                EffectId = gameEffect.EffectId,
                EffectLevel = gameEffect.EffectLevel,
                SourceId = (int)actor.EntityId,
                Announced = true,
                Duration = gameEffect.Duration,
                DamageType = 0,
                AttrId = 1,
                IsActive = true,
                IsBuff = true,
                IsDebuff = false,
                IsNegativeEffect = false
            });
            // do ability specific work
            UpdateMovementMod(mapClient, actor);
        }

        public void DettachEffect(MapChannelClient mapClient, Actor actor, GameEffect gameEffect)
        {
            // inform clients (Recv_GameEffectDetached 75)
            mapClient.Player.Client.CellSendPacket(mapClient, actor.EntityId, new GameEffectDetachedPacket { EffectId = gameEffect.EffectId });
            // remove from list
            RemoveFromList(actor, gameEffect);
            // do ability specific work
            if (gameEffect.TypeId == 247)
                UpdateMovementMod(mapClient, actor);
            // more todo..
        }

        public void DoWork(MapChannel mapChannel, long passedTime)
        {
            foreach (var mapClient in mapChannel.PlayerList)
            {
                if (mapClient.Player == null)
                    continue;
                var actor = mapClient.Player.Actor;
                var gameEffect = actor.ActiveEffects;

                // This need future work
                foreach (var t in gameEffect)
                {
                    var effect = t.Value;
                    effect.EffectTime += (int)passedTime;
                    // stop effect if too old
                    if (effect.EffectTime >= effect.Duration)
                    {
                        DettachEffect(mapClient, actor, effect);
                        break;
                    }

                }

            }
            
        }
        

        public void RemoveFromList(Actor actor, GameEffect gameEffect)
        {
            actor.ActiveEffects.Remove(gameEffect.EffectId);
        }

        public void UpdateMovementMod(MapChannelClient mapClient, Actor actor)
        {
            var movementMod = 1.0d;
            // check for sprint
            foreach (var t in actor.ActiveEffects)
            {
                var efect = t.Value;
                if (efect.TypeId == 247) // ToDO curently hardcoded EFFECT_TYPE_SPRINT
                {
                    // apply sprint bonus
                    movementMod += 1.0d;
                    movementMod += efect.EffectLevel * 0.10d;
                    break;
                }
            }
            // todo: other modificators?
            // ToDO send on cell domain
            mapClient.Player.Client.CellSendPacket(mapClient, actor.EntityId, new MovementModChangePacket(movementMod));
        }
    }
}
