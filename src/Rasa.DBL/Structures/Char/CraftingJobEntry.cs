using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// One item a crafting station is making, or holding, for a character (crafting_job): what
    /// comes out, how many, when the station is done with it, and which page of the window asked.
    /// The row's id is the job's id to the client (CraftingStatus, RequestRetrieveFinishedCraftItem).
    /// The ingredients and credits were taken when the row was made; the item is made when the
    /// row goes.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(CharacterId), Name = "crafting_job_index_character_id")]
    [Index(nameof(KraftwerksId), Name = "crafting_job_index_kraftwerks_id")]
    public class CraftingJobEntry
    {
        public const string TableName = "crafting_job";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>The station's kraftwerks row, which outlives its entity id and survives a GM moving it.</summary>
        [Column("kraftwerks_id")]
        [Required]
        public uint KraftwerksId { get; set; }

        [Column("result_class_id")]
        [Required]
        public uint ResultClassId { get; set; }

        [Column("result_template_id")]
        [Required]
        public uint ResultTemplateId { get; set; }

        /// <summary>How many are still to be taken.</summary>
        [Column("count")]
        [Required]
        public uint Count { get; set; }

        [Column("crafting_page")]
        [Required]
        public uint CraftingPage { get; set; }

        [Column("quality_id")]
        [Required]
        public uint QualityId { get; set; }

        /// <summary>When the station is done with it, UTC.</summary>
        [Column("finish_at")]
        [Required]
        public DateTime FinishAt { get; set; }
    }
}
