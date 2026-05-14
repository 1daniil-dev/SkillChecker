using System.Security.Cryptography;
using System.Text;

namespace SkillChecker.Common.Security
{
    public static class PasswordHasher
    {
        public const string Pbkdf2Prefix = "pbkdf2$";
        public const int Pbkdf2Iterations = 210000;
        public const int Pbkdf2SaltSize = 16;
        public const int Pbkdf2HashSize = 32;

        public static string Hash(string password)
        {
            string safe = password == null ? "" : password;
            byte[] salt = new byte[Pbkdf2SaltSize];
            RandomNumberGenerator.Fill(salt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                safe,
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                Pbkdf2HashSize);
            StringBuilder sb = new StringBuilder();
            sb.Append(Pbkdf2Prefix);
            sb.Append(Pbkdf2Iterations);
            sb.Append('$');
            sb.Append(Convert.ToBase64String(salt));
            sb.Append('$');
            sb.Append(Convert.ToBase64String(hash));
            return sb.ToString();
        }

        public static bool Verify(string password, string stored, out bool needsMigration)
        {
            needsMigration = false;
            if (password == null || stored == null || stored.Length == 0) return false;
            if (stored.StartsWith(Pbkdf2Prefix))
            {
                return VerifyPbkdf2(password, stored);
            }
            bool legacyOk = VerifyLegacySha256(password, stored);
            if (legacyOk) needsMigration = true;
            return legacyOk;
        }

        private static bool VerifyPbkdf2(string password, string stored)
        {
            string body = stored.Substring(Pbkdf2Prefix.Length);
            string[] parts = body.Split('$');
            if (parts.Length != 3) return false;
            int iterations;
            if (!int.TryParse(parts[0], out iterations)) return false;
            if (iterations <= 0) return false;
            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }
            if (salt.Length == 0 || expected.Length == 0) return false;
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        private static bool VerifyLegacySha256(string password, string stored)
        {
            if (stored.Length != 64) return false;
            for (int i = 0; i < stored.Length; i++)
            {
                char c = stored[i];
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex) return false;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = SHA256.HashData(bytes);
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            string actual = sb.ToString();
            byte[] actualBytes = Encoding.ASCII.GetBytes(actual);
            byte[] storedBytes = Encoding.ASCII.GetBytes(stored.ToLowerInvariant());
            if (actualBytes.Length != storedBytes.Length) return false;
            return CryptographicOperations.FixedTimeEquals(actualBytes, storedBytes);
        }
    }
}
