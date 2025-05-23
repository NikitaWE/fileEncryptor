using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace FileEncryptor
{
    public partial class LoginForm : Form
    {
        private const string UsersFile = "users.dat";
        private const string DataFolder = "UserData";
        private bool _isLoggedIn = false;
        public bool IsExiting { get; private set; } = false;
        public string Username { get; private set; }
        public string Password { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
            CheckDataDirectory();
        }

        private void CheckDataDirectory()
        {
            try
            {
                if (!Directory.Exists(DataFolder))
                {
                    Directory.CreateDirectory(DataFolder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании директории данных: {ex.Message}",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Введите имя пользователя", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Введите пароль", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Focus();
                    return;
                }

                if (txtPassword.Text.Length < 6)
                {
                    MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Focus();
                    return;
                }

                if (txtPassword.Text != txtConfirmPassword.Text)
                {
                    MessageBox.Show("Пароли не совпадают", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtConfirmPassword.Focus();
                    return;
                }

                RegisterUser(txtUsername.Text.Trim(), txtPassword.Text);

                txtUsername.Clear();
                txtPassword.Clear();
                txtConfirmPassword.Clear();

                MessageBox.Show("Регистрация прошла успешно! Теперь вы можете войти.",
                              "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Некорректные данные",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Пользователь существует",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка регистрации",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError(ex);
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (_isLoggedIn) return;

            try
            {
                _isLoggedIn = true;
                this.Cursor = Cursors.WaitCursor;

                if (ValidateUser(txtUsername.Text, txtPassword.Text))
                {
                    Username = txtUsername.Text;
                    Password = txtPassword.Text;

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    _isLoggedIn = false;
                    this.Cursor = Cursors.Default;
                    MessageBox.Show("Неверный логин или пароль", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (ObjectDisposedException)
            {
                Application.Exit();
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                _isLoggedIn = false;
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Критическая ошибка: {ex.Message}\nПриложение будет закрыто.",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                Environment.Exit(1);
            }
            finally
            {
                if (!_isLoggedIn)
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void LogError(Exception ex)
        {
            try
            {
                string logMessage = $"[{DateTime.Now}] Ошибка: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText("error_log.txt", logMessage);
            }
            catch { /* Игнорируем ошибки логирования */ }
        }

        private bool UserExists(string username)
        {
            try
            {
                if (!File.Exists(UsersFile)) return false;

                foreach (var line in File.ReadAllLines(UsersFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split('|');
                    if (parts.Length > 0 && parts[0].Equals(username, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return true; // В случае ошибки считаем что пользователь существует
            }
        }

        private void RegisterUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 4)
                throw new ArgumentException("Имя пользователя должно содержать минимум 4 символа");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ArgumentException("Пароль должен содержать минимум 6 символов");

            if (username.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Имя пользователя содержит недопустимые символы");

            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }

            var salt = GenerateSalt();
            var hashedPassword = HashPassword(password, salt);
            var userRecord = $"{username}|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(hashedPassword)}";

            var tempFile = Path.GetTempFileName();
            bool fileExists = File.Exists(UsersFile);

            try
            {
                if (fileExists)
                {
                    var existingLines = File.ReadAllLines(UsersFile);
                    foreach (var line in existingLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var parts = line.Split('|');
                            if (parts.Length > 0 && parts[0].Equals(username, StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException("Пользователь с таким именем уже зарегистрирован");
                            }
                        }
                    }
                    File.WriteAllLines(tempFile, existingLines);
                }

                File.AppendAllText(tempFile, userRecord + Environment.NewLine);

                var userDir = Path.Combine(DataFolder, username);
                Directory.CreateDirectory(userDir);

                if (fileExists)
                {
                    File.Replace(tempFile, UsersFile, null);
                }
                else
                {
                    File.Move(tempFile, UsersFile);
                }
            }
            catch (IOException ex)
            {
                throw new ApplicationException($"Ошибка записи данных: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException($"Нет прав доступа: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Ошибка регистрации: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }

        private bool ValidateUser(string username, string password)
        {
            try
            {
                if (!File.Exists(UsersFile)) return false;

                foreach (var line in File.ReadAllLines(UsersFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split('|');
                    if (parts.Length >= 3 && parts[0] == username)
                    {
                        var salt = Convert.FromBase64String(parts[1]);
                        var storedHash = Convert.FromBase64String(parts[2]);
                        var computedHash = HashPassword(password, salt);

                        return CompareByteArrays(storedHash, computedHash);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
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

        private byte[] HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);
            }
        }

        private bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_isLoggedIn)
            {
                var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение",
                                           MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    Application.Exit();
                    Environment.Exit(0);
                }
            }
        }
    }
}