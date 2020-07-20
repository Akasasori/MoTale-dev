using OpenNos.GameObject.Networking;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OpenNos.GameObject.Helpers
{
    public static class AntiCheatHelper
    {
        #region Members

        private static readonly string _table = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        #endregion

        #region Properties

        public static bool IsAntiCheatEnabled => ServerManager.Instance.Configuration.IsAntiCheatEnabled;

        public static string ClientKey => ServerManager.Instance.Configuration.AntiCheatClientKey;

        public static string ServerKey => ServerManager.Instance.Configuration.AntiCheatServerKey;

        #endregion

        #region Methods

        public static string Encrypt(byte[] data, byte[] encryptedKey)
        {
            byte[] plainTextKey = encryptedKey;

            for (int i = 0; i < data.Length; i++)
            {
                plainTextKey[i] ^= 0x0F;
            }

            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= plainTextKey[i];
            }

            return Encoding.UTF8.GetString(data);
        }

        public static string GenerateData(int length)
        {
            string result = "";

            while (length-- > 0)
            {
                result += _table[ServerManager.RandomNumber(0, _table.Length)];
            }

            return result;
        }

        public static bool IsValidSignature(string signature, byte[] data, byte[] encryptedKey, string crc32)
        {
            byte[] buffer = data.Concat(encryptedKey).Concat(Encoding.ASCII.GetBytes(crc32)).Concat(Encoding.ASCII.GetBytes(ServerKey)).ToArray();

            return signature == Sha512(buffer);
        }

        public static string Sha512(string input) => Sha512(Encoding.ASCII.GetBytes(input));

        public static string Sha512(byte[] buffer)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                return string.Concat(sha512.ComputeHash(buffer).Select(s => s.ToString("X2")));
            }
        }

        #endregion
    }
}