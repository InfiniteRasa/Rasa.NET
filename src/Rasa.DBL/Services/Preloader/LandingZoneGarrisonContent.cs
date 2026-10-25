using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace Rasa.Services.Preloader
{
    /// <summary>Three AFS reinforcement groups linked to Landing Zone's existing garrison.</summary>
    public static class LandingZoneGarrisonContent
    {
        public const uint Map = 1220, Point = 4;
        public const uint Mech = 680001, Rifleman = 680002, RifleShot = 680001;
        public const uint MechPool = 680001, InfantryA = 680002, InfantryB = 680003;
        public static readonly Vector3 Teleporter = new(156.1914f, 163.5078f, -112.5625f);
        public static readonly Vector3 Gunship = new(206.9219f, 171.0039f, -94.5f);
        public const double TeleportRotation = 3.5766, GunshipRotation = 1.6901;
        public static readonly Vector3[] MechPatrol = { new(233.2812f, 164.1914f, -17.9141f), new(168.8867f, 164.5898f, -14.1484f) };
        // Elevated surfaces sampled from the Wilderness navmesh: side walls and the front crosswalk.
        public static readonly Vector3[][] Catwalks = {
            new[] { new Vector3(172,171.1178f,-114), new Vector3(172,171.2237f,-78), new Vector3(168,171.1178f,-72), new Vector3(168,171.1178f,-40) },
            new[] { new Vector3(224,171.1178f,-114), new Vector3(224,171.1178f,-76), new Vector3(232,171.1178f,-72), new Vector3(232,171.1178f,-52), new Vector3(228,171.1178f,-40) },
            new[] { new Vector3(172,171.1178f,-38), new Vector3(226,171.1178f,-38) }
        };
        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature_action (id,description,action_id,action_arg_id,range_min,range_max,cooldown,windup,min_damage,max_damage,damage_type) " +
                "SELECT 680001,'Landing Zone AFS rifle',action_id,action_arg_id,range_min,35,cooldown,windup,18,28,damage_type FROM creature_action WHERE id=72003;";
            yield return "INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) VALUES " +
                "(680001,'Landing Zone AFS Mech Defender',10235,1,11,5000,10736,9,5,13,0,0,0,0,0,0,0)," +
                "(680002,'Landing Zone AFS Rifleman',29423,1,11,650,8717,7,2,680001,0,0,0,0,0,0,0);";
            yield return "INSERT INTO creature_stat (id,body,mind,spirit,health,armor) VALUES (680001,30,30,30,5000,500),(680002,15,15,15,650,120);";
            yield return "INSERT INTO creature_appearance (id,slot_id,Class_id,color) VALUES (680002,13,27220,1);";
            yield return Pool(MechPool, Mech, 2, 0, 6000, Teleporter, TeleportRotation);
            yield return Pool(InfantryA, Rifleman, 3, 2, 3000, Gunship, GunshipRotation);
            yield return Pool(InfantryB, Rifleman, 3, 2, 3000, Gunship, GunshipRotation);
            yield return "INSERT INTO control_point_link (control_point_id,kind,object_id) VALUES (4,2,680001),(4,2,680002),(4,2,680003);";
        }
        private static string Pool(uint id, uint creature, int count, int animation, int respawn, Vector3 at, double rotation) =>
            "INSERT INTO spawnpool (id,mode,anim_type,respown_time,pos_x,pos_y,pos_z,rotation,map_context_id," +
            "creature_1_Id,creature_1_min_count,creature_1_max_count,creature_2_Id,creature_2_min_count,creature_2_max_count," +
            "creature_3_Id,creature_3_min_count,creature_3_max_count,creature_4_Id,creature_4_min_count,creature_4_max_count," +
            "creature_5_Id,creature_5_min_count,creature_5_max_count,creature_6_Id,creature_6_min_count,creature_6_max_count,radius) VALUES " +
            $"({id},1,{animation},{respawn},{N(at.X)},{N(at.Y)},{N(at.Z)},{N(rotation)},1220,{creature},{count},{count},0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1.5);";
        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        public static IEnumerable<string> Down()
        {
            yield return "DELETE FROM control_point_link WHERE control_point_id=4 AND kind=2 AND object_id BETWEEN 680001 AND 680003;";
            yield return "DELETE FROM spawnpool WHERE id BETWEEN 680001 AND 680003;";
            yield return "DELETE FROM creature_appearance WHERE id=680002;";
            yield return "DELETE FROM creature_stat WHERE id BETWEEN 680001 AND 680002;";
            yield return "DELETE FROM creature WHERE id BETWEEN 680001 AND 680002;";
            yield return "DELETE FROM creature_action WHERE id=680001;";
        }
    }
}
