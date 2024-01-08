using System.Text;

namespace SharedLibrary
{

    public class DopHeader
    {
        public char[] DATA { get; set; } = Array.Empty<char>();
        public UInt32 SubChunk { get; set; } = 0;
        public byte[] ExtraBytes { get; set; } = Array.Empty<byte>();
    }

    public class WavHeaderModel
    {
        public char[] RIFF { get; set; } = new char[4] { 'R', 'I', 'F', 'F' };//"RIFF" (0->4)
        public UInt32 ChunkHeaderSize { get; set; } = 0;// file length - 8 (4->4) //размер данных секции Wave. Размер файла минус RIFF и ChunkHeaderSize
        public char[] WAVE { get; set; } = new char[4] { 'W', 'A', 'V', 'E' };//"WAVE" (8->4)
        public char[] Fmt { get; set; } = new char[4] { 'f', 'm', 't', ' ' };//"fmt " (12->4)
        public UInt32 HeaderSize { get; set; } = (UInt32)16;//16 (16->4) 16 по умолчанию (для случая без сжатия аудиопотока)
        public UInt16 AudioFormatPcm { get; set; } = 1;//1 PCM (uncompressed) or 3 (20->2) (wFormatTag)
        public UInt16 Channels { get; set; } = 1;//1 (22->2)(nChannels)
        public UInt32 SampleRate { get; set; } = 16000;//16000 (24->4)(nSamplesPerSec)
        public UInt32 ByteRate { get; set; } = 0;// avg. bytes/sec-> SampleRate*(BitsPerSample/8)*Channels (28->4)(nAvgBytesPerSec)
        public UInt16 BlockAlign { get; set; } = 0;// block-align -> Channels* (BitsPerSample/8) (32->2)(nBlockAlign)
        public UInt16 SampleSize { get; set; } = 16;//16 (34->2)(wBitsPerSample)
        public UInt16? CbSize { get; set; }//cbSize
        public char[] DATA { get; set; } = new char[4] { 'd', 'a', 't', 'a' };//"data" (36->4)
        public UInt32 SubChunk { get; set; } = 0;//sound.Length (40->4)

        public byte[]? ExtraBytes { get; set; }//доп данные для формата

        List<DopHeader> DopHeaders { get; set; } = new();

        public WavHeaderModel()
        {

        }

        public uint GetAllHeaderLength()
        {
            return HeaderSize + (uint)GetDopSection.Length + (CbSize ?? 0) + 28;
        }

        public WavHeaderModel(byte[] bytes)
        {
            int countBytes = 4;

            int startPosition = 0;
            try
            {
                if (bytes.Length > 15)
                {
                    if (bytes.Length > 42)
                    {
                        RIFF = Encoding.UTF8.GetString(bytes, startPosition/*0*/, countBytes/*4*/).ToArray();
                        startPosition += countBytes;

                        ChunkHeaderSize = BitConverter.ToUInt32(bytes, startPosition/*4*/);
                        startPosition += countBytes;

                        WAVE = Encoding.UTF8.GetString(bytes, startPosition/*8*/, countBytes/*4*/).ToArray();
                        startPosition += countBytes;


                        Fmt = Encoding.UTF8.GetString(bytes, startPosition/*12*/, countBytes/*4*/).ToArray();
                        startPosition += countBytes;

                        HeaderSize = BitConverter.ToUInt32(bytes, startPosition/*16*/);
                        startPosition += countBytes;

                    }
                    AudioFormatPcm = BitConverter.ToUInt16(bytes, startPosition/*20*/);
                    startPosition += 2;

                    Channels = BitConverter.ToUInt16(bytes, startPosition/*22*/);
                    startPosition += 2;

                    SampleRate = BitConverter.ToUInt32(bytes, startPosition/*24*/);
                    startPosition += countBytes;


                    ByteRate = BitConverter.ToUInt32(bytes, startPosition/*28*/);
                    startPosition += countBytes;


                    BlockAlign = BitConverter.ToUInt16(bytes, startPosition/*32*/);
                    startPosition += 2;

                    SampleSize = BitConverter.ToUInt16(bytes, startPosition/*34*/);
                    startPosition += 2;

                    if (HeaderSize > 16)
                    {
                        CbSize = BitConverter.ToUInt16(bytes, startPosition/*36*/);
                        startPosition += 2;
                    }

                    if (CbSize != null)
                    {
                        ExtraBytes = bytes.Skip(startPosition).Take(CbSize.Value).ToArray();
                        startPosition += CbSize.Value;
                    }

                    if (bytes.Length > 42)
                    {
                        string s = Encoding.UTF8.GetString(bytes, startPosition/*38*/, countBytes/*4*/);

                        while (s != "data" && startPosition < bytes.Length - 4)
                        {
                            startPosition += countBytes;
                            var shunks = BitConverter.ToUInt32(bytes, startPosition/*44*/);

                            startPosition += countBytes;
                            var extraBytes = bytes.Skip(startPosition).Take((int)shunks).ToArray();

                            DopHeaders.Add(new DopHeader()
                            {
                                DATA = s.ToArray(),
                                SubChunk = shunks,
                                ExtraBytes = extraBytes
                            });
                            startPosition += (int)shunks;
                            s = Encoding.UTF8.GetString(bytes, startPosition/*38*/, countBytes/*4*/);
                        }

                        if (s == "data")
                        {
                            DATA = s.ToArray();
                            startPosition += countBytes;
                            SubChunk = BitConverter.ToUInt32(bytes, startPosition/*44*/);
                            startPosition += countBytes;
                        }
                    }
                }
            }
            catch
            {

            }
        }



