using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Services.DbContext
{
    public interface IDbContextPropertyModifier
    {
        PropertyBuilder<T> AsIdColumn<T>(PropertyBuilder<T> builder);

        PropertyBuilder<T> AsCurrentDateTime<T>(PropertyBuilder<T> builder);

        PropertyBuilder<T> AsUnsignedTinyInt<T>(PropertyBuilder<T> builder, in int length);

        PropertyBuilder<T> AsUnsignedInt<T>(PropertyBuilder<T> builder, in int length);

        PropertyBuilder<T> AsUnsignedDouble<T>(PropertyBuilder<T> builder);
    }
}