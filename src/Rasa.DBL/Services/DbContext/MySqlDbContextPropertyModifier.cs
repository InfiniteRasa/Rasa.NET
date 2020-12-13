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

        public PropertyBuilder<T> AsTinyInt<T>(PropertyBuilder<T> builder, in int length)
        {
            return builder.HasColumnType($"tinyint({length}) unsigned");
        }

        public PropertyBuilder<T> AsInt<T>(PropertyBuilder<T> builder, in int length)
        {
            return builder.HasColumnType($"int({length}) unsigned");
        }

        public PropertyBuilder<T> AsDouble<T>(PropertyBuilder<T> builder, bool unsigned)
        {
            return builder.HasColumnType($"double{(unsigned ? " unsigned" : string.Empty)}");
        }

        public PropertyBuilder<T> AsCurrentDateTime<T>(PropertyBuilder<T> builder)
        {
            return builder.HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}