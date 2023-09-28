namespace Rasa.Managers
{
    using Game;
    using Repositories.UnitOfWork;
    using Structures;
    public class LogosManager
    {
        private static LogosManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public static LogosManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new LogosManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private LogosManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        internal void LogosInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var logosList = unitOfWork.Logoses.GetLogos();

            foreach (var entry in logosList)
            {
                var mapChannel = MapChannelManager.Instance.MapChannelArray[entry.MapContextId];
                mapChannel.DynamicObjects.Add(new Logos(entry));
            }
        }
    }
}
