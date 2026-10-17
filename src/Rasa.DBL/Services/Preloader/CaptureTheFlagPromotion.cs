using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Capture the Flag (mission 1994, Bootcamp): the promotion Corporal DeSimone gives as the
    /// mission starts (objective 4, "Speak to Corporal DeSimone"; reward 59) pays 500 experience.
    /// It paid 43000, the experience of level 5, which took a recruit from level 1 to 5 at once.
    /// The completion reward at Captain Youngblood (reward 1: 5000 experience, 200 credits) is
    /// unchanged.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike, guarded on the row as it was: a reward a GM has
    /// set since is left alone, and Down puts back only what is still as Up left it.
    /// </summary>
    public static class CaptureTheFlagPromotion
    {
        public const uint MissionId = 1994;
        public const uint RewardId = 59;
        public const string Revision = "deployment_11";
        public const uint ExperienceWas = 43000;
        public const uint Experience = 500;

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
