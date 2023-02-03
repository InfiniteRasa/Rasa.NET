using System.Net.Sockets;

namespace Rasa.Extensions;

public static class SocketAsyncEventArgsExtensions
{
    public static T? GetUserToken<T>(this SocketAsyncEventArgs args) where T : class
    {
        return args.UserToken as T;
    }
}
