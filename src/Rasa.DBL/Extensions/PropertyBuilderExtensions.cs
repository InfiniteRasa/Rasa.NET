using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Extensions
{
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

        public static PropertyBuilder<T> AsUnsignedInt<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier, in int length)
        {
            return modifier.AsUnsignedInt(builder, length);
        }

        public static PropertyBuilder<T> AsUnsignedTinyInt<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier, in int length)
        {
            return modifier.AsUnsignedTinyInt(builder, length);
        }

        public static PropertyBuilder<T> AsUnsignedBigInt<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier, in int length)
        {
            return modifier.AsUnsignedBigInt(builder, length);
        }

        public static PropertyBuilder<T> AsUnsignedDouble<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier)
        {
            return modifier.AsUnsignedDouble(builder);
        }
    }
}