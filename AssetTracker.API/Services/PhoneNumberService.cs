using System.Text;

namespace AssetTracker.API.Services
{
    public interface IPhoneNumberService
    {
        /// <summary>
        /// Returns true when the supplied value is a valid South African phone number.
        /// Null or empty values are treated as valid because phone numbers are optional.
        /// </summary>
        bool IsValid(string? phoneNumber);

        /// <summary>
        /// Normalizes a South African phone number to the canonical +27XXXXXXXXX format.
        /// Returns null when the input is null or empty.
        /// </summary>
        string? Normalize(string? phoneNumber);
    }

    public class PhoneNumberService : IPhoneNumberService
    {
        public bool IsValid(string? phoneNumber)
        {
            // Phone number is optional, so an empty value is acceptable.
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return true;
            }

            return TryNormalize(phoneNumber, out _);
        }

        public string? Normalize(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            return TryNormalize(phoneNumber, out var normalized)
                ? normalized
                : phoneNumber.Trim();
        }

        private static bool TryNormalize(string phoneNumber, out string normalized)
        {
            normalized = string.Empty;

            // Keep only digits and a single leading plus sign.
            var builder = new StringBuilder(phoneNumber.Length);
            foreach (var ch in phoneNumber)
            {
                if (char.IsDigit(ch))
                {
                    builder.Append(ch);
                }
                else if (ch == '+' && builder.Length == 0)
                {
                    builder.Append(ch);
                }
            }

            var cleaned = builder.ToString();
            string nationalNumber;

            if (cleaned.StartsWith("+27"))
            {
                nationalNumber = cleaned.Substring(3);
            }
            else if (cleaned.StartsWith("27") && cleaned.Length == 11)
            {
                nationalNumber = cleaned.Substring(2);
            }
            else if (cleaned.StartsWith("0"))
            {
                nationalNumber = cleaned.Substring(1);
            }
            else
            {
                return false;
            }

            // South African subscriber numbers are 9 digits and never start with 0.
            if (nationalNumber.Length != 9 || nationalNumber[0] == '0')
            {
                return false;
            }

            normalized = "+27" + nationalNumber;
            return true;
        }
    }
}
