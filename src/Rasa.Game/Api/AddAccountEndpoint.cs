using System;
using System.Text.Json;

namespace Rasa.Api
{
    using Data;

    /// <summary>What the Auth server said to a request for an account, or that it said nothing.</summary>
    public readonly struct AccountAnswer
    {
        /// <summary>The link to the Auth server was up and the request went out.</summary>
        public bool Asked { get; }

        /// <summary>The Auth server answered in time.</summary>
        public bool Answered { get; }

        public CreateAccountResult Result { get; }
        public uint AccountId { get; }

        private AccountAnswer(bool asked, bool answered, CreateAccountResult result, uint accountId)
        {
            Asked = asked;
            Answered = answered;
            Result = result;
            AccountId = accountId;
        }

        public static AccountAnswer NotConnected => new AccountAnswer(false, false, CreateAccountResult.Failed, 0);
        public static AccountAnswer TimedOut => new AccountAnswer(true, false, CreateAccountResult.Failed, 0);
        public static AccountAnswer Of(CreateAccountResult result, uint accountId) => new AccountAnswer(true, true, result, accountId);
    }

    /// <summary>
    /// POST /addaccount: makes a login for the server. The body is JSON,
    /// {"email":"...","username":"...","password":"..."}, sent as application/json.
    ///
    /// The accounts are the Auth server's, so the request is handed on over the game server's
    /// link to it (<see cref="Create"/>, AccountRelay) and its answer is the answer here:
    ///   201 {"result":"created","username":"...","account_id":12}
    ///   400 {"error":"..."}            the body is not that JSON, or the details will not do (AccountRules)
    ///   409 {"error":"username taken"} or {"error":"email taken"}
    ///   415                            the body was not sent as application/json
    ///   500                            the Auth server could not store it; its log says why
    ///   503                            the Auth server is not connected
    ///   504                            the Auth server did not answer in time (the account may exist)
    ///
    /// It is <see cref="ApiEndpoint.Sensitive"/>: off until ApiConfig.Rest.Endpoints.addaccount
    /// turns it on, and never public because the API is. The password is never logged.
    /// </summary>
    public sealed class AddAccountEndpoint : ApiEndpoint
    {
        public override string Name => "addaccount";
        public override string Method => "POST";
        public override bool Sensitive => true;

        /// <summary>Asks the Auth server for the account: e-mail, user name, password. Set by Server; null answers 503.</summary>
        public Func<string, string, string, AccountAnswer> Create { get; set; }

        public override ApiResponse Handle(ApiRequest request)
        {
            // Not a form and not text: a page on some other site cannot send this without the
            // browser asking first, and the API does not say yes.
            if (!request.Headers.TryGetValue("Content-Type", out var type)
                || !(type ?? "").TrimStart().StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                return ApiResponse.Error(415, "content type must be application/json");

            string email, userName, password;

            try
            {
                using var document = JsonDocument.Parse(request.Body ?? "");

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return ApiResponse.Error(400, "the body must be a JSON object with email, username and password");

                if (!Text(document.RootElement, "email", out email, out var problem)
                    || !Text(document.RootElement, "username", out userName, out problem)
                    || !Text(document.RootElement, "password", out password, out problem))
                    return ApiResponse.Error(400, problem);
            }
            catch (JsonException)
            {
                return ApiResponse.Error(400, "the body is not JSON");
            }

            email = email?.Trim();
            userName = userName?.Trim();

            var wrong = AccountRules.Problem(email, userName, password);

            if (wrong != null)
                return ApiResponse.Error(400, wrong);

            var create = Create;

            if (create == null)
                return ApiResponse.Error(503, "the auth server is not connected");

            var answer = create(email, userName, password);

            if (!answer.Asked)
                return ApiResponse.Error(503, "the auth server is not connected");

            if (!answer.Answered)
                return ApiResponse.Error(504, "the auth server did not answer in time; the account may or may not have been created");

            switch (answer.Result)
            {
                case CreateAccountResult.Created:
                    Logger.WriteLog(LogType.Command, $"REST API: account {userName} ({answer.AccountId}) created for {request.Remote}.");

                    return new ApiResponse(201, ServerStatus.Json(writer =>
                    {
                        writer.WriteString("result", "created");
                        writer.WriteString("username", userName);
                        writer.WriteNumber("account_id", answer.AccountId);
                    }));

                case CreateAccountResult.UsernameTaken:
                    return ApiResponse.Error(409, "username taken");

                case CreateAccountResult.EmailTaken:
                    return ApiResponse.Error(409, "email taken");

                case CreateAccountResult.Invalid:
                    return ApiResponse.Error(400, "the auth server refused the details");

                default:
                    return ApiResponse.Error(500, "the account could not be created");
            }
        }

        /// <summary>A property of the body by its name in any case, which has to be text. Left out, it is null and AccountRules says so.</summary>
        private static bool Text(JsonElement body, string name, out string value, out string problem)
        {
            value = null;
            problem = null;

            foreach (var property in body.EnumerateObject())
            {
                if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    problem = $"{name} must be text";
                    return false;
                }

                value = property.Value.GetString();
            }

            return true;
        }
    }
}
