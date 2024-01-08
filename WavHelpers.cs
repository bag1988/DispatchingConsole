using System.Runtime.InteropServices;

namespace LibraryProto.Helpers
{
    public static class WavHelpers
    {
        public static byte[] WavFromFormatAndSound(byte[] format, byte[] sound)
        {
            byte[] wav = format.Concat(sound).ToArray();
            return wav;
        }


        //Переводим format из БД в Dictionary<string, int> 
        public static Dictionary<string, int> FormatToDictionary(byte[] format)
        { 
            Dictionary<string, int> formatToDictionary = new Dictionary<string, int>();

            //первое 2 байта - Аудио формат, список допустипых форматов. Для PCM = 1 (то есть, Линейное квантование). Значения, отличающиеся от 1, обозначают некоторый формат сжатия.
            var audioFormat = format.Take(2).ToArray();
            formatToDictionary.Add("audioFormat", (Int32)(BitConverter.ToInt16(audioFormat, 0)));

            //след 2 байта - Количество каналов. Моно = 1, Стерео = 2 и т.д.
            var numChannels = format.Skip(2).Take(2).ToArray();
            formatToDictionary.Add("numChannels", (Int32)(BitConverter.ToInt16(numChannels, 0)));

            //след 4 байта - Частота дискретизации. 8000 Гц, 44100 Гц и т.д.
            var sampleRate = format.Skip(4).Take(4).ToArray();
            formatToDictionary.Add("sampleRate", (Int32)(BitConverter.ToInt16(sampleRate, 0)));


            //след 4 байта - Количество байт, переданных за секунду воспроизведения.
            var byteRate = format.Skip(8).Take(4).ToArray();
            formatToDictionary.Add("byteRate", (Int32)(BitConverter.ToInt16(byteRate, 0)));


            //след 2 байта - Количество байт для одного сэмпла, включая все каналы.
            var blockAlign = format.Skip(12).Take(2).ToArray();
            formatToDictionary.Add("blockAlign", (Int32)(BitConverter.ToInt16(blockAlign, 0)));


            //Количество бит в сэмпле. Так называемая «глубина» или точность звучания. 8 бит, 16 бит и т.д.
            var bitsPerSample = format.Skip(14).Take(2).ToArray();
            formatToDictionary.Add("bitsPerSample", (Int32)(BitConverter.ToInt16(bitsPerSample, 0)));


            return formatToDictionary;
        }


        public static List<byte[]> FormatAndSoundFromWav(byte[] wav)
        {
            var header = new WavHeader();

            // Размер заголовка
            var headerSize = Marshal.SizeOf(header);

            // Чтобы не считывать каждое значение заголовка по отдельности,
            // воспользуемся выделением unmanaged блока памяти
            var headerPtr = Marshal.AllocHGlobal(headerSize);

            // Копируем считанные байты из файла в выделенный блок памяти
            Marshal.Copy(wav, 0, headerPtr, headerSize);

            // Преобразовываем указатель на блок памяти к нашей структуре
            Marshal.PtrToStructure(headerPtr, header);

            //Длина тело файлв файла (format)
            //var leinghFormat = header.Subchunk2Size;
            var leinghFormat = wav.Length - headerSize;

            // Освобождаем выделенный блок памяти
            Marshal.FreeHGlobal(headerPtr);

            MemoryStream wavStream = new MemoryStream(wav);

            byte[] bufferFormat = new byte[headerSize]; //Создаем переменную с размером заголовка (обычно 44 байта)

            if (wavStream.Read(bufferFormat, 0, headerSize) != headerSize)
            {
                throw new Exception("File corrupted");
            }

            //!!! Заголовок (format) сообщения в байтах

            //!!! всего ЗАГОЛОВОК занимает 44 байта - на по нашей старой логике в заголовок идут только 16 байст с 20 позиции
            byte[] byteFormat = new byte[16];
            byteFormat = bufferFormat.Skip(20).Take(16).ToArray();

            // Создаем переменную для Тела сообщения в байтах
            byte[] byteFile = new byte[leinghFormat];
            wavStream.Read(byteFile, 0, (int)leinghFormat);

            List<byte[]> formatAndSound = new List<byte[]>();
            formatAndSound.Add(byteFormat);
            formatAndSound.Add(byteFile);

            return formatAndSound;

            //SQL скрипт что бы заменить данные после теста
            //UPDATE[Message]
            //SET[Sound] = (SELECT[Sound] FROM[GSO].[dbo].[Message] where MsgID = 4474 and StaffID = 45510988)
            //where MsgID = 16437 and StaffID = 71208132

            //UPDATE[Message]
            //SET[Format] = (SELECT[Format] FROM[GSO].[dbo].[Message] where MsgID = 4474 and StaffID = 45510988)
            //where MsgID = 16437 and StaffID = 71208132

        }


        [StructLayout(LayoutKind.Sequential)]
        // Структура, описывающая заголовок WAV файла.
        internal class WavHeader
        {
            // WAV-формат начинается с RIFF-заголовка:

            // Содержит символы "RIFF" в ASCII кодировке
            // (0x52494646 в big-endian представлении)
            public UInt32 ChunkId;

            // 36 + subchunk2Size, или более точно:
            // 4 + (8 + subchunk1Size) + (8 + subchunk2Size)
            // Это оставшийся размер цепочки, начиная с этой позиции.
            // Иначе говоря, это размер файла - 8, то есть,
            // исключены поля chunkId и chunkSize.
            public UInt32 ChunkSize;

            // Содержит символы "WAVE"
            // (0x57415645 в big-endian представлении)
            public UInt32 Format;

            // Формат "WAVE" состоит из двух подцепочек: "fmt " и "data":
            // Подцепочка "fmt " описывает формат звуковых данных:

            // Содержит символы "fmt "
            // (0x666d7420 в big-endian представлении)
            public UInt32 Subchunk1Id;

            // 16 для формата PCM.
            // Это оставшийся размер подцепочки, начиная с этой позиции.
            public UInt32 Subchunk1Size;

            // Аудио формат, полный список можно получить здесь http://audiocoding.ru/
            // Для PCM = 1 (то есть, Линейное квантование).
            // Значения, отличающиеся от 1, обозначают некоторый формат сжатия.
            public UInt16 AudioFormat;

            // Количество каналов. Моно = 1, Стерео = 2 и т.д.
            public UInt16 NumChannels;

            // Частота дискретизации. 8000 Гц, 44100 Гц и т.д.
            public UInt32 SampleRate;

            // sampleRate * numChannels * bitsPerSample/8
            public UInt32 ByteRate;

            // numChannels * bitsPerSample/8
            // Количество байт для одного сэмпла, включая все каналы.
            public UInt16 BlockAlign;

            // Так называемая "глубиная" или точность звучания. 8 бит, 16 бит и т.д.
            public UInt16 BitsPerSample;

            // Подцепочка "data" содержит аудио-данные и их размер.

            // Содержит символы "data"
            // (0x64617461 в big-endian представлении)
            public UInt32 Subchunk2Id;

            // numSamples * numChannels * bitsPerSample/8
            // Количество байт в области данных.
            public UInt32 Subchunk2Size;

            // Далее следуют непосредственно Wav данные.


        }
    }
}
