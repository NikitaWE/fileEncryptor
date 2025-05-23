using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace FileEncryptor
{
    public partial class LoginForm : Form
    {
        private bool _isLoggedIn = false;
        public bool IsExiting { get; private set; } = false;
        public string Username { get; private set; }
        public string Password { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text) || txtUsername.Text.Length < 4)
                    throw new ArgumentException("Имя пользователя должно содержать минимум 4 символа");

                if (string.IsNullOrWhiteSpace(txtPassword.Text) || txtPassword.Text.Length < 6)
                    throw new ArgumentException("Пароль должен содержать минимум 6 символов");

                if (txtPassword.Text != txtConfirmPassword.Text)
                    throw new ArgumentException("Пароли не совпадают");

                RegisterUser(txtUsername.Text.Trim(), txtPassword.Text);

                txtUsername.Clear();
                txtPassword.Clear();
                txtConfirmPassword.Clear();

                MessageBox.Show("Регистрация прошла успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка регистрации", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show("Неверный логин или пароль", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _isLoggedIn = false;
                this.Cursor = Cursors.Default;
                MessageBox.Show("Критическая ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                Environment.Exit(1);
            }
        }

        private void RegisterUser(string username, string password)
        {
            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();

                using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users WHERE Username = @u", conn))
                {
                    checkCmd.Parameters.AddWithValue("@u", username);
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                        throw new InvalidOperationException("Пользователь уже существует");
                }

                var salt = GenerateSalt();
                var hash = HashPassword(password, salt);

                var rsa = new RSACryptoServiceProvider(2048);
                var privateKeyXml = rsa.ToXmlString(true);
                var publicKeyXml = rsa.ToXmlString(false);

                var keyStorage = new UserKeyStorage(username);
                keyStorage.StoreKeys(privateKeyXml, password, salt);

                using (var insertCmd = new SQLiteCommand("INSERT INTO Users (Username, Salt, PasswordHash, PublicKey) VALUES (@u, @s, @h, @p)", conn))
                {
                    insertCmd.Parameters.AddWithValue("@u", username);
                    insertCmd.Parameters.AddWithValue("@s", Convert.ToBase64String(salt));
                    insertCmd.Parameters.AddWithValue("@h", Convert.ToBase64String(hash));
                    insertCmd.Parameters.AddWithValue("@p", publicKeyXml);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        private bool ValidateUser(string username, string password)
        {
            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand("SELECT Salt, PasswordHash FROM Users WHERE Username = @u", conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var salt = Convert.FromBase64String(reader.GetString(0));
                            var storedHash = Convert.FromBase64String(reader.GetString(1));
                            var computedHash = HashPassword(password, salt);

                            return CompareByteArrays(storedHash, computedHash);
                        }
                    }
                }
            }
            return false;
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

        private byte[] HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);
            }
        }

        private bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_isLoggedIn)
            {
                var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                    e.Cancel = true;
                else
                {
                    Application.Exit();
                    Environment.Exit(0);
                }
            }
        }
    }
}
