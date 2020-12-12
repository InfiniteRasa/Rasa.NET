namespace Rasa.Services
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class SqliteDbContextPropertyModifier : IDbContextPropertyModifier
    {
        public PropertyBuilder<T> AsIdColumn<T>(PropertyBuilder<T> builder)
        {
            return builder.HasColumnType("integer");
        }

        public PropertyBuilder<T> AsTinyInt<T>(PropertyBuilder<T> builder, int length)
        {
            return builder.HasColumnType($"tinyint({length})");
        }

        public PropertyBuilder<T> AsCurrentDateTime<T>(PropertyBuilder<T> builder)
        {
            return builder.HasDefaultValueSql("date('now')");
        }
    }
}