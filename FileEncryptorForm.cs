using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;

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
                var result = MessageBox.Show("Не удалось загрузить ключи. Создать новые?",
                                           "Ошибка ключей",
                                           MessageBoxButtons.YesNo);

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

        private void InitializeDataGridView()
        {
            dataGridViewRecords.Columns.Clear();

            dataGridViewRecords.Columns.Add("DateColumn", "Дата шифрования");
            dataGridViewRecords.Columns.Add("OriginalFileColumn", "Исходный файл");

            var linkColumn = new DataGridViewLinkColumn
            {
                HeaderText = "Зашифрованный файл",
                Name = "EncryptedFileColumn"
            };
            dataGridViewRecords.Columns.Add(linkColumn);

            dataGridViewRecords.Columns.Add("FileHashColumn", "SHA-256 файла");
            dataGridViewRecords.Columns.Add("KeyHashColumn", "Хеш ключа");
            dataGridViewRecords.Columns.Add("OwnerColumn", "Владелец ключа");

            UpdateDataGridView();

            dataGridViewRecords.CellContentClick += DataGridViewRecords_CellContentClick;
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

        private void DataGridViewRecords_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridViewRecords.Columns[e.ColumnIndex].Name == "EncryptedFileColumn")
            {
                var path = dataGridViewRecords.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                OpenFileInExplorer(path);
            }
        }

        private void LoadFileRecords()
        {
            try
            {
                if (File.Exists(_recordsFilePath))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<FileRecord>));
                    using (var reader = new StreamReader(_recordsFilePath))
                    {
                        var records = (List<FileRecord>)serializer.Deserialize(reader);
                        _fileRecords.AddRange(records);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveFileRecords()
        {
            try
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<FileRecord>));
                Directory.CreateDirectory(Path.GetDirectoryName(_recordsFilePath));
                using (var writer = new StreamWriter(_recordsFilePath))
                {
                    serializer.Serialize(writer, _fileRecords);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения истории: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenFileInExplorer(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else
                {
                    MessageBox.Show("Файл не найден!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия файла: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private byte[] GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[32];
                rng.GetBytes(salt);
                return salt;
            }
        }

        private void btnSelectPublicKey_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*",
                Title = "Выберите файл с открытым ключом"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtPublicKeyPath.Text = openFileDialog.FileName;
            }
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPublicKeyPath.Text))
            {
                MessageBox.Show("Выберите файл с открытым ключом", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                        MessageBox.Show("Файл успешно зашифрован", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (CryptographicException ex)
                {
                    MessageBox.Show($"Ошибка шифрования: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            if (_privateKeyProvider == null)
            {
                MessageBox.Show("Не удалось загрузить приватный ключ. Проверьте пароль.",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        MessageBox.Show("Файл успешно расшифрован", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (CryptographicException)
                {
                    MessageBox.Show("Не удалось расшифровать файл. Возможно, он был зашифрован другим ключом.",
                                  "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnExportPublicKey_Click(object sender, EventArgs e)
        {
            try
            {
                var publicKeyXml = _keyStorage.GetPublicKeyXml(_password);
                if (string.IsNullOrEmpty(publicKeyXml))
                {
                    MessageBox.Show("Не удалось получить открытый ключ", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "XML файлы (*.xml)|*.xml",
                    FileName = $"{_username}_public_key.xml"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, publicKeyXml);
                    MessageBox.Show("Открытый ключ успешно экспортирован", "Успех",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void FileEncryptorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SaveFileRecords();
            }
            catch
            {
                // Игнорируем ошибки при сохранении при выходе
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