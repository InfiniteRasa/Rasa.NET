using Rasa.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Managers
{
    using Packets.Game.Server;

    public class ManifestationManager : IManifestationManager
    {
        public void RequestToggleRun(Client client)
        {
            client.Player.IsRunning = !client.Player.IsRunning;

            client.CallMethod(client.Player.EntityId, new IsRunningPacket(client.Player.IsRunning));
        }
    }
}
