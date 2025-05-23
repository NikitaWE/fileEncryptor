using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.SQLite;

namespace FileEncryptor
{
    public partial class FileEncryptorForm : Form
    {
        private readonly string _username;
        private readonly UserKeyStorage _keyStorage;
        private RSACryptoServiceProvider _privateKeyProvider;
        private readonly List<FileRecord> _fileRecords;
        private readonly string _recordsFilePath;
        private readonly string _password;

        public FileEncryptorForm(string username, string password)
        {
            InitializeComponent();
            _username = username;
            _password = password;
            _keyStorage = new UserKeyStorage(username);
            _recordsFilePath = Path.Combine("UserData", username, "encrypted_records.xml");

            _fileRecords = new List<FileRecord>();
            LoadFileRecords();
            InitializeDataGridView();

            if (!_keyStorage.HasKeys())
            {
                var salt = GenerateSalt();
                _keyStorage.GenerateAndStoreKeys(password, salt);
            }

            _privateKeyProvider = _keyStorage.GetPrivateKeyProvider(password);

            if (_privateKeyProvider == null)
            {
                var result = MessageBox.Show("Не удалось загрузить ключи. Создать новые?", "Ошибка ключей", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    var salt = GenerateSalt();
                    _keyStorage.GenerateAndStoreKeys(password, salt);
                    _privateKeyProvider = _keyStorage.GetPrivateKeyProvider(password);
                }
                else
                {
                    Close();
                }
            }
        }

        private int GetUserId(string username)
        {
            using var conn = new SQLiteConnection("Data Source=Encryptor.db;");
            conn.Open();

            var cmd = new SQLiteCommand("SELECT Id FROM Users WHERE Username = @u", conn);
            cmd.Parameters.AddWithValue("@u", username);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPublicKeyPath.Text))
            {
                MessageBox.Show("Выберите файл с открытым ключом", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var publicKeyXml = File.ReadAllText(txtPublicKeyPath.Text);
                    var publicKeyProvider = new RSACryptoServiceProvider(2048);
                    publicKeyProvider.FromXmlString(publicKeyXml);

                    var fileData = File.ReadAllBytes(openFileDialog.FileName);
                    int maxLength = publicKeyProvider.KeySize / 8 - 42;
                    if (fileData.Length > maxLength)
                    {
                        throw new Exception($"Файл слишком большой. Максимальный размер: {maxLength} байт");
                    }

                    var encryptedData = publicKeyProvider.Encrypt(fileData, false);

                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "Зашифрованные файлы (*.enc)|*.enc",
                        FileName = Path.GetFileName(openFileDialog.FileName) + ".enc"
                    };

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(saveDialog.FileName, encryptedData);

                        var record = new FileRecord
                        {
                            EncryptDate = DateTime.Now,
                            OriginalName = Path.GetFileName(openFileDialog.FileName),
                            EncryptedPath = saveDialog.FileName,
                            HashOfFile = CalculateSHA256(encryptedData),
                            HashOfKey = CalculateSHA256(Encoding.UTF8.GetBytes(publicKeyXml)),
                            IsOwnedKey = publicKeyXml == _keyStorage.GetPublicKeyXml(_password)
                        };

                        _fileRecords.Add(record);
                        UpdateDataGridView();
                        SaveFileRecords();

                        int userId = GetUserId(_username);
                        if (userId != -1)
                        {
                            FileLogRepository.LogAction(userId, record.OriginalName, record.HashOfFile, "encrypt");
                        }

                        MessageBox.Show("Файл успешно зашифрован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            if (_privateKeyProvider == null)
            {
                MessageBox.Show("Не удалось загрузить приватный ключ. Проверьте пароль.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Зашифрованные файлы (*.enc)|*.enc"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var encryptedData = File.ReadAllBytes(openFileDialog.FileName);
                    var decryptedData = _privateKeyProvider.Decrypt(encryptedData, false);

                    var saveDialog = new SaveFileDialog
                    {
                        FileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName)
                    };

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(saveDialog.FileName, decryptedData);

                        int userId = GetUserId(_username);
                        if (userId != -1)
                        {
                            FileLogRepository.LogAction(userId, Path.GetFileName(openFileDialog.FileName), CalculateSHA256(decryptedData), "decrypt");
                        }

                        MessageBox.Show("Файл успешно расшифрован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateDataGridView()
        {
            dataGridViewRecords.Rows.Clear();
            foreach (var record in _fileRecords)
            {
                dataGridViewRecords.Rows.Add(
                    record.EncryptDate.ToString("yyyy-MM-dd HH:mm"),
                    record.OriginalName,
                    record.EncryptedPath,
                    record.HashOfFile,
                    record.HashOfKey,
                    record.IsOwnedKey ? "Мой" : "Сторонний"
                );
            }
        }

        private string CalculateSHA256(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLower().Substring(0, 16) + "...";
            }
        }
    }

    [Serializable]
    public class FileRecord
    {
        public DateTime EncryptDate { get; set; }
        public string OriginalName { get; set; }
        public string EncryptedPath { get; set; }
        public string HashOfFile { get; set; }
        public string HashOfKey { get; set; }
        public bool IsOwnedKey { get; set; }
    }
}
