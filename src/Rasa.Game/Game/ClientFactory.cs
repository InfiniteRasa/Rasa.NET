using System;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Game
{
    using Cryptography;
    using Networking;

    public class ClientFactory : IClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Client Create(LengthedSocket socket, ClientCryptData data, Server server)
        {
            var client = _serviceProvider.GetService<Client>();
            client.RegisterAtServer(server, socket, data);
            return client;
        }
    }
}