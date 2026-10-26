using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Services.Preloader
{
    /// <summary>Landing Zone's hourly assault: dedicated level-11 creatures and six authored dropship approaches.</summary>
    public static class LandingZoneAssaultContent
    {
        public const uint MapId = 1220, PointId = 4;
        public const uint Trainee = 620001, Private = 620002, Sergeant = 620003,
            Hunter = 620004, Linker = 620005, Boss = 620006;
        public readonly struct Drop { public Vector3 Position { get; } public double Rotation { get; } public Drop(Vector3 position, double rotation) { Position = position; Rotation = rotation; } }
        public static readonly Drop[] Drops =
        {
            new(new Vector3(296.1445f, 171.0938f, -5.7227f), 1.1937),
            new(new Vector3(254.8516f, 167.4609f, 5.6797f), 0.6564),
            new(new Vector3(223.3047f, 166.6562f, 8.0703f), 0.6266),
            new(new Vector3(192.7656f, 166.9766f, 7.6367f), 6.2281),
            new(new Vector3(157.7227f, 166.5859f, 9.6758f), 5.4113),
            new(new Vector3(126.0703f, 167.3320f, -1.3164f), 5.1395)
        };

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature_action (id,description,action_id,action_arg_id,range_min,range_max,cooldown,windup,min_damage,max_damage,damage_type) VALUES " +
                "(620001,'LZ trainee rifle',1,79,1,35,1300,0,12,18,6)," +
                "(620002,'LZ private rifle',1,79,1,35,1100,0,18,26,6)," +
                "(620003,'LZ sergeant rifle',1,79,1,35,1000,0,26,38,6)," +
                "(620004,'LZ hunter weapon',1,244,1,30,1500,0,18,28,1)," +
                "(620005,'LZ hunter net',403,1,1,40,10000,900,11,21,13)," +
                "(620006,'LZ linker chest blast',263,1,1,30,2500,1800,16,24,1)," +
                "(620007,'LZ linker channel',410,1,0,40,20000,5666,0,0,1)," +
                "(620008,'LZ boss rifle',1,79,1,40,1000,0,38,55,6)," +
                "(620009,'LZ boss kick',397,3,0.5,4,6000,333,30,45,1);";
            yield return "INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) VALUES " +
                "(620001,'Landing Zone Thrax Infantry Trainee',4047,0,11,300,7675,9,5,620001,0,0,0,0,0,0,0)," +
                "(620002,'Landing Zone Thrax Infantry Private',4047,0,11,450,7676,9,5,620002,0,0,0,0,0,0,0)," +
                "(620003,'Landing Zone Thrax Infantry Sergeant',4047,0,11,800,7680,9,5,620003,0,0,0,0,0,0,0)," +
                "(620004,'Landing Zone Hunter',10166,0,11,450,392,9,5,620004,620005,0,0,0,0,0,0)," +
                "(620005,'Landing Zone Linker',9751,0,11,450,419,9,5,620006,620007,0,0,0,0,0,0)," +
                "(620006,'Landing Zone Thrax Soldier Boss',10504,0,11,2400,9224,9,5,620008,620009,0,0,0,0,0,0);";
            yield return "INSERT INTO creature_stat (id,body,mind,spirit,health,armor) VALUES " +
                "(620001,15,15,15,300,60),(620002,15,15,15,450,80),(620003,15,15,15,800,120)," +
                "(620004,15,15,15,450,80),(620005,15,15,15,450,80),(620006,30,30,30,2400,350);";
        }
        public static IEnumerable<string> Down()
        {
            yield return "DELETE FROM creature_stat WHERE id BETWEEN 620001 AND 620006;";
            yield return "DELETE FROM creature WHERE id BETWEEN 620001 AND 620006;";
            yield return "DELETE FROM creature_action WHERE id BETWEEN 620001 AND 620009;";
        }
    }
}
