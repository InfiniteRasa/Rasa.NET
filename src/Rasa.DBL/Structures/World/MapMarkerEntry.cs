using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.World
{
    /// <summary>What kind of server object a marker's state is read from.</summary>
    public enum MapMarkerSource : byte
    {
        /// <summary>A row in the teleporter table: a waypoint, a hospital or a safe zone.</summary>
        Teleporter = 1,

        /// <summary>A row in the kraftwerks table: a crafting station.</summary>
        Kraftwerks = 2
    }

    /// <summary>
    /// Ties one of the client's map-screen status markers to the server object whose state it
    /// shows.
    ///
    /// The map screen draws these from its own <c>generated.client.uimapmarker.factionedmarkers</c>,
    /// and every row there begins with an entity id - the *original* server's id for that object,
    /// compiled into the client in 2009. <c>Recv_MapMarkerInfo</c> is a dictionary keyed by those
    /// ids, so state sent under an id this server happened to mint at runtime reaches nothing: the
    /// marker looks the state up by the id it was drawn with.
    ///
    /// Nothing carries that mapping across, so it was recovered the way the NPC placements and the
    /// map links were - nearest object of the matching kind on the same map - and the distances
    /// are what say whether that is a match or a guess. Medians of 0.4 m for hospitals and 1.5 m
    /// for waypoints are the same object surveyed twice; the rejects start at 26 m, so the
    /// threshold sits in open water rather than through a cluster.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(MapContextId), Name = "map_marker_index_map_context_id")]
    public class MapMarkerEntry
    {
        public const string TableName = "map_marker";

        public MapMarkerEntry()
        {
        }

        /// <summary>
        /// The client's own entity id for this marker, and the key the state is sent under. Not
        /// an entity this server knows: nothing is ever spawned with it.
        ///
        /// Not unique on its own, which is why the key is this and the map together. Seven
        /// markers appear in two map templates each - a zone and its wargame variant, which the
        /// client draws from one marker id but the server loads as two different objects. Within
        /// one map they are unique, and one map is all the client holds at a time.
        /// </summary>
        [Column("marker_entity_id")]
        [Required]
        public ulong MarkerEntityId { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        /// <summary>uimapmarker's type: waypoint, hospital, safe zone, crafting station.</summary>
        [Column("marker_type")]
        [Required]
        public uint MarkerType { get; set; }

        [Column("object_kind")]
        [Required]
        public byte ObjectKind { get; set; }

        /// <summary>The id of the teleporter or kraftwerks row this marker stands for.</summary>
        [Column("object_id")]
        [Required]
        public uint ObjectId { get; set; }

        /// <summary>
        /// How far the marker sat from the object it was matched to, in metres. Kept because it
        /// is the evidence for the row: a marker that turns out to be wrong in game is looked up
        /// here, and a large number is where to look first.
        /// </summary>
        [Column("match_distance")]
        [Required]
        public double MatchDistance { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        public string Comment { get; set; }

        public MapMarkerSource Source => (MapMarkerSource)ObjectKind;
    }
}
