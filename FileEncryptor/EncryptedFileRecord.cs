using System;

namespace FileEncryptor
{
    [Serializable]
    public class EncryptedFileRecord
    {
        public DateTime EncryptionDate { get; set; }
        public string OriginalFileName { get; set; }
        public string EncryptedFilePath { get; set; }
        public string FileHash { get; set; }
        public string PublicKeyHash { get; set; }
        public bool IsOwnKey { get; set; }

        // Для отображения в таблице
        public string KeyOwnerDisplay => IsOwnKey ? "Мой" : "Сторонний";
    }
}