namespace FileEncryptor
{
    partial class FileEncryptorForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileEncryptorForm));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageEncrypt = new System.Windows.Forms.TabPage();
            this.btnExportPublicKey = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtPublicKeyPath = new System.Windows.Forms.TextBox();
            this.btnSelectPublicKey = new System.Windows.Forms.Button();
            this.btnDecrypt = new System.Windows.Forms.Button();
            this.btnEncrypt = new System.Windows.Forms.Button();
            this.tabPageHistory = new System.Windows.Forms.TabPage();
            this.dataGridViewRecords = new System.Windows.Forms.DataGridView();
            this.tabControl.SuspendLayout();
            this.tabPageEncrypt.SuspendLayout();
            this.tabPageHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewRecords)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPageEncrypt);
            this.tabControl.Controls.Add(this.tabPageHistory);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(800, 450);
            this.tabControl.TabIndex = 0;
            // 
            // tabPageEncrypt
            // 
            this.tabPageEncrypt.Controls.Add(this.btnExportPublicKey);
            this.tabPageEncrypt.Controls.Add(this.label1);
            this.tabPageEncrypt.Controls.Add(this.txtPublicKeyPath);
            this.tabPageEncrypt.Controls.Add(this.btnSelectPublicKey);
            this.tabPageEncrypt.Controls.Add(this.btnDecrypt);
            this.tabPageEncrypt.Controls.Add(this.btnEncrypt);
            this.tabPageEncrypt.Location = new System.Drawing.Point(4, 22);
            this.tabPageEncrypt.Name = "tabPageEncrypt";
            this.tabPageEncrypt.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEncrypt.Size = new System.Drawing.Size(792, 424);
            this.tabPageEncrypt.TabIndex = 0;
            this.tabPageEncrypt.Text = "Шифрование";
            this.tabPageEncrypt.UseVisualStyleBackColor = true;
            // 
            // btnExportPublicKey
            // 
            this.btnExportPublicKey.Location = new System.Drawing.Point(20, 120);
            this.btnExportPublicKey.Name = "btnExportPublicKey";
            this.btnExportPublicKey.Size = new System.Drawing.Size(200, 30);
            this.btnExportPublicKey.TabIndex = 5;
            this.btnExportPublicKey.Text = "Экспорт моего открытого ключа";
            this.btnExportPublicKey.UseVisualStyleBackColor = true;
            this.btnExportPublicKey.Click += new System.EventHandler(this.btnExportPublicKey_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(115, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Открытый ключ RSA:";
            // 
            // txtPublicKeyPath
            // 
            this.txtPublicKeyPath.Location = new System.Drawing.Point(20, 40);
            this.txtPublicKeyPath.Name = "txtPublicKeyPath";
            this.txtPublicKeyPath.ReadOnly = true;
            this.txtPublicKeyPath.Size = new System.Drawing.Size(300, 20);
            this.txtPublicKeyPath.TabIndex = 3;
            // 
            // btnSelectPublicKey
            // 
            this.btnSelectPublicKey.Location = new System.Drawing.Point(330, 40);
            this.btnSelectPublicKey.Name = "btnSelectPublicKey";
            this.btnSelectPublicKey.Size = new System.Drawing.Size(30, 20);
            this.btnSelectPublicKey.TabIndex = 2;
            this.btnSelectPublicKey.Text = "...";
            this.btnSelectPublicKey.UseVisualStyleBackColor = true;
            this.btnSelectPublicKey.Click += new System.EventHandler(this.btnSelectPublicKey_Click);
            // 
            // btnDecrypt
            // 
            this.btnDecrypt.Location = new System.Drawing.Point(20, 80);
            this.btnDecrypt.Name = "btnDecrypt";
            this.btnDecrypt.Size = new System.Drawing.Size(200, 30);
            this.btnDecrypt.TabIndex = 1;
            this.btnDecrypt.Text = "Расшифровать файл";
            this.btnDecrypt.UseVisualStyleBackColor = true;
            this.btnDecrypt.Click += new System.EventHandler(this.btnDecrypt_Click);
            // 
            // btnEncrypt
            // 
            this.btnEncrypt.Location = new System.Drawing.Point(230, 80);
            this.btnEncrypt.Name = "btnEncrypt";
            this.btnEncrypt.Size = new System.Drawing.Size(200, 30);
            this.btnEncrypt.TabIndex = 0;
            this.btnEncrypt.Text = "Зашифровать файл";
            this.btnEncrypt.UseVisualStyleBackColor = true;
            this.btnEncrypt.Click += new System.EventHandler(this.btnEncrypt_Click);
            // 
            // tabPageHistory
            // 
            this.tabPageHistory.Controls.Add(this.dataGridViewRecords);
            this.tabPageHistory.Location = new System.Drawing.Point(4, 22);
            this.tabPageHistory.Name = "tabPageHistory";
            this.tabPageHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHistory.Size = new System.Drawing.Size(792, 424);
            this.tabPageHistory.TabIndex = 1;
            this.tabPageHistory.Text = "История";
            this.tabPageHistory.UseVisualStyleBackColor = true;
            // 
            // dataGridViewRecords
            // 
            this.dataGridViewRecords.AllowUserToAddRows = false;
            this.dataGridViewRecords.AllowUserToDeleteRows = false;
            this.dataGridViewRecords.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewRecords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewRecords.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewRecords.Name = "dataGridViewRecords";
            this.dataGridViewRecords.ReadOnly = true;
            this.dataGridViewRecords.Size = new System.Drawing.Size(786, 418);
            this.dataGridViewRecords.TabIndex = 0;
            // 
            // FileEncryptorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tabControl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FileEncryptorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FileEncryptor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FileEncryptorForm_FormClosing);
            this.tabControl.ResumeLayout(false);
            this.tabPageEncrypt.ResumeLayout(false);
            this.tabPageEncrypt.PerformLayout();
            this.tabPageHistory.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewRecords)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageEncrypt;
        private System.Windows.Forms.TabPage tabPageHistory;
        private System.Windows.Forms.DataGridView dataGridViewRecords;
        private System.Windows.Forms.Button btnEncrypt;
        private System.Windows.Forms.Button btnDecrypt;
        private System.Windows.Forms.Button btnSelectPublicKey;
        private System.Windows.Forms.TextBox txtPublicKeyPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnExportPublicKey;
    }
}
