using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    [Keyless]
    public class NpcMissionEntry :IHasId
    {
        public const string TableName = "npc_mission";

        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("command")]
        [Required]
        public byte Command { get; set; }

        [Column("var1")]
        [Required]
        public uint Var1 { get; set; }

        [Column("var2")]
        [Required]
        public uint Var2 { get; set; }

        [Column("var3")]
        [Required]
        public uint Var3 { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; }
    }
}
