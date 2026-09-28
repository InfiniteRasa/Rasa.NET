using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    public enum MissionScenarioStartPolicy : byte
    {
        Automatic = 1,
        PlayerTriggered = 2
    }

    [Table(TableName)]
    public class MissionScenarioEntry
    {
        public const string TableName = "mission_scenario";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("scenario_id")]
        [Required]
        public uint ScenarioId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("start_policy")]
        [Required]
        public MissionScenarioStartPolicy StartPolicy { get; set; } =
            MissionScenarioStartPolicy.Automatic;

        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; } = string.Empty;

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionContentDefinitionEntry Content { get; set; }
        public ICollection<MissionScenarioStepEntry> Steps { get; set; } =
            new List<MissionScenarioStepEntry>();
    }
}
