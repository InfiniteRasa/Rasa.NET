﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Rasa.Context.Char;

namespace Rasa.Migrations
{
    [DbContext(typeof(MySqlCharContext))]
    [Migration("20201215112758_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("Rasa.Structures.CharacterAppearanceEntry", b =>
                {
                    b.Property<uint>("CharacterId")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("character_id");

                    b.Property<uint>("Slot")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("slot");

                    b.Property<uint>("Class")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("class");

                    b.Property<uint>("Color")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("color");

                    b.HasKey("CharacterId", "Slot");

                    b.ToTable("character_appearance");
                });

            modelBuilder.Entity("Rasa.Structures.CharacterEntry", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("id");

                    b.Property<uint>("AccountId")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("account_id");

                    b.Property<uint>("Body")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("body");

                    b.Property<uint>("Class")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("class");

                    b.Property<uint>("CloneCredits")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11) unsigned")
                        .HasDefaultValue(0u)
                        .HasColumnName("clone_credits");

                    b.Property<double>("CoordX")
                        .HasColumnType("double")
                        .HasColumnName("coord_x");

                    b.Property<double>("CoordY")
                        .HasColumnType("double")
                        .HasColumnName("coord_y");

                    b.Property<double>("CoordZ")
                        .HasColumnType("double")
                        .HasColumnName("coord_z");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<uint>("Experience")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11) unsigned")
                        .HasDefaultValue(0u)
                        .HasColumnName("experience");

                    b.Property<ulong>("Gender")
                        .HasColumnType("bit")
                        .HasColumnName("gender");

                    b.Property<DateTime?>("LastLogin")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_login")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<byte>("Level")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(3) unsigned")
                        .HasDefaultValue((byte)1)
                        .HasColumnName("level");

                    b.Property<uint>("MapContextId")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("map_context_id");

                    b.Property<uint>("Mind")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("mind");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(64)")
                        .HasColumnName("name");

                    b.Property<uint>("NumLogins")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11) unsigned")
                        .HasDefaultValue(0u)
                        .HasColumnName("num_logins");

                    b.Property<byte>("Race")
                        .HasColumnType("tinyint(3) unsigned")
                        .HasColumnName("race");

                    b.Property<double>("Rotation")
                        .HasColumnType("double")
                        .HasColumnName("rotation");

                    b.Property<double>("Scale")
                        .HasColumnType("double unsigned")
                        .HasColumnName("scale");

                    b.Property<byte>("Slot")
                        .HasColumnType("tinyint(3) unsigned")
                        .HasColumnName("slot");

                    b.Property<uint>("Spirit")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("spirit");

                    b.Property<uint>("TotalTimePlayed")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11) unsigned")
                        .HasDefaultValue(0u)
                        .HasColumnName("total_time_played");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AccountId" }, "character_index_account");

                    b.ToTable("character");
                });

            modelBuilder.Entity("Rasa.Structures.ClanEntry", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(100)")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.ToTable("clan");
                });

            modelBuilder.Entity("Rasa.Structures.ClanMemberEntry", b =>
                {
                    b.Property<uint>("ClanId")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("clan_id");

                    b.Property<uint>("CharacterId")
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("character_id");

                    b.Property<byte>("Rank")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(3) unsigned")
                        .HasDefaultValue((byte)0)
                        .HasColumnName("rank");

                    b.HasKey("ClanId", "CharacterId");

                    b.HasIndex(new[] { "CharacterId" }, "clan_member_index_character")
                        .IsUnique();

                    b.ToTable("clan_member");
                });

            modelBuilder.Entity("Rasa.Structures.GameAccountEntry", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11) unsigned")
                        .HasColumnName("id")
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

                    b.Property<ulong>("CanSkipBootcamp")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(0ul)
                        .HasColumnName("can_skip_bootcamp");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("varchar(255)")
                        .HasColumnName("email");

                    b.Property<string>("FamilyName")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("varchar(64)")
                        .HasDefaultValue("")
                        .HasColumnName("family_name");

                    b.Property<string>("LastIp")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("varchar(15)")
                        .HasDefaultValue("0.0.0.0")
                        .HasColumnName("last_ip");

                    b.Property<DateTime>("LastLogin")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_login")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<byte>("Level")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(3) unsigned")
                        .HasDefaultValue((byte)0)
                        .HasColumnName("level");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(64)")
                        .HasColumnName("name");

                    b.Property<byte>("SelectedSlot")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(3) unsigned")
                        .HasDefaultValue((byte)0)
                        .HasColumnName("selected_slot");

                    b.HasKey("Id");

                    b.ToTable("account");
                });

            modelBuilder.Entity("Rasa.Structures.CharacterAppearanceEntry", b =>
                {
                    b.HasOne("Rasa.Structures.CharacterEntry", "Character")
                        .WithMany("CharacterAppearance")
                        .HasForeignKey("CharacterId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Character");
                });

            modelBuilder.Entity("Rasa.Structures.CharacterEntry", b =>
                {
                    b.HasOne("Rasa.Structures.GameAccountEntry", "GameAccount")
                        .WithMany("Characters")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("GameAccount");
                });

            modelBuilder.Entity("Rasa.Structures.ClanMemberEntry", b =>
                {
                    b.HasOne("Rasa.Structures.CharacterEntry", "Character")
                        .WithOne("MemberOfClan")
                        .HasForeignKey("Rasa.Structures.ClanMemberEntry", "CharacterId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Rasa.Structures.ClanEntry", "Clan")
                        .WithMany("Members")
                        .HasForeignKey("ClanId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Character");

                    b.Navigation("Clan");
                });

            modelBuilder.Entity("Rasa.Structures.CharacterEntry", b =>
                {
                    b.Navigation("CharacterAppearance");

                    b.Navigation("MemberOfClan");
                });

            modelBuilder.Entity("Rasa.Structures.ClanEntry", b =>
                {
                    b.Navigation("Members");
                });

            modelBuilder.Entity("Rasa.Structures.GameAccountEntry", b =>
                {
                    b.Navigation("Characters");
                });
#pragma warning restore 612, 618
        }
    }
}
