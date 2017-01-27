namespace Rasa.Managers
{
    using Structures;

    public class ActorActionManager
    {
        private static ActorActionManager _instance;
        private static readonly object InstanceLock = new object();

        public static ActorActionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ActorActionManager();
                    }
                }

                return _instance;
            }
        }

        private ActorActionManager()
        {
        }

        /*bool ActionCompletedTimer(MapChannel mapChannel, void* param, int timePassed)
        {
            var actor = (actor_t*)param;
            actor_completeCurrentAction(mapChannel, actor);
            // return false to delete the timer immediately
            return false;
        }

        public void StartActionOnEntity(MapChannel mapChannel, Actor actor, uint targetEntityId, int actionId, int actionArgId, int windupTime, int recoverTimepublic, ActorCurrentAction.ActorActionUpdateCallback callback)
        {
            if (actor.CurrentAction.ActionId != 0)
                return;
            // update current action data
            actor.CurrentAction.ActionId = actionId;
            actor.CurrentAction.ActionArgId = actionArgId;
            actor.CurrentAction.TargetEntityId = targetEntityId;
            ActorActionUpdateCallback = callback;
            if (windupTime >= 0)
                mapChannel.RegisterTimer(mapChannel, windupTime, actor, _cb_actor_actionCompletedTimer);
            else if (windupTime == 0)

                _cb_actor_actionCompletedTimer(mapChannel, actor, 0);
        }*/
    }
}
