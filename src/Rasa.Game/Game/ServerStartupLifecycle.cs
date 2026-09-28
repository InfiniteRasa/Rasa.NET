using System;

namespace Rasa.Game
{
    internal sealed class ServerStartupLifecycle
    {
        private readonly Func<bool> _validateMissionReadiness;
        private readonly Action _startLoop;
        private readonly Action _setupCommunicator;
        private readonly Action _createListener;
        private readonly Action _registerLoginAndQueue;
        private readonly Action _beginAccept;
        private readonly Action _registerTimers;
        private readonly Action _loadRemainingData;
        private readonly Action _publishReady;
        private readonly Action _cleanup;

        internal ServerStartupLifecycle(
            Func<bool> validateMissionReadiness,
            Action startLoop,
            Action setupCommunicator,
            Action createListener,
            Action registerLoginAndQueue,
            Action beginAccept,
            Action registerTimers,
            Action loadRemainingData,
            Action publishReady,
            Action cleanup)
        {
            _validateMissionReadiness = validateMissionReadiness ??
                                        throw new ArgumentNullException(nameof(validateMissionReadiness));
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
        }

        internal bool Start()
        {
            if (!_validateMissionReadiness())
                return false;

            try
            {
                _startLoop();
                _setupCommunicator();
                _createListener();
                _registerLoginAndQueue();
                _beginAccept();
                _registerTimers();
                _loadRemainingData();
                _publishReady();
                return true;
            }
            catch
            {
                _cleanup();
                return false;
            }
        }
    }
}
