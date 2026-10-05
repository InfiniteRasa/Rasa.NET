using System.Text;

namespace Rasa.Data
{
    /// <summary>
    /// What an account's details have to be for the account to be usable, checked where an
    /// account is asked for (the game server's REST API) and again where it is made (Auth).
    ///
    /// The lengths are the client's: its login sends the user name in 14 bytes and the password
    /// in 16 (Auth's LoginPacket), so a longer one of either is an account nobody can log in
    /// to. The e-mail is the column's 255.
    /// </summary>
    public static class AccountRules
    {
        public const int MaxUserNameBytes = 14;
        public const int MaxPasswordBytes = 16;
        public const int MaxEmailLength = 255;

        /// <summary>Null when the details will do; otherwise what is wrong with them, for whoever asked.</summary>
        public static string Problem(string email, string userName, string password)
        {
            if (string.IsNullOrEmpty(userName))
                return "username is required";

            if (Encoding.UTF8.GetByteCount(userName) > MaxUserNameBytes)
                return $"username is longer than {MaxUserNameBytes} characters, the most the game client can send";

            foreach (var character in userName)
                if (char.IsWhiteSpace(character) || char.IsControl(character))
                    return "username has a space or a control character in it";

            if (string.IsNullOrEmpty(password))
                return "password is required";

            if (Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes)
                return $"password is longer than {MaxPasswordBytes} characters, the most the game client can send";

            foreach (var character in password)
                if (char.IsControl(character))
                    return "password has a control character in it";

            if (string.IsNullOrEmpty(email))
                return "email is required";

            if (email.Length > MaxEmailLength)
                return $"email is longer than {MaxEmailLength} characters";

            var at = email.IndexOf('@');

            if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1)
                return "email is not an address";

            foreach (var character in email)
                if (char.IsWhiteSpace(character) || char.IsControl(character))
                    return "email is not an address";

            return null;
        }
    }
}
