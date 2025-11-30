using System.Text.RegularExpressions;

namespace EcomerceBE.Service.user
{
    public class AddressParser
    {

        public class ParsedAddress
        {
            public string HouseNumber { get; set; }
            public string Street { get; set; }
            public string Ward { get; set; }
            public string Province { get; set; }
            public string PostalCode { get; set; }
            public string Country { get; set; }
            public string Note { get; set; }
        }

        public static ParsedAddress Parse(string fullAddress)
        {
            // chỉ tách theo dấu phẩy, bỏ khoảng trắng dư
            var parts = fullAddress
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            var result = new ParsedAddress();

            if (parts.Length > 0)
            {
                var first = parts[0];
                var numberMatch = Regex.Match(first, @"^\d+");
                if (numberMatch.Success)
                {
                    result.HouseNumber = numberMatch.Value;
                    result.Street = first.Substring(numberMatch.Length).Trim();
                }
                else
                {
                    result.Street = first;
                }
            }

            if (parts.Length > 1) result.Ward = parts[1];
            if (parts.Length > 2) result.Province = parts[2];
            if (parts.Length > 3) result.PostalCode = parts[3];
            if (parts.Length > 4) result.Country = parts[4];
            if (parts.Length > 5) result.Note = parts[5];

            return result;
        }

    }
}
