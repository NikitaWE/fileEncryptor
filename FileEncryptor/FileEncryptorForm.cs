//using System;
//using System.Collections.Generic;
//using System.Data.SQLite;
//using System.Diagnostics;
//using System.IO;
//using System.Security.Cryptography;
//using System.Text;
//using System.Windows.Forms;

//namespace FileEncryptor
//{
//    public partial class FileEncryptorForm : Form
//    {
//        private readonly string _username;
//        private readonly string _password;
//        private readonly UserKeyStorage _keyStorage;
//        private RSACryptoServiceProvider _privateKeyProvider;
//        private readonly List<FileRecord> _fileRecords = new List<FileRecord>();

//        public FileEncryptorForm(string username, string password)
//        {
//            InitializeComponent();
//            _username = username;
//            _password = password;
//            _keyStorage = new UserKeyStorage(username);

//            LoadFileRecords();
//            InitializeDataGridView();

//            if (!_keyStorage.HasKeys())
//            {
//                var salt = GenerateSalt();
//                _keyStorage.GenerateAndStoreKeys(password, salt);
//            }

//            _privateKeyProvider = _keyStorage.GetPrivateKeyProvider(password);
//            if (_privateKeyProvider == null)
//            {
//                var result = MessageBox.Show("Не удалось загрузить ключи. Создать новые?", "Ошибка ключей", MessageBoxButtons.YesNo);
//                if (result == DialogResult.Yes)
//                {
//                    var salt = GenerateSalt();
//                    _keyStorage.GenerateAndStoreKeys(password, salt);
//                    _privateKeyProvider = _keyStorage.GetPrivateKeyProvider(password);
//                }
//                else
//                {
//                    Close();
//                }
//            }
//        }

//        private byte[] GenerateSalt()
//        {
//            var salt = new byte[32];
//            using (var rng = RandomNumberGenerator.Create())
//            {
//                rng.GetBytes(salt);
//            }
//            return salt;
//        }

//        private string CalculateSHA256(byte[] data)
//        {
//            using (var sha256 = SHA256.Create())
//            {
//                var hash = sha256.ComputeHash(data);
//                return BitConverter.ToString(hash).Replace("-", "").ToLower().Substring(0, 16) + "...";
//            }
//        }

