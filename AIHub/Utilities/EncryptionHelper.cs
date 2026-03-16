using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AIHub.Utilities
{
    public class EncryptedData
    {
        public string EncryptedValue { get; set; } = string.Empty;
        public string InitializationVector { get; set; } = string.Empty;
    }

    public static class EncryptionHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("A1B2C3D4E5F678901234567890ABCDEF");

        public static EncryptedData Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return new EncryptedData();
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new StreamWriter(cs)) sw.Write(plainText);
            return new EncryptedData 
            { 
                EncryptedValue = Convert.ToBase64String(ms.ToArray()),
                InitializationVector = Convert.ToBase64String(aes.IV)
            };
        }

        public static string Decrypt(string cipherText, string ivBase64)
        {
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(ivBase64)) return cipherText;
            try 
            {
                using Aes aes = Aes.Create();
                aes.Key = Key;
                aes.IV = Convert.FromBase64String(ivBase64);
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using System.IO.StreamReader sr = new System.IO.StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch 
            {
                return string.Empty; // Return empty string if decryption fails
            }
        }
    }
}
