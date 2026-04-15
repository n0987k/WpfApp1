using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace WpfApp1.Infrastructure
{
    public static class InputValidation
    {
        private static readonly Regex PhoneDigitsRegex = new Regex(@"^[\d\s\+\-\(\)]{10,}$", RegexOptions.Compiled);

        public static bool IsValidOptionalEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return true;
            }

            try
            {
                new MailAddress(email.Trim());
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool IsValidOptionalPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return true;
            }

            var trimmed = phone.Trim();
            var digitCount = 0;

            foreach (var ch in trimmed)
            {
                if (char.IsDigit(ch))
                {
                    digitCount++;
                }
            }

            if (digitCount < 10)
            {
                return false;
            }

            return PhoneDigitsRegex.IsMatch(trimmed);
        }
    }
}
