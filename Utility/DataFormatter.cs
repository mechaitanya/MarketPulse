namespace MarketPulse.Utility
{
    internal static class DataFormatter
    {
        public static string ApplyFormatSpecifier(string value, string formatSpecifier)
        {
            switch (formatSpecifier)
            {
                case "D2":
                    if (int.TryParse(value, out int intValue))
                    {
                        return intValue.ToString("D2");
                    }
                    break;

                case "F2":
                    if (double.TryParse(value, out double doubleValue))
                    {
                        return doubleValue.ToString("F2");
                    }
                    break;

                case "F0":
                    if (double.TryParse(value, out doubleValue))
                    {
                        return doubleValue.ToString("F0");
                    }
                    break;

                case "MMM dd, yyyy":
                    if (DateTime.TryParse(value, out DateTime dateTimeValue))
                    {
                        return dateTimeValue.ToString("MMM dd, yyyy");
                    }
                    break;

                case "dd/MM/yyyy":
                    if (DateTime.TryParse(value, out dateTimeValue))
                    {
                        return dateTimeValue.ToString("dd/MM/yyyy");
                    }
                    break;

                case "MMM dd":
                    if (DateTime.TryParse(value, out dateTimeValue))
                    {
                        return dateTimeValue.ToString("MMM dd");
                    }
                    break;

                default:
                    // For any other unrecognized format specifier, return the original value
                    return value;
            }
            return value;
        }
    }
}