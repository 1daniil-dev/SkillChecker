using System.Security.Cryptography;
using System.Text;
using SkillChecker.Common.Security;

namespace SkillChecker.Tests
{
    public class PasswordHasherTests
    {
        [Fact]
        public void Hash_ProducesPbkdf2FormattedString()
        {
            string stored = PasswordHasher.Hash("test_password");

            Assert.StartsWith(PasswordHasher.Pbkdf2Prefix, stored);
            string body = stored.Substring(PasswordHasher.Pbkdf2Prefix.Length);
            string[] parts = body.Split('$');
            Assert.Equal(3, parts.Length);
            Assert.Equal(PasswordHasher.Pbkdf2Iterations.ToString(), parts[0]);
        }

        [Fact]
        public void Hash_TwoCallsProduceDifferentResults_DueToRandomSalt()
        {
            string a = PasswordHasher.Hash("same_password");
            string b = PasswordHasher.Hash("same_password");

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Verify_ValidPassword_ReturnsTrue()
        {
            string stored = PasswordHasher.Hash("correct horse battery staple");
            bool needsMigration;

            bool result = PasswordHasher.Verify("correct horse battery staple", stored, out needsMigration);

            Assert.True(result);
            Assert.False(needsMigration);
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            string stored = PasswordHasher.Hash("correct");
            bool needsMigration;

            bool result = PasswordHasher.Verify("wrong", stored, out needsMigration);

            Assert.False(result);
            Assert.False(needsMigration);
        }

        [Fact]
        public void Verify_CaseSensitive()
        {
            string stored = PasswordHasher.Hash("Pass123");
            bool needsMigration;

            bool result = PasswordHasher.Verify("pass123", stored, out needsMigration);

            Assert.False(result);
        }

        [Fact]
        public void Verify_EmptyPassword_RoundtripsCorrectly()
        {
            string stored = PasswordHasher.Hash("");
            bool needsMigration;

            bool result = PasswordHasher.Verify("", stored, out needsMigration);

            Assert.True(result);
            Assert.False(needsMigration);
        }

        [Fact]
        public void Verify_UnicodePassword_RoundtripsCorrectly()
        {
            string password = "пароль_с_кириллицей_ёж";
            string stored = PasswordHasher.Hash(password);
            bool needsMigration;

            bool result = PasswordHasher.Verify(password, stored, out needsMigration);

            Assert.True(result);
        }

        [Fact]
        public void Verify_LegacySha256Hash_ReturnsTrueAndFlagsMigration()
        {
            string password = "old_password";
            string legacyHash = ComputeLegacySha256Hex(password);
            bool needsMigration;

            bool result = PasswordHasher.Verify(password, legacyHash, out needsMigration);

            Assert.True(result);
            Assert.True(needsMigration);
        }

        [Fact]
        public void Verify_LegacySha256Hash_WrongPassword_ReturnsFalseAndNotMigrating()
        {
            string legacyHash = ComputeLegacySha256Hex("real");
            bool needsMigration;

            bool result = PasswordHasher.Verify("wrong", legacyHash, out needsMigration);

            Assert.False(result);
            Assert.False(needsMigration);
        }

        [Fact]
        public void Verify_LegacySha256Hash_UppercaseHex_StillVerifies()
        {
            string password = "abc";
            string legacyHash = ComputeLegacySha256Hex(password).ToUpperInvariant();
            bool needsMigration;

            bool result = PasswordHasher.Verify(password, legacyHash, out needsMigration);

            Assert.True(result);
            Assert.True(needsMigration);
        }

        [Fact]
        public void Verify_EmptyStoredHash_ReturnsFalse()
        {
            bool needsMigration;

            bool result = PasswordHasher.Verify("anything", "", out needsMigration);

            Assert.False(result);
            Assert.False(needsMigration);
        }

        [Fact]
        public void Verify_NullPassword_ReturnsFalse()
        {
            string stored = PasswordHasher.Hash("real");
            bool needsMigration;

            bool result = PasswordHasher.Verify(null!, stored, out needsMigration);

            Assert.False(result);
        }

        [Fact]
        public void Verify_NullStoredHash_ReturnsFalse()
        {
            bool needsMigration;

            bool result = PasswordHasher.Verify("anything", null!, out needsMigration);

            Assert.False(result);
            Assert.False(needsMigration);
        }

        [Fact]
        public void Verify_MalformedPbkdf2_ReturnsFalse()
        {
            bool needsMigration;

            Assert.False(PasswordHasher.Verify("x", PasswordHasher.Pbkdf2Prefix + "abc", out needsMigration));
            Assert.False(PasswordHasher.Verify("x", PasswordHasher.Pbkdf2Prefix + "abc$def", out needsMigration));
            Assert.False(PasswordHasher.Verify("x", PasswordHasher.Pbkdf2Prefix + "0$AAAA$BBBB", out needsMigration));
            Assert.False(PasswordHasher.Verify("x", PasswordHasher.Pbkdf2Prefix + "-5$AAAA$BBBB", out needsMigration));
            Assert.False(PasswordHasher.Verify("x", PasswordHasher.Pbkdf2Prefix + "100$!!!$BBBB", out needsMigration));
        }

        [Fact]
        public void Verify_GarbageStored_ReturnsFalse()
        {
            bool needsMigration;

            Assert.False(PasswordHasher.Verify("x", "not_a_hash_at_all", out needsMigration));
            Assert.False(PasswordHasher.Verify("x", "deadbeef", out needsMigration));
            Assert.False(PasswordHasher.Verify("x", new string('z', 64), out needsMigration));
        }

        [Fact]
        public void Hash_DefaultIterationsAtLeastOwaspMinimum()
        {
            Assert.True(PasswordHasher.Pbkdf2Iterations >= 210000);
        }

        [Fact]
        public void Hash_SaltSizeMatchesSpec()
        {
            string stored = PasswordHasher.Hash("test");
            string[] parts = stored.Substring(PasswordHasher.Pbkdf2Prefix.Length).Split('$');
            byte[] salt = Convert.FromBase64String(parts[1]);

            Assert.Equal(PasswordHasher.Pbkdf2SaltSize, salt.Length);
        }

        [Fact]
        public void Hash_OutputSizeMatchesSpec()
        {
            string stored = PasswordHasher.Hash("test");
            string[] parts = stored.Substring(PasswordHasher.Pbkdf2Prefix.Length).Split('$');
            byte[] hash = Convert.FromBase64String(parts[2]);

            Assert.Equal(PasswordHasher.Pbkdf2HashSize, hash.Length);
        }

        private static string ComputeLegacySha256Hex(string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = SHA256.HashData(bytes);
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
