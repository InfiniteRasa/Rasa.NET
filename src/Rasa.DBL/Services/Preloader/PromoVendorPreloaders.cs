using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// John the Swag Vendor and Elm the Emote Vendor in Alia Das, the first town after Bootcamp:
    /// every account reward (AccountReward_*) the client knows, at 1 credit each.
    ///
    /// John sells the promotional items - armour dyes and their recipes, the pre-order and
    /// veteran pets, the Sunset pets, the retailer Soyuz model rockets, the veteran titles,
    /// resupply bot, dropship beacon and XP booster, the holiday snowballs and launcher, and the
    /// space helmet. The three companion tokens are left out: they are mission items
    /// (Flag_mission_items) handed to an NPC for a companion mission, not things to own.
    ///
    /// Elm sells the emotes - the bonus emotes (AccountReward_Emote_*, with the veteran Logos
    /// Greet) and the Sunset emotes (AccountReward_Sunset_Emote_*). Drunk has two identical
    /// templates, 131483 and 131484; the counter carries one.
    ///
    /// Neither has a name in the client's creaturenamelanguage, so both rows have name_id 0 and
    /// are named by creature_actor_name. Both stand on open ground east of the Alia Das vendor
    /// stall, on the navmesh and reachable on foot from the waypoint and the new-character
    /// arrival, facing the waypoint. Vendor_Human_Male for John, Vendor_Human_Female for Elm,
    /// dressed like the AFS vendors beside them; general goods packages no other vendor uses.
    ///
    /// Everything they sell is bound, not tradable and not sellable (Bind_account_reward_items),
    /// so the 1 credit price cannot be sold back for more.
    /// </summary>
    public static class PromoVendors
    {
        public const uint JohnId = 502001;
        public const uint ElmId = 502002;

        public const uint FirstId = JohnId;
        public const uint LastId = ElmId;

        public const int ItemPrice = 1;

        public const uint AliaDasMapContextId = 1220;
    }

    public class PromoVendorCreaturePreloader : PreloaderBase, IPreloader
    {
        private static readonly string[] Columns =
        {
            "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
            "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureEntry.TableName, Columns);
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { PromoVendors.JohnId, "John the Swag Vendor Alia Das", 20975, 1, 6, 555, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { PromoVendors.ElmId, "Elm the Emote Vendor Alia Das", 20972, 1, 6, 555, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        }
    }

    /// <summary>The names the client shows, over their heads and on the vendor window.</summary>
    public class PromoVendorActorNamePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActorNameEntry.TableName, new[] { "id", "actor_name" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { PromoVendors.JohnId, "John the Swag Vendor" };
            yield return new object[] { PromoVendors.ElmId, "Elm the Emote Vendor" };
        }
    }

    /// <summary>The same outfit as the Alia Das supply vendors (creatures 72 and 73).</summary>
    public class PromoVendorAppearancePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureAppearanceEntry.TableName, new[] { "id", "slot_id", "Class_id", "color" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            foreach (var id in new[] { PromoVendors.JohnId, PromoVendors.ElmId })
            {
                yield return new object[] { id, 2, 4021, 4294934528 };
                yield return new object[] { id, 13, 6271, 1 };
                yield return new object[] { id, 14, 9781, 4278655809 };
                yield return new object[] { id, 15, 4023, 4294934528 };
                yield return new object[] { id, 16, 4022, 4294934528 };
                yield return new object[] { id, 17, 24008, 4286690539 };
            }
        }
    }

    /// <summary>The same stats as the Alia Das supply vendors.</summary>
    public class PromoVendorStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, new[] { "id", "body", "mind", "spirit", "health", "armor" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { PromoVendors.JohnId, 15, 15, 15, 120, 70 };
            yield return new object[] { PromoVendors.ElmId, 15, 15, 15, 120, 70 };
        }
    }

    /// <summary>
    /// One pool each, 4 m apart on the plaza east of the vendor stall (which is between them and
    /// the supply vendors), at the navmesh's own ground height. Rotation 5.1 faces the waypoint.
    /// </summary>
    public class PromoVendorSpawnpoolPreloader : PreloaderBase, IPreloader
    {
        private static readonly string[] Columns =
        {
            "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
            "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
            "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
            "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
            "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
            "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
            "creature_6_Id", "creature_6_min_count", "creature_6_max_count"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolEntry.TableName, Columns);
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { PromoVendors.JohnId, 0, 0, 200, 771.0f, 294.42f, 403.0f, 5.1f, PromoVendors.AliaDasMapContextId, PromoVendors.JohnId, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { PromoVendors.ElmId, 0, 0, 200, 771.0f, 294.44f, 407.0f, 5.1f, PromoVendors.AliaDasMapContextId, PromoVendors.ElmId, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        }
    }

    /// <summary>General goods packages (vendordata.vendorpackages type 2) that no other vendor uses.</summary>
    public class PromoVendorPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, VendorEntry.TableName, new[] { "id", "package_id" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { PromoVendors.JohnId, 107 };
            yield return new object[] { PromoVendors.ElmId, 108 };
        }
    }

    public class PromoVendorPricePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, VendorPriceEntry.TableName, new[] { "id", "item_price" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { PromoVendors.JohnId, PromoVendors.ItemPrice };
            yield return new object[] { PromoVendors.ElmId, PromoVendors.ItemPrice };
        }
    }

    /// <summary>The stock, in the order the counter lists it; each item template's class in the comment.</summary>
    public class PromoVendorItemPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, VendorItemEntry.TableName, new[] { "id", "item_template_id" });
        }

        protected override IEnumerable<object[]> GetRows()
        {
            const uint john = PromoVendors.JohnId;
            const uint elm = PromoVendors.ElmId;

            // John: 45 promotional items.
            yield return new object[] { john, 111124 }; // AccountReward_Dye_Recipe_V1
            yield return new object[] { john, 111125 }; // AccountReward_Dye_Recipe_V2
            yield return new object[] { john, 111126 }; // AccountReward_Dye_Recipe_V3
            yield return new object[] { john, 111127 }; // AccountReward_Dye_Recipe_V4
            yield return new object[] { john, 111128 }; // AccountReward_Dye_V1
            yield return new object[] { john, 111129 }; // AccountReward_Dye_V2
            yield return new object[] { john, 111130 }; // AccountReward_Dye_V3
            yield return new object[] { john, 111131 }; // AccountReward_Dye_V4
            yield return new object[] { john, 111119 }; // AccountReward_PetSummoner_BooBot
            yield return new object[] { john, 122247 }; // AccountReward_PetSummoner_Lumin_Blue
            yield return new object[] { john, 122246 }; // AccountReward_PetSummoner_Lumin_Green
            yield return new object[] { john, 122245 }; // AccountReward_PetSummoner_Lumin_Purple
            yield return new object[] { john, 122248 }; // AccountReward_PetSummoner_Lumin_Red
            yield return new object[] { john, 111117 }; // AccountReward_PetSummoner_PineOck
            yield return new object[] { john, 111118 }; // AccountReward_PetSummoner_ShellBot
            yield return new object[] { john, 131948 }; // AccountReward_Sunset_PetSummoner_Cavern_Creepa
            yield return new object[] { john, 131949 }; // AccountReward_Sunset_PetSummoner_Geyser_Hopper
            yield return new object[] { john, 131950 }; // AccountReward_Sunset_PetSummoner_Grisel
            yield return new object[] { john, 131951 }; // AccountReward_Sunset_PetSummoner_Kalastride
            yield return new object[] { john, 131952 }; // AccountReward_Sunset_PetSummoner_Lavar
            yield return new object[] { john, 131953 }; // AccountReward_Sunset_PetSummoner_Lognar
            yield return new object[] { john, 131954 }; // AccountReward_Sunset_PetSummoner_Mush_Digger
            yield return new object[] { john, 131955 }; // AccountReward_Sunset_PetSummoner_Rhegit
            yield return new object[] { john, 131956 }; // AccountReward_Sunset_PetSummoner_Salvage_Bot
            yield return new object[] { john, 131957 }; // AccountReward_Sunset_PetSummoner_Skitterin
            yield return new object[] { john, 131958 }; // AccountReward_Sunset_PetSummoner_Spore_Hitcher
            yield return new object[] { john, 131959 }; // AccountReward_Sunset_PetSummoner_Swamp_Rat
            yield return new object[] { john, 131960 }; // AccountReward_Sunset_PetSummoner_Yimma
            yield return new object[] { john, 122878 }; // AccountReward_SoyuzModelRocket_Amazon
            yield return new object[] { john, 122835 }; // AccountReward_SoyuzModelRocket_BestBuy
            yield return new object[] { john, 122877 }; // AccountReward_SoyuzModelRocket_CircuitCity
            yield return new object[] { john, 122834 }; // AccountReward_SoyuzModelRocket_Default
            yield return new object[] { john, 123340 }; // AccountReward_SoyuzModelRocket_DestinationGames
            yield return new object[] { john, 122843 }; // AccountReward_SoyuzModelRocket_Gamestop
            yield return new object[] { john, 123341 }; // AccountReward_SoyuzModelRocket_NCsoft
            yield return new object[] { john, 123342 }; // AccountReward_SoyuzModelRocket_TabulaRasa
            yield return new object[] { john, 122876 }; // AccountReward_SoyuzModelRocket_TenTonHammer
            yield return new object[] { john, 122272 }; // AccountReward_Vet09_Title_Shadow
            yield return new object[] { john, 130394 }; // AccountReward_Vet12_Title_Special_Forces
            yield return new object[] { john, 130393 }; // AccountReward_Vet12_Resupply_Vendor_Bot
            yield return new object[] { john, 130285 }; // AccountReward_Vet12_Temp_Dropship_Beacon
            yield return new object[] { john, 130396 }; // AccountReward_Vet9_Consumable_XP_Booster
            yield return new object[] { john, 131481 }; // AccountReward_Holiday_Consumable_Snowball
            yield return new object[] { john, 131482 }; // AccountReward_Weapon_Avatar_Snowball_Launcher
            yield return new object[] { john, 122853 }; // AccountReward_Armor_Special_Spacehelmet

            // Elm: 56 emotes - the bonus emotes, then the Sunset emotes.
            yield return new object[] { elm, 122132 }; // AccountReward_Emote_Ballet
            yield return new object[] { elm, 122134 }; // AccountReward_Emote_Breakdance
            yield return new object[] { elm, 117201 }; // AccountReward_Emote_Cutthroat
            yield return new object[] { elm, 117199 }; // AccountReward_Emote_Defeat
            yield return new object[] { elm, 131483 }; // AccountReward_Emote_Drunk
            yield return new object[] { elm, 117169 }; // AccountReward_Emote_Fireworks
            yield return new object[] { elm, 117202 }; // AccountReward_Emote_FireworksBlue
            yield return new object[] { elm, 117196 }; // AccountReward_Emote_FireworksFountain
            yield return new object[] { elm, 117203 }; // AccountReward_Emote_FireworksRed
            yield return new object[] { elm, 117204 }; // AccountReward_Emote_FireworksWhite
            yield return new object[] { elm, 111317 }; // AccountReward_Emote_Golfclap
            yield return new object[] { elm, 117168 }; // AccountReward_Emote_HeadBow
            yield return new object[] { elm, 117170 }; // AccountReward_Emote_Hug
            yield return new object[] { elm, 117198 }; // AccountReward_Emote_JumpForJoy
            yield return new object[] { elm, 117171 }; // AccountReward_Emote_Kiss
            yield return new object[] { elm, 111120 }; // AccountReward_Emote_LogosFist
            yield return new object[] { elm, 121308 }; // AccountReward_Emote_LogosPCGamerUK
            yield return new object[] { elm, 111122 }; // AccountReward_Emote_LogosPhi
            yield return new object[] { elm, 117200 }; // AccountReward_Emote_MomentOfSilence
            yield return new object[] { elm, 117172 }; // AccountReward_Emote_Propose
            yield return new object[] { elm, 117173 }; // AccountReward_Emote_RaisedFist
            yield return new object[] { elm, 111121 }; // AccountReward_Emote_Rave
            yield return new object[] { elm, 121309 }; // AccountReward_Emote_Robot
            yield return new object[] { elm, 117197 }; // AccountReward_Emote_Stomp
            yield return new object[] { elm, 122265 }; // AccountReward_Emote_TaiChi
            yield return new object[] { elm, 111123 }; // AccountReward_Emote_Thumbs
            yield return new object[] { elm, 117174 }; // AccountReward_Emote_Toast
            yield return new object[] { elm, 117175 }; // AccountReward_Emote_TrickOrTreat
            yield return new object[] { elm, 122133 }; // AccountReward_Emote_YMCA
            yield return new object[] { elm, 130395 }; // AccountReward_Vet12_Emote_LogosGreet
            yield return new object[] { elm, 131922 }; // AccountReward_Sunset_Emote_Airguitar
            yield return new object[] { elm, 131923 }; // AccountReward_Sunset_Emote_Fixit
            yield return new object[] { elm, 131924 }; // AccountReward_Sunset_Emote_Hug
            yield return new object[] { elm, 131925 }; // AccountReward_Sunset_Emote_Idiot
            yield return new object[] { elm, 131926 }; // AccountReward_Sunset_Emote_Kiss
            yield return new object[] { elm, 131927 }; // AccountReward_Sunset_Emote_LogosAngry
            yield return new object[] { elm, 131928 }; // AccountReward_Sunset_Emote_LogosEvil
            yield return new object[] { elm, 131929 }; // AccountReward_Sunset_Emote_LogosGood
            yield return new object[] { elm, 131930 }; // AccountReward_Sunset_Emote_LogosHappy
            yield return new object[] { elm, 131931 }; // AccountReward_Sunset_Emote_LogosIHateYou
            yield return new object[] { elm, 131932 }; // AccountReward_Sunset_Emote_LogosILoveYou
            yield return new object[] { elm, 131933 }; // AccountReward_Sunset_Emote_LogosLove
            yield return new object[] { elm, 131934 }; // AccountReward_Sunset_Emote_LogosPlanet
            yield return new object[] { elm, 131935 }; // AccountReward_Sunset_Emote_LogosSad
            yield return new object[] { elm, 131936 }; // AccountReward_Sunset_Emote_LogosStop
            yield return new object[] { elm, 131937 }; // AccountReward_Sunset_Emote_PoleDance
            yield return new object[] { elm, 131938 }; // AccountReward_Sunset_Emote_Propose
            yield return new object[] { elm, 131939 }; // AccountReward_Sunset_Emote_RaisedFist
            yield return new object[] { elm, 131940 }; // AccountReward_Sunset_Emote_Read
            yield return new object[] { elm, 131941 }; // AccountReward_Sunset_Emote_Scan
            yield return new object[] { elm, 131942 }; // AccountReward_Sunset_Emote_ShakeFist
            yield return new object[] { elm, 131943 }; // AccountReward_Sunset_Emote_ShootMe
            yield return new object[] { elm, 131944 }; // AccountReward_Sunset_Emote_Situps
            yield return new object[] { elm, 131945 }; // AccountReward_Sunset_Emote_Submission
            yield return new object[] { elm, 131946 }; // AccountReward_Sunset_Emote_Warmth
            yield return new object[] { elm, 131947 }; // AccountReward_Sunset_Emote_Windmill
        }
    }
}
