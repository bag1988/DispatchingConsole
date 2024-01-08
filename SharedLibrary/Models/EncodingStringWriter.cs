using System.Text;

namespace SharedLibrary.Models
{
    public class EncodingStringWriter : StringWriter
    {
        private readonly Encoding _encoding;
        public EncodingStringWriter(Encoding e)
        {
            _encoding = e;
        }
        public override Encoding Encoding => _encoding;
    }
}
