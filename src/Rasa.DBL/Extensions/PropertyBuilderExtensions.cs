using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Extensions
{
    using Services;
    using Services.DbContext;

    public static class PropertyBuilderExtensions
    {
        public static PropertyBuilder<T> AsIdColumn<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier)
        {
            return modifier.AsIdColumn(builder);
        }

        public static PropertyBuilder<T> AsCurrentDateTime<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier)
        {
            return modifier.AsCurrentDateTime(builder);
        }

        public static PropertyBuilder<T> AsInt<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier, in int length)
        {
            return modifier.AsInt(builder, length);
        }

        public static PropertyBuilder<T> AsTinyInt<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier, in int length)
        {
            return modifier.AsTinyInt(builder, length);
        }

        public static PropertyBuilder<T> AsDouble<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier, in bool unsigned)
        {
            return modifier.AsDouble(builder, unsigned);
        }
    }
}