using System;
using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CensorWordsTable
    {
        private static readonly MySqlCommand GetCensoredWordsCommand = new MySqlCommand("SELECT * FROM `censor_words`");

        public static void Initialize()
        {
            GetCensoredWordsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCensoredWordsCommand.Prepare();
        }

        public static List<string> GetCensoredWords()
        {
            var words = new List<string>();

            lock (GameDatabaseAccess.CharLock)
            {
                using (var reader = GetCensoredWordsCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        words.Add(CensoredWordEntry.Read(reader, false).Word);
                    }
                }
            }

            return words;
        }
    }
}
