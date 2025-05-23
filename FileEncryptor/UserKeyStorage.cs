//using System;
//using System.IO;
//using System.Security.Cryptography;
//using System.Text;

//namespace FileEncryptor
//{
//    public class UserKeyStorage
//    {
//        private readonly string _keyDirectory;
//        private readonly string _privateKeyPath;
//        private readonly string _publicKeyPath;

//        public UserKeyStorage(string username)
//        {
//            _keyDirectory = Path.Combine("UserData", username, "Keys");
//            _privateKeyPath = Path.Combine(_keyDirectory, "private.key");
//            _publicKeyPath = Path.Combine(_keyDirectory, "public.key");

//            if (!Directory.Exists(_keyDirectory))
//            {
//                Directory.CreateDirectory(_keyDirectory);
//            }
//        }

//        public bool HasKeys()
//        {
//            return File.Exists(_privateKeyPath) && File.Exists(_publicKeyPath);
//        }

//        public void GenerateAndStoreKeys(string password, byte[] salt)
//        {
//            var rsa = new RSACryptoServiceProvider(2048);
//            var privateKey = rsa.ToXmlString(true);
//            var publicKey = rsa.ToXmlString(false);

//            var encryptedPrivateKey = EncryptPrivateKey(privateKey, password, salt);

//            File.WriteAllBytes(_privateKeyPath, encryptedPrivateKey);
//            File.WriteAllText(_publicKeyPath, publicKey);
//        }

//        public void StoreKeys(string privateKeyXml, string password, byte[] salt)
//        {
//            if (!Directory.Exists(_keyDirectory))
//                Directory.CreateDirectory(_keyDirectory);

//            var encryptedPrivateKey = EncryptPrivateKey(privateKeyXml, password, salt);
//            File.WriteAllBytes(_privateKeyPath, encryptedPrivateKey);
//        }

//        public RSACryptoServiceProvider GetPrivateKeyProvider(string password)
//        {
//            if (!File.Exists(_privateKeyPath)) return null;

//            try
//            {
//                var encryptedPrivateKey = File.ReadAllBytes(_privateKeyPath);
//                var decryptedXml = DecryptPrivateKey(encryptedPrivateKey, password);

//                var rsa = new RSACryptoServiceProvider();
//                rsa.FromXmlString(decryptedXml);
//                return rsa;
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        public string GetPublicKeyXml(string password)
//        {
//            if (!File.Exists(_publicKeyPath)) return null;

//            return File.ReadAllText(_publicKeyPath);
//        }

//        private byte[] EncryptPrivateKey(string xml, string password, byte[] salt)
//        {
//            var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256).GetBytes(32);

//            using (var aes = Aes.Create())
//            {
//                aes.Key = key;
//                aes.GenerateIV();

//                using (var ms = new MemoryStream())
//                {
//                    ms.Write(salt, 0, salt.Length);
//                    ms.Write(aes.IV, 0, aes.IV.Length);

//                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
//                    using (var sw = new StreamWriter(cs))
//                    {
//                        sw.Write(xml);
//                    }

//                    return ms.ToArray();
//                }
//            }
//        }

//        private string DecryptPrivateKey(byte[] data, string password)
//        {
//            var salt = new byte[32];
//            var iv = new byte[16];
//            Array.Copy(data, 0, salt, 0, 32);
//            Array.Copy(data, 32, iv, 0, 16);

//            var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256).GetBytes(32);

//            using (var aes = Aes.Create())
//            {
//                aes.Key = key;
//                aes.IV = iv;

//                using (var ms = new MemoryStream(data, 48, data.Length - 48))
//                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
//                using (var sr = new StreamReader(cs))
//                {
//                    return sr.ReadToEnd();
//                }
//            }
//        }
//    }
//}
using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileEncryptor
{
    public class UserKeyStorage
    {
        private readonly string _username;
        private readonly string _keyDirectory;
        private readonly string _privateKeyPath;
        private readonly string _publicKeyPath;

        public UserKeyStorage(string username)
        {
            _username = username;
            _keyDirectory = Path.Combine("UserData", username, "Keys");
            _privateKeyPath = Path.Combine(_keyDirectory, "private.key");
            _publicKeyPath = Path.Combine(_keyDirectory, "public.key");

            if (!Directory.Exists(_keyDirectory))
            {
                Directory.CreateDirectory(_keyDirectory);
            }
        }

        public bool HasKeys()
        {
            return File.Exists(_privateKeyPath) && File.Exists(_publicKeyPath);
        }

        public void GenerateAndStoreKeys(string password, byte[] salt)
        {
            var rsa = new RSACryptoServiceProvider(2048);
            var privateKey = rsa.ToXmlString(true);
            var publicKey = rsa.ToXmlString(false);

            var encryptedPrivateKey = EncryptPrivateKey(privateKey, password, salt);

            File.WriteAllBytes(_privateKeyPath, encryptedPrivateKey);
            File.WriteAllText(_publicKeyPath, publicKey);

            // Обновление публичного ключа в базе данных
            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE Users SET PublicKey = @pk WHERE Username = @u", conn))
                {
                    cmd.Parameters.AddWithValue("@pk", publicKey);
                    cmd.Parameters.AddWithValue("@u", _username);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void StoreKeys(string privateKeyXml, string password, byte[] salt)
        {
            if (!Directory.Exists(_keyDirectory))
                Directory.CreateDirectory(_keyDirectory);

            var encryptedPrivateKey = EncryptPrivateKey(privateKeyXml, password, salt);
            File.WriteAllBytes(_privateKeyPath, encryptedPrivateKey);
        }

        public RSACryptoServiceProvider GetPrivateKeyProvider(string password)
        {
            if (!File.Exists(_privateKeyPath)) return null;

            try
            {
                var encryptedPrivateKey = File.ReadAllBytes(_privateKeyPath);
                var decryptedXml = DecryptPrivateKey(encryptedPrivateKey, password);

                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(decryptedXml);
                return rsa;
            }
            catch
            {
                return null;
            }
        }

        public string GetPublicKeyXml(string password)
        {
            if (!File.Exists(_publicKeyPath)) return null;

            return File.ReadAllText(_publicKeyPath);
        }

        private byte[] EncryptPrivateKey(string xml, string password, byte[] salt)
        {
            var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256).GetBytes(32);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                {
                    ms.Write(salt, 0, salt.Length);
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(xml);
                    }

                    return ms.ToArray();
                }
            }
        }

        private string DecryptPrivateKey(byte[] data, string password)
        {
            var salt = new byte[32];
            var iv = new byte[16];
            Array.Copy(data, 0, salt, 0, 32);
            Array.Copy(data, 32, iv, 0, 16);

            var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256).GetBytes(32);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream(data, 48, data.Length - 48))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
