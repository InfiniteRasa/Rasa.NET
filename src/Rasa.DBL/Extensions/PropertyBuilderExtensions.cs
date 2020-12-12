using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Extensions
{
    using Services;

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

        public static PropertyBuilder<T> AsTinyInt<T>(this PropertyBuilder<T> builder, IDbContextPropertyModifier modifier, int length)
        {
            return modifier.AsTinyInt(builder, length);
        }
    }
}