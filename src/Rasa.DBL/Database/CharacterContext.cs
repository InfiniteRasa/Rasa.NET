using Microsoft.EntityFrameworkCore;

namespace Rasa.Database
{
    using Structures;

    public partial class CharacterContext : DbContext
    {
        public CharacterContext()
        {
        }

        public CharacterContext(DbContextOptions<CharacterContext> options)
            : base(options)
        {
        }

        public virtual DbSet<GameAccountEntry> Account { get; set; }
        public virtual DbSet<CharacterEntry> Character { get; set; }
        public virtual DbSet<CharacterAbilitydrawerEntry> CharacterAbilitydrawer { get; set; }
        public virtual DbSet<CharacterAppearanceEntry> CharacterAppearance { get; set; }
        public virtual DbSet<CharacterInventoryEntry> CharacterInventory { get; set; }
        public virtual DbSet<CharacterLockboxEntry> CharacterLockbox { get; set; }
        public virtual DbSet<CharacterLogosEntry> CharacterLogos { get; set; }
        public virtual DbSet<CharacterMissionsEntry> CharacterMissions { get; set; }
        public virtual DbSet<CharacterOptionsEntry> CharacterOptions { get; set; }
        public virtual DbSet<CharacterSkillsEntry> CharacterSkills { get; set; }
        public virtual DbSet<CharacterTeleportersEntry> CharacterTeleporters { get; set; }
        public virtual DbSet<CharacterTitlesEntry> CharacterTitles { get; set; }
        public virtual DbSet<ClanEntry> Clan { get; set; }
        public virtual DbSet<ClanMemberEntry> ClanMember { get; set; }
        public virtual DbSet<ItemsEntry> Items { get; set; }
        public virtual DbSet<UserOptionsEntry> UserOptions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorldContext).Assembly,
                t => t.Namespace?.StartsWith("Rasa.Database.Configurations.Character") ?? false);

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
