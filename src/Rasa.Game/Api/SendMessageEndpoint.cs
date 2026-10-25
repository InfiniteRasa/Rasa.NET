using System;
using System.Text.Json;

namespace Rasa.Api
{
    using Managers;

    /// <summary>Who /sendmessage is for, what it says, and who sent it.</summary>
    public sealed class MessageOrder
    {
        public uint? AccountId { get; set; }
        public string FamilyName { get; set; }
        public string Text { get; set; }
        /// <summary>The client's modal dialog in place of the AFS Message (GmMessages.Dialog).</summary>
        public bool Important { get; set; }
        /// <summary>The dialog's title for an important one; null for GmMessages.DefaultTitle.</summary>
        public string Title { get; set; }
        public string Admin { get; set; }
        public string From { get; set; }
    }

    /// <summary>What came of a message: sent to an account, or why not.</summary>
    public sealed class MessageOutcome
    {
        public bool Sent { get; private set; }
        public string Error { get; private set; }
        public uint AccountId { get; private set; }
        public string FamilyName { get; private set; }

        public static MessageOutcome Done(uint accountId, string familyName) =>
            new MessageOutcome { Sent = true, AccountId = accountId, FamilyName = familyName ?? "" };

        public static MessageOutcome Failed(string error) => new MessageOutcome { Error = error };
    }

    /// <summary>
    /// POST /sendmessage: a game master's message to a player who is online, as .message sends
    /// it (GmMessages.FromGm) - the yellow ADMIN MESSAGE line, and the AFS Message notice they
    /// open to read it all. The body is JSON:
    ///
    ///   {"accountId":12,"text":"Your character was moved to the hospital.","admin":"Atomsk"} or
    ///   {"familyName":"Hart","text":"...","admin":"Atomsk","important":true,"title":"Server restart"}
    ///
    /// accountId or familyName as for /kickuser (KickUserEndpoint.Read), text up to
    /// GmMessages.MaxLength characters, admin, the one sending it, and important, true to open the
    /// client's modal dialog in place of the AFS Message (GmMessages.Dialog), and title, the dialog's
    /// (GmMessages.MaxTitleLength). Every answer of its own is
    /// 200: {"sent":true,"accountId":12,"familyName":"Hart"}, or {"sent":false,"error":"not
    /// online"}. Nothing is kept for a player who is away. Sent on the world loop, which the
    /// clients belong to; it changes what a player sees, so it is ApiEndpoint.Sensitive.
    /// </summary>
    public sealed class SendMessageEndpoint : ApiEndpoint
    {
        public const int WaitMs = 10000;

        /// <summary>Sends on the world loop; null when the loop did not get to it in time. Unset until the server has started.</summary>
        public Func<MessageOrder, MessageOutcome> Send { get; set; }

        public override string Name => "sendmessage";
        public override string Method => "POST";
        public override bool Sensitive => true;

        public override ApiResponse Handle(ApiRequest request)
        {
            var target = KickUserEndpoint.Read(request, out var problem);
            string text = null;
            var important = false;
            string title = null;

            if (target != null && !TryText(request.Body, out text, out important, out title, out problem))
                target = null;

            if (target == null)
                return Answer(MessageOutcome.Failed(problem));

            var send = Send;
            if (send == null)
                return Answer(MessageOutcome.Failed("the game server is not ready"));

            var order = new MessageOrder
            {
                AccountId = target.AccountId,
                FamilyName = target.FamilyName,
                Text = text,
                Important = important,
                Title = title,
                Admin = target.Admin,
                From = ApiAudit.Address(request.Remote)
            };

            return Answer(send(order) ?? MessageOutcome.Failed("the game server did not answer in time"));
        }

        /// <summary>On the world loop: the message to the account, if it is online.</summary>
        public static MessageOutcome Perform(MessageOrder order)
        {
            var targets = order.AccountId.HasValue ? Moderation.OnlineAccount(order.AccountId.Value) : Moderation.Online(order.FamilyName);
            var client = targets.Find(c => c.State == Data.ClientState.Ingame && c.Player != null);

            if (client == null)
                return MessageOutcome.Failed("not online");

            GmMessages.FromGm(client, order.Text, order.Important, order.Title);
            Logger.WriteLog(LogType.Command, $"Message to {client.AccountEntry.FamilyName} from {order.Admin} through the REST API from {order.From}: {order.Text}");
            return MessageOutcome.Done(client.AccountEntry.Id, client.AccountEntry.FamilyName);
        }

        private static bool TryText(string body, out string text, out bool important, out string title, out string problem)
        {
            text = null;
            important = false;
            title = null;
            problem = null;

            using var document = JsonDocument.Parse(body ?? "{}");

            if (document.RootElement.TryGetProperty("important", out var flag) && flag.ValueKind != JsonValueKind.Null)
            {
                if (flag.ValueKind != JsonValueKind.True && flag.ValueKind != JsonValueKind.False)
                {
                    problem = "important must be true or false";
                    return false;
                }

                important = flag.ValueKind == JsonValueKind.True;
            }

            if (document.RootElement.TryGetProperty("title", out var named) && named.ValueKind != JsonValueKind.Null)
            {
                if (named.ValueKind != JsonValueKind.String || named.GetString().Trim().Length > GmMessages.MaxTitleLength)
                {
                    problem = $"title must be text of at most {GmMessages.MaxTitleLength} characters";
                    return false;
                }

                title = named.GetString().Trim();
            }
            if (!document.RootElement.TryGetProperty("text", out var value) || value.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(value.GetString()))
            {
                problem = "text is required";
                return false;
            }

            text = value.GetString().Trim();
            if (text.Length > GmMessages.MaxLength)
            {
                problem = $"text is longer than {GmMessages.MaxLength} characters";
                return false;
            }

            return true;
        }

        private static ApiResponse Answer(MessageOutcome outcome) =>
            ApiResponse.Ok(ServerStatus.Json(writer =>
            {
                writer.WriteBoolean("sent", outcome.Sent);
                if (outcome.Sent)
                {
                    writer.WriteNumber("accountId", outcome.AccountId);
                    writer.WriteString("familyName", outcome.FamilyName ?? "");
                }
                else
                {
                    writer.WriteString("error", outcome.Error ?? "not sent");
                }
            }));
    }
}
