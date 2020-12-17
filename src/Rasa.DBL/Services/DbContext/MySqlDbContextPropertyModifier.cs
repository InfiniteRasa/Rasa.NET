using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Services.DbContext
{
    public class MySqlDbContextPropertyModifier : IDbContextPropertyModifier
    {
        public PropertyBuilder<T> AsIdColumn<T>(PropertyBuilder<T> builder)
        {
            return builder.HasColumnType("int(11) unsigned");
        }

        public PropertyBuilder<T> AsUnsignedTinyInt<T>(PropertyBuilder<T> builder, in int length)
        {
            return builder.HasColumnType($"tinyint({length}) unsigned");
        }

        public PropertyBuilder<T> AsUnsignedInt<T>(PropertyBuilder<T> builder, in int length)
        {
            return builder.HasColumnType($"int({length}) unsigned");
        }

        public PropertyBuilder<T> AsUnsignedDouble<T>(PropertyBuilder<T> builder)
        {
            return builder.HasColumnType("double unsigned");
        }

        public PropertyBuilder<T> AsCurrentDateTime<T>(PropertyBuilder<T> builder)
        {
            return builder.HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}