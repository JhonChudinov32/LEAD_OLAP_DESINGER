using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LEAD_OLAP_DESINGER.Helpers
{
    public static class EncryptionHelper
    {
        private static readonly string EncryptionKey = LoadOrGenerateKey(); // Замените на свой ключ шифрования

        public static string Encrypt(string plainText)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(16).Substring(0, 16));
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = new byte[16]; // Инициализационный вектор (нулевой)
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(16).Substring(0, 16));
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = new byte[16];
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        private static string LoadOrGenerateKey()
        {
            string keyFilePath = Path.Combine(DirectoryHelper.GetResDirectory(), "config", "encryption_key.txt");
            if (File.Exists(keyFilePath))
            {
                return File.ReadAllText(keyFilePath);
            }

            string generatedKey = GenerateEncryptionKey();
            File.WriteAllText(keyFilePath, generatedKey);
            return generatedKey;
        }

        public static string GenerateEncryptionKey()
        {
            using (var aes = Aes.Create())
            {
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);

            }
        }
    }
}
