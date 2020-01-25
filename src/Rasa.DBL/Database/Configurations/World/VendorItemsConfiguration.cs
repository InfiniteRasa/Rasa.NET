using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class VendorItemsConfiguration : IEntityTypeConfiguration<VendorItemsEntry>
    {
        public void Configure(EntityTypeBuilder<VendorItemsEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("vendor_items");

            builder.Property(e => e.DbId)
                .HasColumnName("creatureDbId")
                .HasColumnType("int(11)");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");
        }
    }
}