using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Services.DbContext
{
    public class SqliteDbContextPropertyModifier : IDbContextPropertyModifier
    {
        public PropertyBuilder<T> AsIdColumn<T>(PropertyBuilder<T> builder)
        {
            return builder.HasColumnType("integer");
        }

        public PropertyBuilder<T> AsUnsignedTinyInt<T>(PropertyBuilder<T> builder, in int length)
        {
            // sqlite doesn't know unsigned
            return builder.HasColumnType($"tinyint({length})");
        }

        public PropertyBuilder<T> AsUnsignedInt<T>(PropertyBuilder<T> builder, in int length)
        {
            // sqlite doesn't know unsigned
            return builder.HasColumnType($"int({length})");
        }

        public PropertyBuilder<T> AsUnsignedBigInt<T>(PropertyBuilder<T> builder, in int length)
        {
            // sqlite doesn't know unsigned
            return builder.HasColumnType($"bigint({length})");
        }

        public PropertyBuilder<T> AsUnsignedDouble<T>(PropertyBuilder<T> builder)
        {
            // sqlite doesn't know unsigned
            return builder.HasColumnType("double");
        }

        public PropertyBuilder<T> AsCurrentDateTime<T>(PropertyBuilder<T> builder)
        {
            return builder.HasDefaultValueSql("datetime('now')");
        }
    }
}