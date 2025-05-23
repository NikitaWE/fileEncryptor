using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace FileEncryptor
{
    public class UserKeyStorage
    {
        private readonly string _username;
        private readonly string _keysFile;
        private const int KeySize = 2048;
        private const int Iterations = 100000;

        public UserKeyStorage(string username)
        {
            _username = username;
            _keysFile = Path.Combine("UserData", username, "keys.enc");
        }

        public bool HasKeys()
        {
            return File.Exists(_keysFile);
        }

        public void GenerateAndStoreKeys(string password, byte[] salt)
        {
            using (var rsa = new RSACryptoServiceProvider(KeySize))
            {
                try
                {
                    var publicKey = rsa.ToXmlString(false);
                    var privateKey = rsa.ToXmlString(true);

                    var encryptedPrivateKey = EncryptData(privateKey, password, salt);

                    var encryptedPublicKey = EncryptData(publicKey, password, salt);

                    var keyData = new KeyData
                    {
                        EncryptedPublicKey = encryptedPublicKey,
                        EncryptedPrivateKey = encryptedPrivateKey,
                        Salt = Convert.ToBase64String(salt)
                    };

                    SaveKeyData(keyData);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        public RSACryptoServiceProvider GetPrivateKeyProvider(string password)
        {
            try
            {
                var keyData = LoadKeyData();
                if (keyData == null)
                {
                    MessageBox.Show("Файл ключей не найден. Создайте новые ключи.");
                    return null;
                }

                var salt = Convert.FromBase64String(keyData.Salt);
                var decryptedPrivateKey = DecryptData(keyData.EncryptedPrivateKey, password, salt);

                if (string.IsNullOrEmpty(decryptedPrivateKey))
                {
                    MessageBox.Show("Неверный пароль. Попробуйте снова.");
                    return null;
                }

                var rsa = new RSACryptoServiceProvider(2048);
                rsa.FromXmlString(decryptedPrivateKey);
                return rsa;
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show($"Ошибка дешифровки: {ex.Message}\nПроверьте пароль.");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ключа: {ex.Message}");
                return null;
            }
        }

        public string GetPublicKeyXml(string password)
        {
            try
            {
                var keyData = LoadKeyData();
                if (keyData == null || keyData.EncryptedPublicKey == null)
                {
                    MessageBox.Show("Файл ключей не найден или не содержит открытый ключ");
                    return null;
                }

                var salt = Convert.FromBase64String(keyData.Salt);
                return DecryptData(keyData.EncryptedPublicKey, password, salt);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения ключа: {ex.Message}");
                return null;
            }
        }

        private byte[] EncryptData(string data, string password, byte[] salt)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);
                    aes.Key = key.GetBytes(32);
                    aes.IV = key.GetBytes(16);

                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        var bytes = Encoding.UTF8.GetBytes(data);
                        cs.Write(bytes, 0, bytes.Length);
                        cs.FlushFinalBlock();
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка шифрования: {ex.Message}");
                return null;
            }
        }

        private string DecryptData(byte[] encryptedData, string password, byte[] salt)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);
                    aes.Key = key.GetBytes(32);
                    aes.IV = key.GetBytes(16);

                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, 0, encryptedData.Length);
                        cs.FlushFinalBlock();
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (CryptographicException)
            {
                MessageBox.Show("Неверный пароль или поврежденные данные");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка дешифровки: {ex.Message}");
                return null;
            }
        }

        private void SaveKeyData(KeyData keyData)
        {
            var serializer = new XmlSerializer(typeof(KeyData));
            Directory.CreateDirectory(Path.GetDirectoryName(_keysFile));

            using (var writer = new StreamWriter(_keysFile))
            {
                serializer.Serialize(writer, keyData);
            }
        }

        private KeyData LoadKeyData()
        {
            if (!File.Exists(_keysFile)) return null;

            try
            {
                var serializer = new XmlSerializer(typeof(KeyData));
                using (var reader = new StreamReader(_keysFile))
                {
                    return (KeyData)serializer.Deserialize(reader);
                }
            }
            catch
            {
                return null;
            }
        }

        [Serializable]
        public class KeyData
        {
            [XmlElement(DataType = "base64Binary")]
            public byte[] EncryptedPublicKey { get; set; }

            [XmlElement(DataType = "base64Binary")]
            public byte[] EncryptedPrivateKey { get; set; }

            public string Salt { get; set; }
        }
    }
}