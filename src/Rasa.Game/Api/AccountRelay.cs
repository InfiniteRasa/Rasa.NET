using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa.Api
{
    using Data;
    using Packets.Communicator;

    /// <summary>
    /// The game server's side of asking the Auth server for an account: the request goes out
    /// on the link with a number, and the thread that asked waits for the answer carrying that
    /// number - which arrives on the link's own thread (<see cref="Answer"/>) - or for
    /// <see cref="TimeoutMs"/>, whichever is first. An Auth server from before this message
    /// ignores it, and the wait runs out.
    /// </summary>
    public sealed class AccountRelay
    {
        /// <summary>How long an answer is waited for. A request to the REST API has five seconds in all.</summary>
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>Whether the link to the Auth server is up and logged in.</summary>
        public Func<bool> Connected { get; set; } = () => false;

        /// <summary>Puts a request on the link.</summary>
        public Action<CreateAccountRequestPacket> Send { get; set; }

        private readonly ConcurrentDictionary<uint, TaskCompletionSource<CreateAccountResponsePacket>> _waiting =
            new ConcurrentDictionary<uint, TaskCompletionSource<CreateAccountResponsePacket>>();

        private int _lastId;

        /// <summary>How many requests are waiting for their answer.</summary>
        public int Waiting => _waiting.Count;

        /// <summary>Asks, and waits for the answer. Called on a REST API thread, never on the world loop.</summary>
        public AccountAnswer Create(string email, string userName, string password)
        {
            var send = Send;

            if (send == null || Connected?.Invoke() != true)
                return AccountAnswer.NotConnected;

            var id = (uint)Interlocked.Increment(ref _lastId);
            var waiting = new TaskCompletionSource<CreateAccountResponsePacket>(TaskCreationOptions.RunContinuationsAsynchronously);

            _waiting[id] = waiting;

            try
            {
                send(new CreateAccountRequestPacket { RequestId = id, Email = email, Username = userName, Password = password });

                if (!waiting.Task.Wait(Math.Max(1, TimeoutMs)))
                    return AccountAnswer.TimedOut;

                var answer = waiting.Task.Result;

                return AccountAnswer.Of(answer.Result, answer.AccountId);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Could not ask the Auth server for account {userName}: {e.GetBaseException().Message}");
                return AccountAnswer.NotConnected;
            }
            finally
            {
                _waiting.TryRemove(id, out _);
            }
        }

        /// <summary>The Auth server's answer, from the link. One nobody is waiting for any more is dropped.</summary>
        public void Answer(CreateAccountResponsePacket packet)
        {
            if (packet != null && _waiting.TryGetValue(packet.RequestId, out var waiting))
                waiting.TrySetResult(packet);
        }
    }
}
