using System.Text;

namespace SharedLibrary.Models
{
    public class ReporterFont
    {
        public int StructType { get; set; }
        public int FontSize { get; set; } = 2;
        public int Style { get; set; } = 0;
        public int Weight { get; set; } = 400;
        public int Size { get; set; } = 10;
        public string? StrName { get; set; } = "Verdana";

        public ReporterFont()
        {
        }

        public ReporterFont(byte[] b)
        {
            if (b.Length < 16)
                return;
            this.StructType = BitConverter.ToInt32(b, 0);
            this.FontSize = BitConverter.ToInt32(b, 4);
            this.Style = BitConverter.ToInt32(b, 8);
            this.Weight = BitConverter.ToInt32(b, 12);
            this.Size = BitConverter.ToInt32(b, 16);
            this.StrName = Encoding.Unicode.GetString(b.Skip(20).ToArray());

            if (this.StrName?.Contains("\u0000") ?? false)
            {
                this.StrName = this.StrName.Split("\u0000")[0];
            }
        }

        public ReporterFont(string strByteBase64)
        {
            if (string.IsNullOrEmpty(strByteBase64))
                return;
            byte[] b = Convert.FromBase64String(strByteBase64);
            StructType = BitConverter.ToInt32(b, 0);
            FontSize = BitConverter.ToInt32(b, 4);
            Style = BitConverter.ToInt32(b, 8);
            Weight = BitConverter.ToInt32(b, 12);
            Size = BitConverter.ToInt32(b, 16);
            StrName = Encoding.Unicode.GetString(b.Skip(20).ToArray());

            if (StrName?.Contains("\u0000") ?? false)
            {
                StrName = StrName.Split("\u0000")[0];
            }
        }

        public byte[] GetBytes()
        {
            List<byte> b = new();

            b.AddRange(BitConverter.GetBytes(StructType));
            b.AddRange(BitConverter.GetBytes(FontSize));
            b.AddRange(BitConverter.GetBytes(Style));
            b.AddRange(BitConverter.GetBytes(Weight));
            b.AddRange(BitConverter.GetBytes(Size));
            b.AddRange(Encoding.Unicode.GetBytes(StrName ?? ""));

            return b.ToArray();
        }

        public string GetBase64()
        {
            return Convert.ToBase64String(GetBytes());
        }

    }



}
