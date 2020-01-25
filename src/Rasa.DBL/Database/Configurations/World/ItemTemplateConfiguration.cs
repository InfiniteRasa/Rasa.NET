using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ItemTemplateConfiguration : IEntityTypeConfiguration<ItemTemplateEntry>
    {
        public void Configure(EntityTypeBuilder<ItemTemplateEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("itemtemplate");

            builder.Property(e => e.BoundToCharacterFlag)
                .HasColumnName("boundToCharacterFlag")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.BuyPrice)
                .HasColumnName("buyPrice")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.HasAccountUniqueFlag)
                .HasColumnName("hasAccountUniqueFlag")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.HasBoEFlag)
                .HasColumnName("hasBoEFlag")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.HasCharacterUniqueFlag)
                .HasColumnName("hasCharacterUniqueFlag")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.HasSellableFlag)
                .HasColumnName("hasSellableFlag")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.InventoryCategory)
                .HasColumnName("inventoryCategory")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");

            builder.Property(e => e.NotPlaceableInLockBoxFlag)
                .HasColumnName("notPlaceableInLockBoxFlag")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.NotTradableFlag)
                .HasColumnName("notTradableFlag")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.QualityId)
                .HasColumnName("qualityId")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.SellPrice)
                .HasColumnName("sellPrice")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");
        }
    }
}