using System.Collections.Generic;
using System.Xml.Serialization;

namespace FileEncryptor
{
    [XmlRoot("EncryptedFilesList")]
    public class BindingListWithXmlSerialization<T> : List<T>
    {
        // Пустой конструктор для сериализации
        public BindingListWithXmlSerialization() : base() { }
        public BindingListWithXmlSerialization(IEnumerable<T> collection) : base(collection) { }
    }
}