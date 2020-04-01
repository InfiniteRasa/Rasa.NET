using System.Collections.Generic;

namespace Rasa.Managers
{
    public class VerifyDatabaseManager
    {
        private static readonly List<string> Character = new List<string> {
            "rasachar_2018_11_20_16_35",
            "rasachar_2018_11_26_16_58_added_character_options",
            "rasachar_2018_12_11_20_08_work_on_position",
            "rasachar_2018_12_17_18_40_added_credits",
            "rasachar_2018_12_31_15_40_added_active_weapon",
            "rasachar_2019_01_06_21_04_added_character_teleporters",
            "rasachar_2019_01_23_13_44_orientation_fix",
            "rasachar_2019_12_20_14_05_changed_all_credits_to_int",
            "rasachar_2020_04_01_21_00_add_clan_data"
        };
        private static readonly List<string> World = new List<string> {
            "rasaworld_2018_11_20_16_35",
            "rasaworld_2018_11_29_15_26_added_itemtemplate_resistance",
            "rasaworld_2018_12_03_17_55_updated_look_for test_npcs",
            "rasaworld_2018_12_11_20_12_work_on_position",
            "rasaworld_2019_01_06_00_27_added_support_for_dynamic_objects",
            "rasaworld_2019_01_09_13_00_new_teleporter_structure",
            "rasaworld_2019_01_23_13_31_creature_apearence_color_fix",
            "rasaworld_2019_01_23_13_43_orientation_fix",
            "rasaworld_2019_01_23_17_00_patched_daghdas_urn_waypoint",
            "rasaworld_2019_01_25_17_00_added_missing_creature_stats_and_actions",
            "rasaworld_2019_02_05_14_07_added_creture_actions",
            "rasaworld_2019_02_08_11_05_patched_creature_run_walk_speed",
            "rasaworld_2019_02_12_11_43_clan_master_added",
            "rasaworld_2019_08_23_15_44_added_all_waypoints_hospitals",
            "rasaworld_2019_12_12_23_12_added_footlockers_in_wilderness"
        };

        private static VerifyDatabaseManager _instance;
        private static readonly object InstanceLock = new object();
        public static VerifyDatabaseManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new VerifyDatabaseManager();
                    }
                }

                return _instance;
            }
        }

        private VerifyDatabaseManager()
        {
        }

        internal void VerifyDatabase()
        {
            Logger.WriteLog(LogType.Initialize, "");
            Logger.WriteLog(LogType.Initialize, "Verifying Database:");
            VerifyCharacter();
            VerifyWorld();
        }

        internal void VerifyCharacter()
        {
            var appliedPatches = Database.Tables.Character.AppliedPatchesTable.GetAppliedPatches();

            if (VerifyMore(Character, appliedPatches, "rasachar"))
                if (VerifyLess(Character, appliedPatches, "rasachar"))
                    if (VerifyEqual(Character, appliedPatches, "rasachar"))
                        Logger.WriteLog(LogType.Initialize, $"<rasachar> database OK");
        }

        internal void VerifyWorld()
        {
            var appliedPatches = Database.Tables.World.AppliedPatchesTable.GetAppliedPatches();

            if (VerifyMore(World, appliedPatches, "rasaworld"))
                if (VerifyLess(World, appliedPatches, "rasaworld"))
                    if (VerifyEqual(World, appliedPatches, "rasaworld"))
                        Logger.WriteLog(LogType.Initialize, $"<rasaworld> database OK");
        }

        internal bool VerifyMore(List<string> server, List<string> database, string source)
        {
            var result = true;
            if (server.Count > database.Count)
            {
                foreach (var patch in server)
                {
                    var count = 0;

                    for (var i = 0; i < database.Count; i++)
                    {
                        count++;
                        if (database[i] == patch)
                            break;

                        if (count == database.Count)
                        {
                            Logger.WriteLog(LogType.Error, $"<{source}> database you miss patch <{patch}>\nUpdate <{source}> database.");
                            result = false;
                        }
                    }
                }
            }
            return result;
        }

        internal bool VerifyLess(List<string> server, List<string> database, string source)
        {
            var result = true;
            if (server.Count < database.Count)
            {
                foreach (var appliedpatch in database)
                {
                    var count = 0;

                    for (var i = 0; i < server.Count; i++)
                    {
                        count++;
                        if (server[i] == appliedpatch)
                            break;

                        if (count == server.Count)
                        {
                            Logger.WriteLog(LogType.Error, $"patch <{appliedpatch}> in <{source}> database is missiing from server.\nAre you running outdated server?");
                            result = false;
                        }
                    }
                }
            }
            return result;
        }

        internal bool VerifyEqual(List<string> server, List<string> database, string source)
        {
            var result = true;

            for (var i = 0; i < server.Count; i++)
                if (server[i] != database[i])
                {
                    Logger.WriteLog(LogType.Error, $"Patch < {database[i]} > in <{source}> database is installed in wrong order");
                    result = false;
                }

            return result;
        }
    }
}
