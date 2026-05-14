using System.Security.Cryptography;
using System.Text;

namespace WhatsAppToDB.Services
{
    /// <summary>
    /// AES-256 encryption for sensitive config fields.
    /// Encrypted values are stored with an "ENC:" prefix so they are easy to identify.
    /// </summary>
    public class FieldEncryptionService
    {
        // -----------------------------------------------------------
        // Hardcoded 256-bit key (32 bytes).
        // Replace this value – keep it out of source control once you
        // move to production.  For now it lives here for simplicity.
        // -----------------------------------------------------------
        private static readonly byte[] _key = Convert.FromBase64String(
            "8/iEgUR64232aU8xsZ5yOFLcOycHl3uVJVsVFEcHQzc=" 
        );

        private const string Prefix = "ENC:";

        // -----------------------------------------------------------
        // PUBLIC API
        // -----------------------------------------------------------

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            if (plainText.StartsWith(Prefix))    return plainText;   // already encrypted

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();                       // fresh random IV each time

            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);    // prepend 16-byte IV

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
                sw.Write(plainText);

            return Prefix + Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))          return cipherText;
            if (!cipherText.StartsWith(Prefix))            return cipherText;   // not encrypted

            var bytes = Convert.FromBase64String(cipherText[Prefix.Length..]);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV  = bytes[..16];                         // extract IV from front

            using var ms = new MemoryStream(bytes[16..]);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        public bool IsEncrypted(string? value) =>
            !string.IsNullOrEmpty(value) && value.StartsWith(Prefix);
    }
}