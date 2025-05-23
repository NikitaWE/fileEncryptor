using System.Collections.Generic;
using System.Xml.Serialization;

namespace FileEncryptor
{
    [XmlRoot("EncryptedFilesList")]
    public class BindingListWithXmlSerialization<T> : List<T>
    {
        public BindingListWithXmlSerialization() : base() { }

        public BindingListWithXmlSerialization(IEnumerable<T> collection) : base(collection) { }
    }
}
