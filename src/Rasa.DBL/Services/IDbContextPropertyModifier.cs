using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Services
{
    public interface IDbContextPropertyModifier
    {
        PropertyBuilder<T> AsIdColumn<T>(PropertyBuilder<T> builder);

        PropertyBuilder<T> AsCurrentDateTime<T>(PropertyBuilder<T> builder);

        PropertyBuilder<T> AsTinyInt<T>(PropertyBuilder<T> builder, int length);
    }
}