        public byte[] ToBytesOnlyHeader()
        {
            List<byte> header = new List<byte>();
            header.AddRange(BitConverter.GetBytes((UInt16)AudioFormatPcm));// PCM (uncompressed)
            header.AddRange(BitConverter.GetBytes((UInt16)Channels));
            header.AddRange(BitConverter.GetBytes((UInt32)SampleRate));
            header.AddRange(BitConverter.GetBytes((UInt32)(SampleRate * ((SampleSize / 8) * Channels))));// avg. bytes/sec-> SampleRate*(BitsPerSample/8)*Channels (28->4)(nAvgBytesPerSec)
            header.AddRange(BitConverter.GetBytes((UInt16)(Channels * (SampleSize / 8))));// block-align -> Channels* (BitsPerSample/8) (32->2)(nBlockAlign)
            header.AddRange(BitConverter.GetBytes((UInt16)SampleSize));// 16-bit (hardcoded in this demo) sampleSize???

            if (CbSize != null)
            {
                header.AddRange(BitConverter.GetBytes(CbSize.Value));
                if (ExtraBytes != null)
                {
                    header.AddRange(ExtraBytes.Take(CbSize.Value));
                }
            }

            return header.ToArray();
        }


        public byte[] GetDopSection
        {
            get
            {
                List<byte> header = new List<byte>();

                if (DopHeaders?.Count > 0)
                {
                    foreach (var section in DopHeaders)
                    {
                        header.AddRange(section.DATA.Select(c => (byte)c).ToArray());// "????" - chunk
                        header.AddRange(BitConverter.GetBytes((UInt32)section.SubChunk));// Sub-chunk size.
                        header.AddRange(section.ExtraBytes);
                    }
                }
                return header.ToArray();
            }
        }

        public byte[] ToBytesHeader(UInt32? Slen = null)
        {
            uint headerAllSize = (uint)ToBytesOnlyHeader().Length;

            List<byte> header = new List<byte>();
            // write WAVE header
            header.AddRange(new byte[] { 0x52, 0x49, 0x46, 0x46 });// "RIFF"

            header.AddRange(BitConverter.GetBytes((UInt32)((Slen ?? SubChunk) + headerAllSize + 28 - 8)));//all file length - 8

            header.AddRange(new byte[] { 0x57, 0x41, 0x56, 0x45 });// "WAVE"
            header.AddRange(new byte[] { 0x66, 0x6D, 0x74, 0x20 });// "fmt " chunk
            header.AddRange(BitConverter.GetBytes((UInt32)headerAllSize));// format length
            header.AddRange(ToBytesOnlyHeader());
            header.AddRange(new byte[] { 0x64, 0x61, 0x74, 0x61 });// "data" - chunk
            header.AddRange(BitConverter.GetBytes((UInt32)(Slen ?? SubChunk)));// Sub-chunk 2 size.

            return header.ToArray();
        }


        public byte[] ToBytesAllHeader(UInt32? Slen = null)
        {

            uint HeaderAllSize = (uint)ToBytesOnlyHeader().Length;

            List<byte> header = new List<byte>();
            // write WAVE header
            header.AddRange(new byte[] { 0x52, 0x49, 0x46, 0x46 });// "RIFF"
            if (ChunkHeaderSize > 100 && Slen == null)
            {
                header.AddRange(BitConverter.GetBytes((UInt32)ChunkHeaderSize));
            }
            else
            {
                header.AddRange(BitConverter.GetBytes((UInt32)((Slen ?? SubChunk) + (HeaderAllSize + GetDopSection.Length) + 28 - 8)));//all file length - 8
            }
            header.AddRange(new byte[] { 0x57, 0x41, 0x56, 0x45 });// "WAVE"
            header.AddRange(new byte[] { 0x66, 0x6D, 0x74, 0x20 });// "fmt " chunk
            header.AddRange(BitConverter.GetBytes((UInt32)HeaderAllSize));// format length
            header.AddRange(ToBytesOnlyHeader());
            if (DopHeaders?.Count > 0)
            {
                header.AddRange(GetDopSection);
            }
            header.AddRange(new byte[] { 0x64, 0x61, 0x74, 0x61 });// "data" - chunk
            header.AddRange(BitConverter.GetBytes((UInt32)(Slen ?? SubChunk)));// Sub-chunk 2 size.

            return header.ToArray();
        }


