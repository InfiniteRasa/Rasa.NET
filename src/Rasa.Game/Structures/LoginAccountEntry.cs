using System;

namespace Rasa.Structures;

using Rasa.Communicator;

public class LoginAccountEntry
{
    public uint Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public DateTime ExpireTime { get; set; }
    public uint OneTimeKey { get; set; }

    public LoginAccountEntry(RedirectRequest request)
    {
        Id = request.AccountId;
        Email = request.Email;
        Name = request.Username;
        OneTimeKey = request.OneTimeKey;
        ExpireTime = DateTime.Now.AddMinutes(1);
    }
}
