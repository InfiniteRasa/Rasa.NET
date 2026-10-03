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

            // What each battlefield's Logos title asks for is where the shrines are.
            LogosTitles.Load(logosList);

            foreach (var entry in logosList)
            {
                // A shrine on a map the server does not have is a data error, not a reason to
                // fail startup: record it against the map and carry on.
                if (!MapChannelManager.Instance.MapChannelArray.TryGetValue(entry.MapContextId, out var mapChannel))
                {
                    MapErrorManager.Instance.Record(entry.MapContextId,
                        $"Logos shrine {entry.Id} ({entry.Name}) is placed on map {entry.MapContextId}, which is not loaded.");
                    continue;
                }

                mapChannel.DynamicObjects.Add(new Logos(entry));
            }
        }
    }
}