//        private void LoadFileRecords()
//        {
//            _fileRecords.Clear();
//            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
//            {
//                conn.Open();
//                using (var cmd = new SQLiteCommand("SELECT * FROM FileRecords WHERE Username = @u", conn))
//                {
//                    cmd.Parameters.AddWithValue("@u", _username);
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            _fileRecords.Add(new FileRecord
//                            {
//                                EncryptDate = DateTime.Parse(reader["EncryptDate"].ToString()),
//                                OriginalName = reader["OriginalName"].ToString(),
//                                EncryptedPath = reader["EncryptedPath"].ToString(),
//                                HashOfFile = reader["HashOfFile"].ToString(),
//                                HashOfKey = reader["HashOfKey"].ToString(),
//                                IsOwnedKey = Convert.ToInt32(reader["IsOwnedKey"]) == 1
//                            });
//                        }
//                    }
//                }
//            }
//        }

//        private void SaveFileRecord(FileRecord record)
//        {
//            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
//            {
//                conn.Open();
//                using (var cmd = new SQLiteCommand(conn))
//                {
//                    cmd.CommandText = @"INSERT INTO FileRecords (Username, EncryptDate, OriginalName, EncryptedPath, HashOfFile, HashOfKey, IsOwnedKey) VALUES (@u, @d, @o, @e, @f, @k, @own)";
//                    cmd.Parameters.AddWithValue("@u", _username);
//                    cmd.Parameters.AddWithValue("@d", record.EncryptDate.ToString("yyyy-MM-dd HH:mm:ss"));
//                    cmd.Parameters.AddWithValue("@o", record.OriginalName);
//                    cmd.Parameters.AddWithValue("@e", record.EncryptedPath);
//                    cmd.Parameters.AddWithValue("@f", record.HashOfFile);
//                    cmd.Parameters.AddWithValue("@k", record.HashOfKey);
//                    cmd.Parameters.AddWithValue("@own", record.IsOwnedKey ? 1 : 0);
//                    cmd.ExecuteNonQuery();
//                }
//            }
//        }

//        private void InitializeDataGridView()
//        {
//            dataGridViewRecords.Columns.Clear();
//            dataGridViewRecords.Columns.Add("DateColumn", "Дата шифрования");
//            dataGridViewRecords.Columns.Add("OriginalFileColumn", "Исходный файл");
//            var linkColumn = new DataGridViewLinkColumn { HeaderText = "Зашифрованный файл", Name = "EncryptedFileColumn" };
//            dataGridViewRecords.Columns.Add(linkColumn);
//            dataGridViewRecords.Columns.Add("FileHashColumn", "SHA-256 файла");
//            dataGridViewRecords.Columns.Add("KeyHashColumn", "Хеш ключа");
//            dataGridViewRecords.Columns.Add("OwnerColumn", "Владелец ключа");

//            UpdateDataGridView();
//            dataGridViewRecords.CellContentClick += DataGridViewRecords_CellContentClick;
//        }

//        private void UpdateDataGridView()
//        {
//            dataGridViewRecords.Rows.Clear();
//            foreach (var record in _fileRecords)
//            {
//                dataGridViewRecords.Rows.Add(
//                    record.EncryptDate.ToString("yyyy-MM-dd HH:mm"),
//                    record.OriginalName,
//                    record.EncryptedPath,
//                    record.HashOfFile,
//                    record.HashOfKey,
//                    record.IsOwnedKey ? "Мой" : "Сторонний"
//                );
//            }
//        }

//        private void DataGridViewRecords_CellContentClick(object sender, DataGridViewCellEventArgs e)
//        {
//            if (e.RowIndex >= 0 && dataGridViewRecords.Columns[e.ColumnIndex].Name == "EncryptedFileColumn")
//            {
//                var path = dataGridViewRecords.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
//                if (File.Exists(path))
//                    Process.Start("explorer.exe", $"/select,\"{path}\"");
//                else
//                    MessageBox.Show("Файл не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//            }
//        }

//        private void FileEncryptorForm_FormClosing(object sender, FormClosingEventArgs e)
//        {
//            // Здесь можно добавить сохранение состояния или очистку ресурсов
//        }

//        private void btnExportPublicKey_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                var publicKeyXml = _keyStorage.GetPublicKeyXml(_password);
//                if (string.IsNullOrEmpty(publicKeyXml))
//                {
//                    MessageBox.Show("Не удалось получить открытый ключ", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                    return;
//                }

//                var saveDialog = new SaveFileDialog
//                {
//                    Filter = "XML файлы (*.xml)|*.xml",
//                    FileName = $"{_username}_public_key.xml"
//                };

//                if (saveDialog.ShowDialog() == DialogResult.OK)
//                {
//                    File.WriteAllText(saveDialog.FileName, publicKeyXml);
//                    MessageBox.Show("Открытый ключ успешно экспортирован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private void btnSelectPublicKey_Click(object sender, EventArgs e)
//        {
//            var openFileDialog = new OpenFileDialog
//            {
//                Filter = "XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*",
//                Title = "Выберите файл с открытым ключом"
//            };

//            if (openFileDialog.ShowDialog() == DialogResult.OK)
//            {
//                txtPublicKeyPath.Text = openFileDialog.FileName;
//            }
//        }

//        private void btnEncrypt_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                string publicKeyXml = null;

//                //if (!string.IsNullOrEmpty(txtPublicKeyPath.Text) && File.Exists(txtPublicKeyPath.Text))
//                //{
//                //    publicKeyXml = File.ReadAllText(txtPublicKeyPath.Text);
//                //}
//                //else
//                //{
//                    using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
//                    {
//                        conn.Open();
//                        using (var cmd = new SQLiteCommand("SELECT PublicKey FROM Users WHERE Username = @u", conn))
//                        {
//                            cmd.Parameters.AddWithValue("@u", _username);
//                            publicKeyXml = cmd.ExecuteScalar()?.ToString();
//                        }
//                    }
//                //}

//                if (string.IsNullOrEmpty(publicKeyXml))
//                {
//                    MessageBox.Show("Публичный ключ не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                    return;
//                }

//                var openFileDialog = new OpenFileDialog();
//                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

//                var rsa = new RSACryptoServiceProvider();
//                rsa.FromXmlString(publicKeyXml);

//                byte[] fileBytes = File.ReadAllBytes(openFileDialog.FileName);

//                using (var aes = Aes.Create())
//                {
//                    aes.KeySize = 256;
//                    aes.GenerateKey();
//                    aes.GenerateIV();

//                    byte[] encryptedContent;
//                    using (var ms = new MemoryStream())
//                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
//                    {
//                        cs.Write(fileBytes, 0, fileBytes.Length);
//                        cs.FlushFinalBlock();
//                        encryptedContent = ms.ToArray();
//                    }

//                    var encryptedKey = rsa.Encrypt(aes.Key, false);
//                    var encryptedIv = rsa.Encrypt(aes.IV, false);

//                    var saveDialog = new SaveFileDialog
//                    {
//                        Filter = "Зашифрованные файлы (*.enc)|*.enc",
//                        FileName = Path.GetFileName(openFileDialog.FileName) + ".enc"
//                    };
//                    if (saveDialog.ShowDialog() != DialogResult.OK) return;

//                    using (var writer = new BinaryWriter(File.Create(saveDialog.FileName)))
//                    {
//                        writer.Write(encryptedKey.Length);
//                        writer.Write(encryptedKey);
//                        writer.Write(encryptedIv.Length);
//                        writer.Write(encryptedIv);
//                        writer.Write(encryptedContent.Length);
//                        writer.Write(encryptedContent);
//                    }

//                    var record = new FileRecord
//                    {
//                        EncryptDate = DateTime.Now,
//                        OriginalName = Path.GetFileName(openFileDialog.FileName),
//                        EncryptedPath = saveDialog.FileName,
//                        HashOfFile = CalculateSHA256(encryptedContent),
//                        HashOfKey = CalculateSHA256(Encoding.UTF8.GetBytes(publicKeyXml)),
//                        IsOwnedKey = publicKeyXml == _keyStorage.GetPublicKeyXml(_password)
//                    };

//                    _fileRecords.Add(record);
//                    SaveFileRecord(record);
//                    UpdateDataGridView();

//                    MessageBox.Show("Файл успешно зашифрован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Ошибка при шифровании: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private void btnDecrypt_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                if (_privateKeyProvider == null)
//                {
//                    MessageBox.Show("Приватный ключ не загружен", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                    return;
//                }

//                var openFileDialog = new OpenFileDialog
//                {
//                    Filter = "Зашифрованные файлы (*.enc)|*.enc",
//                    Title = "Выберите зашифрованный файл"
//                };

//                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

//                byte[] encryptedData = File.ReadAllBytes(openFileDialog.FileName);
//                using (var reader = new BinaryReader(new MemoryStream(encryptedData)))
//                {
//                    int keyLen = reader.ReadInt32();
//                    byte[] encryptedKey = reader.ReadBytes(keyLen);

//                    int ivLen = reader.ReadInt32();
//                    byte[] encryptedIv = reader.ReadBytes(ivLen);

//                    int contentLen = reader.ReadInt32();
//                    byte[] encryptedContent = reader.ReadBytes(contentLen);

//                    // Пытаемся расшифровать симметричный ключ и IV с помощью приватного ключа
//                    byte[] aesKey = _privateKeyProvider.Decrypt(encryptedKey, false);
//                    byte[] aesIv = _privateKeyProvider.Decrypt(encryptedIv, false);

//                    using (var aes = Aes.Create())
//                    {
//                        aes.Key = aesKey;
//                        aes.IV = aesIv;

//                        using (var ms = new MemoryStream())
//                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
//                        {
//                            cs.Write(encryptedContent, 0, encryptedContent.Length);
//                            cs.FlushFinalBlock();

//                            var decryptedBytes = ms.ToArray();

//                            var saveDialog = new SaveFileDialog
//                            {
//                                Title = "Сохранить расшифрованный файл",
//                                FileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName)
//                            };

//                            if (saveDialog.ShowDialog() == DialogResult.OK)
//                            {
//                                File.WriteAllBytes(saveDialog.FileName, decryptedBytes);
//                                MessageBox.Show("Файл успешно расшифрован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                            }
//                        }
//                    }
//                }
//            }
//            catch (CryptographicException)
//            {
//                MessageBox.Show("Не удалось расшифровать файл. Возможно, он был зашифрован другим ключом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Ошибка при расшифровке: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }
//    }

//    [Serializable]
//    public class FileRecord
//    {
//        public DateTime EncryptDate { get; set; }
//        public string OriginalName { get; set; }
//        public string EncryptedPath { get; set; }
//        public string HashOfFile { get; set; }
//        public string HashOfKey { get; set; }
//        public bool IsOwnedKey { get; set; }
//    }
//}





using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace FileEncryptor
{
    public partial class FileEncryptorForm : Form
    {
        private readonly string _username;
        private readonly string _password;
        private readonly UserKeyStorage _keyStorage;
        private RSACryptoServiceProvider _privateKeyProvider;
        private readonly List<FileRecord> _fileRecords = new List<FileRecord>();

        public FileEncryptorForm(string username, string password)
        {
            InitializeComponent();
            _username = username;
            _password = password;
            _keyStorage = new UserKeyStorage(username);

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

        private byte[] GenerateSalt()
        {
            var salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private string CalculateSHA256(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLower().Substring(0, 16) + "...";
            }
        }

        private void LoadFileRecords()
        {
            _fileRecords.Clear();
            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM FileRecords WHERE Username = @u", conn))
                {
                    cmd.Parameters.AddWithValue("@u", _username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _fileRecords.Add(new FileRecord
                            {
                                EncryptDate = DateTime.Parse(reader["EncryptDate"].ToString()),
                                OriginalName = reader["OriginalName"].ToString(),
                                EncryptedPath = reader["EncryptedPath"].ToString(),
                                HashOfFile = reader["HashOfFile"].ToString(),
                                HashOfKey = reader["HashOfKey"].ToString(),
                                IsOwnedKey = Convert.ToInt32(reader["IsOwnedKey"]) == 1
                            });
                        }
                    }
                }
            }
        }

        private void SaveFileRecord(FileRecord record)
        {
            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = @"INSERT INTO FileRecords (Username, EncryptDate, OriginalName, EncryptedPath, HashOfFile, HashOfKey, IsOwnedKey) VALUES (@u, @d, @o, @e, @f, @k, @own)";
                    cmd.Parameters.AddWithValue("@u", _username);
                    cmd.Parameters.AddWithValue("@d", record.EncryptDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@o", record.OriginalName);
                    cmd.Parameters.AddWithValue("@e", record.EncryptedPath);
                    cmd.Parameters.AddWithValue("@f", record.HashOfFile);
                    cmd.Parameters.AddWithValue("@k", record.HashOfKey);
                    cmd.Parameters.AddWithValue("@own", record.IsOwnedKey ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InitializeDataGridView()
        {
            dataGridViewRecords.Columns.Clear();
            dataGridViewRecords.Columns.Add("DateColumn", "Дата шифрования");
            dataGridViewRecords.Columns.Add("OriginalFileColumn", "Исходный файл");
            var linkColumn = new DataGridViewLinkColumn { HeaderText = "Зашифрованный файл", Name = "EncryptedFileColumn" };
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
                if (File.Exists(path))
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                else
                    MessageBox.Show("Файл не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void FileEncryptorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Здесь можно добавить сохранение состояния или очистку ресурсов
        }

        private void btnExportPublicKey_Click(object sender, EventArgs e)
        {
            try
            {
                var publicKeyXml = _keyStorage.GetPublicKeyXml(_password);
                if (string.IsNullOrEmpty(publicKeyXml))
                {
                    MessageBox.Show("Не удалось получить открытый ключ", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show("Открытый ключ успешно экспортирован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            try
            {
                string publicKeyXml = null;

                // Если указан путь к открытому ключу вручную — используем его
                if (!string.IsNullOrEmpty(txtPublicKeyPath.Text) && File.Exists(txtPublicKeyPath.Text))
                {
                    publicKeyXml = File.ReadAllText(txtPublicKeyPath.Text);
                }
                else
                {
                    // Иначе — берём ключ из базы данных текущего пользователя
                    using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand("SELECT PublicKey FROM Users WHERE Username = @u", conn))
                        {
                            cmd.Parameters.AddWithValue("@u", _username);
                            publicKeyXml = cmd.ExecuteScalar()?.ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(publicKeyXml))
                {
                    MessageBox.Show("Публичный ключ не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKeyXml);

                byte[] fileBytes = File.ReadAllBytes(openFileDialog.FileName);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.GenerateKey();
                    aes.GenerateIV();

                    byte[] encryptedContent;
                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(fileBytes, 0, fileBytes.Length);
                        cs.FlushFinalBlock();
                        encryptedContent = ms.ToArray();
                    }

                    var encryptedKey = rsa.Encrypt(aes.Key, false);
                    var encryptedIv = rsa.Encrypt(aes.IV, false);

                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "Зашифрованные файлы (*.enc)|*.enc",
                        FileName = Path.GetFileName(openFileDialog.FileName) + ".enc"
                    };
                    if (saveDialog.ShowDialog() != DialogResult.OK) return;

                    using (var writer = new BinaryWriter(File.Create(saveDialog.FileName)))
                    {
                        writer.Write(encryptedKey.Length);
                        writer.Write(encryptedKey);
                        writer.Write(encryptedIv.Length);
                        writer.Write(encryptedIv);
                        writer.Write(encryptedContent.Length);
                        writer.Write(encryptedContent);
                    }

                    var record = new FileRecord
                    {
                        EncryptDate = DateTime.Now,
                        OriginalName = Path.GetFileName(openFileDialog.FileName),
                        EncryptedPath = saveDialog.FileName,
                        HashOfFile = CalculateSHA256(encryptedContent),
                        HashOfKey = CalculateSHA256(Encoding.UTF8.GetBytes(publicKeyXml)),
                        IsOwnedKey = publicKeyXml == _keyStorage.GetPublicKeyXml(_password)
                    };

                    _fileRecords.Add(record);
                    SaveFileRecord(record);
                    UpdateDataGridView();

                    MessageBox.Show("Файл успешно зашифрован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при шифровании: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            try
            {
                if (_privateKeyProvider == null)
                {
                    MessageBox.Show("Приватный ключ не загружен", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Зашифрованные файлы (*.enc)|*.enc",
                    Title = "Выберите зашифрованный файл"
                };

                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                byte[] encryptedData = File.ReadAllBytes(openFileDialog.FileName);
                using (var reader = new BinaryReader(new MemoryStream(encryptedData)))
                {
                    int keyLen = reader.ReadInt32();
                    byte[] encryptedKey = reader.ReadBytes(keyLen);

                    int ivLen = reader.ReadInt32();
                    byte[] encryptedIv = reader.ReadBytes(ivLen);

                    int contentLen = reader.ReadInt32();
                    byte[] encryptedContent = reader.ReadBytes(contentLen);

                    // Пытаемся расшифровать симметричный ключ и IV с помощью приватного ключа
                    byte[] aesKey = _privateKeyProvider.Decrypt(encryptedKey, false);
                    byte[] aesIv = _privateKeyProvider.Decrypt(encryptedIv, false);

                    using (var aes = Aes.Create())
                    {
                        aes.Key = aesKey;
                        aes.IV = aesIv;

                        using (var ms = new MemoryStream())
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedContent, 0, encryptedContent.Length);
                            cs.FlushFinalBlock();

                            var decryptedBytes = ms.ToArray();

                            var saveDialog = new SaveFileDialog
                            {
                                Title = "Сохранить расшифрованный файл",
                                FileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName)
                            };

                            if (saveDialog.ShowDialog() == DialogResult.OK)
                            {
                                File.WriteAllBytes(saveDialog.FileName, decryptedBytes);
                                MessageBox.Show("Файл успешно расшифрован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
                MessageBox.Show("Не удалось расшифровать файл. Возможно, он был зашифрован другим ключом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при расшифровке: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
