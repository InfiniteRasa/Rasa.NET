using System;

namespace Rasa.Game
{
    internal sealed class ServerStartupLifecycle
    {
        private readonly Func<bool> _validateMissionReadiness;
        private readonly Func<bool> _validateLootReadiness;
        private readonly Action _startLoop;
        private readonly Action _setupCommunicator;
        private readonly Action _createListener;
        private readonly Action _registerLoginAndQueue;
        private readonly Action _beginAccept;
        private readonly Action _registerTimers;
        private readonly Action _loadRemainingData;
        private readonly Action _publishReady;
        private readonly Action _cleanup;
        private readonly Action<string, Exception> _reportFailure;

        internal ServerStartupLifecycle(
            Func<bool> validateMissionReadiness,
            Func<bool> validateLootReadiness,
            Action startLoop,
            Action setupCommunicator,
            Action createListener,
            Action registerLoginAndQueue,
            Action beginAccept,
            Action registerTimers,
            Action loadRemainingData,
            Action publishReady,
            Action cleanup,
            Action<string, Exception> reportFailure = null)
        {
            _validateMissionReadiness = validateMissionReadiness ??
                                        throw new ArgumentNullException(nameof(validateMissionReadiness));
            _validateLootReadiness = validateLootReadiness ??
                                    throw new ArgumentNullException(nameof(validateLootReadiness));
            _startLoop = startLoop ?? throw new ArgumentNullException(nameof(startLoop));
            _setupCommunicator = setupCommunicator ?? throw new ArgumentNullException(nameof(setupCommunicator));
            _createListener = createListener ?? throw new ArgumentNullException(nameof(createListener));
            _registerLoginAndQueue = registerLoginAndQueue ??
                                     throw new ArgumentNullException(nameof(registerLoginAndQueue));
            _beginAccept = beginAccept ?? throw new ArgumentNullException(nameof(beginAccept));
            _registerTimers = registerTimers ?? throw new ArgumentNullException(nameof(registerTimers));
            _loadRemainingData = loadRemainingData ?? throw new ArgumentNullException(nameof(loadRemainingData));
            _publishReady = publishReady ?? throw new ArgumentNullException(nameof(publishReady));
            _cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
            _reportFailure = reportFailure ?? LogFailure;
        }

        internal bool Start()
        {
            // What it was doing when it failed, for the report: a missing database table, a port
            // in use and a bad navmesh path all used to end in "Unable to start server!" and
            // nothing else, and an exception out of the mission check - the first read of the
            // world database - was not caught here at all.
            var step = "loading the entity classes and checking the missions";

            try
            {
                if (!_validateMissionReadiness())
                    return false;
                step = "checking the ordinary loot tables";
                if (!_validateLootReadiness())
                    return false;

                // The world first, then the doors: everything loaded before anything can connect,
                // the loop started once there are ports to serve, and the auth server told last,
                // since that is what puts this world on the server list.
                step = "loading the world";
                _loadRemainingData();
                step = "opening the client port";
                _createListener();
                step = "setting up the login queue";
                _registerLoginAndQueue();
                step = "starting the timers";
                _registerTimers();
                step = "accepting clients";
                _beginAccept();
                step = "starting the world loop";
                _startLoop();
                step = "setting up the link to the auth server";
                _setupCommunicator();
                step = "announcing the server ready";
                _publishReady();
                return true;
            }
            catch (Exception e)
            {
                _reportFailure(step, e);

                // Whatever was opened is closed; a cleanup that fails as well is reported too,
                // and does not take the place of the failure that started it.
                try
                {
                    _cleanup();
                }
                catch (Exception cleanupFailure)
                {
                    _reportFailure("cleaning up after the failed start", cleanupFailure);
                }

                return false;
            }
        }

        private static void LogFailure(string step, Exception e)
        {
            Logger.WriteLog(LogType.Error, $"Startup failed while {step}:");
            Logger.WriteLog(LogType.Error, e);
        }
    }
}