        public string ToBase64AllHeader(UInt32? Slen = null)
        {
            return Convert.ToBase64String(ToBytesAllHeader(Slen));
        }

    }

    public class WavOnlyHeaderModel
    {
        public UInt16 AudioFormatPcm { get; set; } = 1;//1 PCM (uncompressed) or 3 (20->2) (wFormatTag)
        public UInt16 Channels { get; set; } = 1;//1 (22->2)(nChannels)
        public UInt32 SampleRate { get; set; } = 16000;//16000 (24->4)(nSamplesPerSec)
        public UInt32 ByteRate { get; set; } = 32000;// avg. bytes/sec-> SampleRate*(BitsPerSample/8)*Channels (28->4)(nAvgBytesPerSec)
        public UInt16 BlockAlign { get; set; } = 2;// block-align -> Channels* (BitsPerSample/8) (32->2)(nBlockAlign)
        public UInt16 SampleSize { get; set; } = 16;//16 (34->2)(wBitsPerSample)
        public UInt16? CbSize { get; set; }//cbSize
        public byte[]? ExtraBytes { get; set; }//доп данные для формата

        public WavOnlyHeaderModel()
        {
           
        }


        public WavOnlyHeaderModel(byte[] bytes)
        {
            int countBytes = 4;

            int startPosition = 0;

            if (bytes.Length > 13)
            {
                AudioFormatPcm = BitConverter.ToUInt16(bytes, startPosition/*0*/);
                startPosition += 2;

                Channels = BitConverter.ToUInt16(bytes, startPosition/*2*/);
                startPosition += 2;


                SampleRate = BitConverter.ToUInt32(bytes, startPosition/*4*/);
                startPosition += countBytes;


                ByteRate = BitConverter.ToUInt32(bytes, startPosition/*8*/);
                startPosition += countBytes;


                BlockAlign = BitConverter.ToUInt16(bytes, startPosition/*12*/);
                startPosition += 2;

                SampleSize = BitConverter.ToUInt16(bytes, startPosition/*14*/);
                startPosition += 2;

                if (bytes.Length >= 18)
                {
                    if (AudioFormatPcm == 1)
                    {
                        CbSize = BitConverter.ToUInt16(bytes, startPosition /*16*/);
                        startPosition += 2;
                    }

                    if (CbSize != null)
                    {
                        ExtraBytes = bytes.Skip(startPosition).Take(CbSize.Value).ToArray();
                    }
                }

                if (BlockAlign == 0 && SampleSize > 0)
                {
                    BlockAlign = (ushort)(Channels * (SampleSize / 8));
                }

                if (ByteRate == 0 && BlockAlign > 0)
                {
                    ByteRate = SampleRate * BlockAlign;
                }
            }
        }



        public byte[] ToBytes()
        {
            List<byte> header = new List<byte>();
            header.AddRange(BitConverter.GetBytes((UInt16)AudioFormatPcm));// PCM (uncompressed)
            header.AddRange(BitConverter.GetBytes((UInt16)Channels));
            header.AddRange(BitConverter.GetBytes((UInt32)SampleRate));
            header.AddRange(BitConverter.GetBytes((UInt32)(SampleRate * ((SampleSize / 8) * Channels))));// avg. bytes/sec-> SampleRate*(BitsPerSample/8)*Channels (28->4)(nAvgBytesPerSec)
            header.AddRange(BitConverter.GetBytes((UInt16)(Channels * (SampleSize / 8))));// block-align -> Channels* (BitsPerSample/8) (32->2)(nBlockAlign)
            header.AddRange(BitConverter.GetBytes((UInt16)SampleSize));// 16-bit (hardcoded in this demo) sampleSize???

            if (CbSize != null)
            {
                header.AddRange(BitConverter.GetBytes(CbSize.Value));
                if (ExtraBytes != null)
                {
                    header.AddRange(ExtraBytes.Take(CbSize.Value));
                }
            }

            return header.ToArray();
        }
    }

}
