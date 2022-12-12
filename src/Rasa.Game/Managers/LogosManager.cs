using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Structures;
    public class LogosManager
    {
        private static LogosManager _instance;
        private static readonly object InstanceLock = new object();
        private List<int> States = new List<int>();

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
                            _instance = new LogosManager();
                    }
                }

                return _instance;
            }
        }

        private LogosManager()
        {
        }

        internal void LogosInit()
        {
            var logosList = LogosTable.LoadLogos();

            foreach (var entry in logosList)
            {
                var mapChannel = MapChannelManager.Instance.MapChannelArray[entry.MapContextId];
                mapChannel.DynamicObjects.Add(new Logos(entry));
            }
        }
    }
}
