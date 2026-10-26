using System.Collections.Generic;
using System.Numerics;
namespace Rasa.Services.Preloader
{
    public static class LandingZoneReinforcementContent
    {
        public const uint Pistol = 680011, Shotgun = 680012, MachineGun = 680013;
        // Two grounded posts in front of the Landing Zone field, reached from the shared gunship pad.
        public static readonly Vector3[] Posts = { new(176,164,-18), new(222,164,-18) };
        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature_action (id,description,action_id,action_arg_id,range_min,range_max,cooldown,windup,min_damage,max_damage,damage_type) VALUES " +
                "(680011,'Landing Zone reinforcement pistol',1,133,0.5,20,1000,0,12,20,1)," +
                "(680012,'Landing Zone reinforcement shotgun',1,121,0.5,12,1500,0,18,30,1)," +
                "(680013,'Landing Zone reinforcement machine gun',149,1,1,30,500,0,10,16,1);";
            yield return "INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) VALUES " +
                "(680011,'Landing Zone reinforcement Infantryman',29423,1,11,650,8716,7,2,680011,0,0,0,0,0,0,0)," +
                "(680012,'Landing Zone reinforcement Shotgunner',29423,1,11,650,8718,7,2,680012,0,0,0,0,0,0,0)," +
                "(680013,'Landing Zone reinforcement Machine Gunner',29433,1,11,650,8719,7,2,680013,0,0,0,0,0,0,0);";
            yield return "INSERT INTO creature_stat (id,body,mind,spirit,health,armor) VALUES (680011,15,15,15,650,120),(680012,15,15,15,650,120),(680013,15,15,15,650,120);";
            yield return "INSERT INTO creature_appearance (id,slot_id,class_id,color) VALUES (680011,13,6271,1),(680012,13,20560,1),(680013,13,20535,1);";
        }
        public static IEnumerable<string> Down()
        {
            yield return "DELETE FROM creature_appearance WHERE id BETWEEN 680011 AND 680013;";
            yield return "DELETE FROM creature_stat WHERE id BETWEEN 680011 AND 680013;";
            yield return "DELETE FROM creature WHERE id BETWEEN 680011 AND 680013;";
            yield return "DELETE FROM creature_action WHERE id BETWEEN 680011 AND 680013;";
        }
    }
}
