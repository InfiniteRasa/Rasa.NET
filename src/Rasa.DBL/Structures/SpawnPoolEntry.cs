using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class SpawnPoolEntry
    {
        public int DbId { get; set; }
        public short Mode { get; set; }
        public short AnimType { get; set; }
        public int RespawnTime { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float Orientation { get; set; }
        public uint MapContextId { get; set; }
        public uint CreatureId1 { get; set; }
        public short CreatureMinCount1 { get; set; }
        public short CreatureMaxCount1 { get; set; }
        public uint CreatureId2 { get; set; }
        public short CreatureMinCount2 { get; set; }
        public short CreatureMaxCount2 { get; set; }
        public uint CreatureId3 { get; set; }
        public short CreatureMinCount3 { get; set; }
        public short CreatureMaxCount3 { get; set; }
        public uint CreatureId4 { get; set; }
        public short CreatureMinCount4 { get; set; }
        public short CreatureMaxCount4 { get; set; }
        public uint CreatureId5 { get; set; }
        public short CreatureMinCount5 { get; set; }
        public short CreatureMaxCount5 { get; set; }
        public uint CreatureId6 { get; set; }
        public short CreatureMinCount6 { get; set; }
        public short CreatureMaxCount6 { get; set; }
    }
}
