using System.Security.Cryptography;
using System.Text;

namespace TurniaCore.Libraries.Utilities
{
    public static class Utils
    {
        public static string GetSha512Hash(string value)
        {
            var stringToHash = value;
            using var sha512Hash = SHA512.Create();
            var byteArray = sha512Hash.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
            return ConvertByteArrayToString(byteArray);
        }

        private static string ConvertByteArrayToString(IEnumerable<byte> byteArray)
        {
            var sb = new StringBuilder();
            foreach (var t in byteArray)
            {
                sb.Append(t.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
