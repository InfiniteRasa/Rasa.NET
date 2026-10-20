using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Capture the Flag (mission 1994, Bootcamp): the promotion Corporal DeSimone gives as the
    /// mission starts (objective 4, reward 59) pays 1750 experience, so it takes the recruit to
    /// level 2.
    ///
    /// The recruit comes to DeSimone with what Initiation (1990, 100) and Gearing Up (1992, 1250)
    /// paid: 1350. Level 2 is 3000 (ExpPerLevel). The 500 CaptureTheFlagPromotion set left them
    /// at 1850, still level 1 after being promoted; 1750 is 3100.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike, guarded on the row as it was: a reward a GM has
    /// set since is left alone, and Down puts back only what is still as Up left it.
    /// </summary>
    public static class CaptureTheFlagPromotionLevel
    {
        public const uint MissionId = CaptureTheFlagPromotion.MissionId;
        public const uint RewardId = CaptureTheFlagPromotion.RewardId;
        public const string Revision = CaptureTheFlagPromotion.Revision;
        public const uint ExperienceWas = CaptureTheFlagPromotion.Experience;
        public const uint Experience = 1750;

        public static IEnumerable<string> Up()
        {
            yield return Set(ExperienceWas, Experience);
        }

        public static IEnumerable<string> Down()
        {
            yield return Set(Experience, ExperienceWas);
        }

        private static string Set(uint from, uint to) =>
            $"UPDATE mission_reward_definition SET experience = {to} " +
            $"WHERE mission_id = {MissionId} AND content_revision = '{Revision}' AND reward_id = {RewardId} AND experience = {from};";
    }
}
