﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Rasa.Context.World;

namespace Rasa.Migrations.SqliteWorld
{
    [DbContext(typeof(SqliteWorldContext))]
    partial class SqliteWorldContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("Rasa.Structures.World.ExperienceForLevelEntry", b =>
                {
                    b.Property<uint>("Level")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("level")
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

                    b.Property<long>("Experience")
                        .HasColumnType("bigint(20)")
                        .HasColumnName("experience");

                    b.HasKey("Level");

                    b.ToTable("player_exp_for_level");
                });

            modelBuilder.Entity("Rasa.Structures.World.ItemTemplateItemClassEntry", b =>
                {
                    b.Property<uint>("ItemTemplateId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("itemTemplateId")
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

                    b.Property<uint>("ItemClass")
                        .HasColumnType("int(11)")
                        .HasColumnName("itemClassId");

                    b.HasKey("ItemTemplateId");

                    b.ToTable("itemtemplate_itemclass");
                });

            modelBuilder.Entity("Rasa.Structures.World.RandomNameEntry", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("varchar(64)")
                        .HasColumnName("name");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint(3)")
                        .HasColumnName("type");

                    b.Property<byte>("Gender")
                        .HasColumnType("tinyint(3)")
                        .HasColumnName("gender");

                    b.HasKey("Name", "Type", "Gender");

                    b.ToTable("player_random_name");
                });
#pragma warning restore 612, 618
        }
    }
}
