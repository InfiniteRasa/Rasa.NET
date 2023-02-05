using System;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Game;

using Rasa.Cryptography;
using Rasa.Networking;

public class ClientFactory : IClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ClientFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Client Create(AsyncLengthedSocket socket, ClientCryptData data, Server server)
    {
        var client = _serviceProvider.GetService<Client>();
        client.RegisterAtServer(server, socket, data);
        return client;
    }
}