using System.Globalization;

namespace ECommerce.Module.Core.Services
{
    public interface ICurrencyService
    {
        CultureInfo CurrencyCulture { get; }

        string FormatCurrency(decimal value);
    }
}
