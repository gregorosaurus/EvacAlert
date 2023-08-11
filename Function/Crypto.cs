using System;
using System.Text;

namespace EvacAlert
{
	public class Crypto
	{

        public static string GenerateHash(string input)
        {
            using (System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create())
            {
                if (string.IsNullOrEmpty(input))
                    return "";

                var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                var hashString = sb.ToString().ToLower();
                return hashString;
            }
        }
    }
}

