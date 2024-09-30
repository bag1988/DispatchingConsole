using SharedLibrary;

namespace BlazorLibrary.Models
{
    public class SndSetting
    {
        public UInt32 SBIndex { get; set; }//byte[4]
        public UInt16 SndLevel { get; set; } = 100;//byte[2] uint16
        public UInt16 SndSource { get; set; }//byte[2] uint16
        public WavOnlyHeaderModel? SndFormat { get; set; } = new(); //byte[18]
        public string? Interfece { get; set; }

        public SndSetting(byte[] b)
        {
            SBIndex = BitConverter.ToUInt32(b, 0);
            SndLevel = b.Length > 4 ? BitConverter.ToUInt16(b, 4) : (UInt16)0;
            SndSource = b.Length > 6 ? BitConverter.ToUInt16(b, 6) : (UInt16)0;
            SndFormat = b.Length >= 8 ? new WavOnlyHeaderModel(b.Skip(8).ToArray()) : new();
        }
        public SndSetting()
        {

        }
        public byte[] ToBytes()
        {
            List<byte> list =
            [
                .. BitConverter.GetBytes((System.UInt32)SBIndex).ToList(),
                .. BitConverter.GetBytes((System.UInt16)SndLevel).ToList(),
                .. BitConverter.GetBytes((System.UInt16)SndSource).ToList(),
            ];
            if (SndFormat != null)
                list.AddRange(SndFormat.ToBytes());
            return list.ToArray();
        }
    }

    public class SndSettingP16
    {
        public UInt32 SBIndex { get; set; }//byte[4]
        public UInt16 SndLevel { get; set; } = 100;//byte[2] uint16
        public UInt16 SndSource { get; set; }//byte[2] uint16
        public UInt32 NotifyTime { get; set; }//byte[4]
        public byte AutoConfirm { get; set; } = 0; //byte
        public WavOnlyHeaderModel? SndFormat { get; set; } = new(); //byte[18]
        public string? Interfece { get; set; }


        public SndSettingP16(byte[] b)
        {
            SBIndex = BitConverter.ToUInt32(b, 0);
            SndLevel = b.Length > 4 ? BitConverter.ToUInt16(b, 4) : (UInt16)0;
            SndSource = b.Length > 6 ? BitConverter.ToUInt16(b, 6) : (UInt16)0;
            NotifyTime = b.Length > 8 ? BitConverter.ToUInt32(b, 8) : (UInt32)0;
            AutoConfirm = b.Length >= 12 ? b[12] : new byte();
            SndFormat = b.Length >= 8 ? new WavOnlyHeaderModel(b.Skip(13).ToArray()) : new();
        }
        public SndSettingP16()
        {

        }
        public byte[] ToBytes()
        {
            List<byte> list =
            [
                .. BitConverter.GetBytes((System.UInt32)SBIndex).ToList(),
                .. BitConverter.GetBytes((System.UInt16)SndLevel).ToList(),
                .. BitConverter.GetBytes((System.UInt16)SndSource).ToList(),
                .. BitConverter.GetBytes((System.UInt32)NotifyTime).ToList(),
                AutoConfirm,
            ];
            if (SndFormat != null)
                list.AddRange(SndFormat.ToBytes());

            return list.ToArray();
        }

    }
}
