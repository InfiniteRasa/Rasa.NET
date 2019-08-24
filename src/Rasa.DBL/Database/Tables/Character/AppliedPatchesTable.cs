using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public static class AppliedPatchesTable
    {
        private static readonly MySqlCommand GetAppliedPatchesCommand = new MySqlCommand("SELECT `patch_name` FROM `applied_patches`");

        public static void Initialize()
        {

            GetAppliedPatchesCommand.Connection = GameDatabaseAccess.CharConnection;
            GetAppliedPatchesCommand.Prepare();
        }

        public static List<string> GetAppliedPatches()
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var appliedPatches = new List<string>();

                using (var reader = GetAppliedPatchesCommand.ExecuteReader())
                    while (reader.Read())
                        appliedPatches.Add(reader.GetString("patch_name"));

                return appliedPatches;
            }
        }
    }
}
