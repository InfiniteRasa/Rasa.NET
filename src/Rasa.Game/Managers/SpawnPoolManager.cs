namespace Rasa.Managers
{
    using Structures;

    public class SpawnPoolManager
    {
        private static SpawnPoolManager _instance;
        private static readonly object InstanceLock = new object();

        public static SpawnPoolManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new SpawnPoolManager();
                    }
                }

                return _instance;
            }
        }

        private SpawnPoolManager()
        {
        }

        public void IncreaseAliveCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.AliveCreatures++;
        }

        void IncreaseDeadCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.DeadCreatures++;
        }
    }
}
