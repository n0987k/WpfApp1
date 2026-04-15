using System;
using System.Globalization;
using WpfApp1.Models;

namespace WpfApp1.Infrastructure
{
    public static class CatalogValueParser
    {
        public static object Parse(string text, CatalogColumnInfo column)
        {
            var trimmed = text == null ? string.Empty : text.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                if (!column.IsNullable)
                {
                    throw new FormatException("Поле «" + column.Name + "» обязательно для заполнения.");
                }

                return DBNull.Value;
            }

            var type = (column.DataType ?? string.Empty).ToLowerInvariant();

            if (type == "bit")
            {
                if (bool.TryParse(trimmed, out var b))
                {
                    return b;
                }

                if (trimmed == "1" || string.Equals(trimmed, "да", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (trimmed == "0" || string.Equals(trimmed, "нет", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                throw new FormatException("Поле «" + column.Name + "»: укажите 0/1 или да/нет.");
            }

            if (type == "tinyint")
            {
                if (byte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out var v))
                {
                    return v;
                }

                throw new FormatException("Поле «" + column.Name + "»: ожидается целое число (0–255).");
            }

            if (type == "smallint")
            {
                if (short.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out var v))
                {
                    return v;
                }

                throw new FormatException("Поле «" + column.Name + "»: ожидается целое число.");
            }

            if (type == "int")
            {
                if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out var v))
                {
                    return v;
                }

                throw new FormatException("Поле «" + column.Name + "»: ожидается целое число.");
            }

            if (type == "bigint")
            {
                if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out var v))
                {
                    return v;
                }

                throw new FormatException("Поле «" + column.Name + "»: ожидается целое число.");
            }

            if (type == "decimal" || type == "numeric" || type == "money" || type == "smallmoney")
            {
                if (TryParseDecimal(trimmed, out var d))
                {
                    return d;
                }

                throw new FormatException("Поле «" + column.Name + "»: ожидается число.");
            }

            if (type == "float" || type == "real")
            {
                if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.CurrentCulture, out var d))
                {
                    return d;
                }

                if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                {
                    return d;
                }

                throw new FormatException("Поле «" + column.Name + "»: ожидается число.");
            }

            if (type == "datetime" || type == "datetime2" || type == "date" || type == "smalldatetime" || type == "time")
            {
                if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
                {
                    return dt;
                }

                if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    return dt;
                }

                throw new FormatException("Поле «" + column.Name + "»: ожидается дата/время.");
            }

            if (type == "uniqueidentifier")
            {
                if (Guid.TryParse(trimmed, out var g))
                {
                    return g;
                }

                throw new FormatException("Поле «" + column.Name + "»: ожидается GUID.");
            }

            if (type.Contains("binary") || type == "image" || type == "rowversion" || type == "timestamp")
            {
                throw new FormatException("Поле «" + column.Name + "»: двоичный тип в этой форме не редактируется.");
            }

            var s = trimmed;
            var maxLen = column.CharacterMaxLength;

            if (maxLen.HasValue && maxLen.Value > 0 && s.Length > maxLen.Value)
            {
                s = s.Substring(0, maxLen.Value);
            }

            return s;
        }

        private static bool TryParseDecimal(string trimmed, out decimal value)
        {
            if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            return decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }
}
