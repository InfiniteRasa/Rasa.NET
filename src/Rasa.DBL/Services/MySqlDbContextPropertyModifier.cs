namespace Rasa.Services
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class MySqlDbContextPropertyModifier : IDbContextPropertyModifier
    {
        public PropertyBuilder<T> AsIdColumn<T>(PropertyBuilder<T> builder)
        {
            return builder.HasColumnType("int(11) unsigned");
        }

        public PropertyBuilder<T> AsTinyInt<T>(PropertyBuilder<T> builder, int length)
        {
            return builder.HasColumnType($"tinyint({length}) unsigned");
        }

        public PropertyBuilder<T> AsCurrentDateTime<T>(PropertyBuilder<T> builder)
        {
            return builder.HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}