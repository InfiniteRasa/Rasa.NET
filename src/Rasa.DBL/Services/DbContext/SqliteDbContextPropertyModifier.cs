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

        public PropertyBuilder<T> AsTinyInt<T>(PropertyBuilder<T> builder, in int length)
        {
            return builder.HasColumnType($"tinyint({length})");
        }

        public PropertyBuilder<T> AsInt<T>(PropertyBuilder<T> builder, in int length)
        {
            return builder.HasColumnType($"int({length})");
        }

        public PropertyBuilder<T> AsDouble<T>(PropertyBuilder<T> builder, bool unsigned)
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