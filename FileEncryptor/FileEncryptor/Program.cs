using System;
using System.Windows.Forms;

namespace FileEncryptor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Database.Initialize();
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new FileEncryptorForm(loginForm.Username, loginForm.Password));
                }
            }
        }
    }
}