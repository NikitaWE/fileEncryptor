﻿using System;
using System.Windows.Forms;

namespace FileEncryptor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Инициализация SQLite-базы данных
            DatabaseHelper.InitializeDatabase();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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
