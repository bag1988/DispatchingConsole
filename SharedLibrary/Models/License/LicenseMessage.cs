using System.Text;
using SharedLibrary.GlobalEnums.License;

namespace SharedLibrary.Models.License;

public class LicenseMessage
{
    public string Signature { get; private set; } = PROTOCOL_SIGNATURE;

    public LicenseSupportPackType PackType { get; private set; }

    public LicenseHeaderType HeaderType { get; private set; }

    public uint RequestId { get; private set; }

    public int Size { get; private set; }

    public uint DataSize { get; private set; }

    private byte[] data = Array.Empty<byte>();

    public ReadOnlyMemory<byte> Data => data;

    protected bool IsParsed { get; private set; }

    protected LicenseMessage(LicenseSupportPackType packType
                           , LicenseHeaderType headerType
                           , uint requestId
                           , uint size
                           , ReadOnlyMemory<byte>? data = null)
    {
        PackType = packType;
        HeaderType = headerType;
        RequestId = requestId;
        DataSize = size;
        this.data = data?.ToArray() ?? this.data;
    }

    protected LicenseMessage()
    {
    }

    public static int GetExpectedLengthForParse(ReadOnlySpan<byte> bytes)
    {
        var expectedLengthForParse = int.MaxValue;
        if (bytes.Length >= MIN_SIZE)
        {
            var licenseMessage = new LicenseMessage();
            licenseMessage.InternalParse(bytes);
            expectedLengthForParse = licenseMessage.Size;
        }

        return expectedLengthForParse;
    }

    public static LicenseMessage Parse(ReadOnlySpan<byte> bytes)
    {
        LicenseMessage licenseMessage;
        var headerType = (LicenseHeaderType) BitConverter.ToUInt32(bytes.Slice(16 + sizeof(uint), sizeof(uint)));
        switch (headerType)
        {
            case LicenseHeaderType.HeaderRequestType:
                licenseMessage = LicenseMessageRequest.Parse(bytes);
                break;

            case LicenseHeaderType.HeaderResponseType:
                licenseMessage = LicenseMessageResponse.Parse(bytes);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(headerType));
        }

        licenseMessage.InternalParse(bytes);
        return licenseMessage;
    }

    public static IEnumerable<byte> GetBytes(LicenseMessage @this) => @this.GetBytes();

    public virtual IEnumerable<byte> GetBytes()
    {
        if (PackType != LicenseSupportPackType.WithoutPack)
        {
            throw new NotImplementedException(nameof(PackType));
        }

        var byteList = new List<byte>();
        byteList.AddRange(Encoding.UTF8.GetBytes(Signature).Concat(Enumerable.Repeat((byte) 0, 16)).Take(16));
        byteList.AddRange(BitConverter.GetBytes((uint) PackType));
        byteList.AddRange(BitConverter.GetBytes((uint) HeaderType));
        byteList.AddRange(BitConverter.GetBytes(RequestId));
        byteList.AddRange(BitConverter.GetBytes(DataSize));

        return byteList;
    }

    protected virtual void InternalParse(ReadOnlySpan<byte> bytes)
    {
        if (!IsParsed)
        {
            Signature = Encoding.UTF8.GetString(bytes.Slice(NextParseIndex, 16)).Trim((char) 0);
            if (!string.Equals(PROTOCOL_SIGNATURE, Signature))
            {
                throw new ArgumentOutOfRangeException(nameof(Signature), $"Неверные данные или не соответствует версия протокола: {Signature}.");
            }

            NextParseIndex += 16;

            PackType = (LicenseSupportPackType) BitConverter.ToUInt32(bytes.Slice(NextParseIndex, sizeof(uint)));
            if (!System.Enum.IsDefined(PackType))
            {
                throw new ArgumentOutOfRangeException(nameof(PackType));
            }

            if (PackType != LicenseSupportPackType.WithoutPack)
            {
                throw new NotImplementedException(nameof(PackType));
            }

            NextParseIndex += sizeof(uint);

            HeaderType = (LicenseHeaderType) BitConverter.ToUInt32(bytes.Slice(NextParseIndex, sizeof(uint)));
            if (!System.Enum.IsDefined(HeaderType))
            {
                throw new ArgumentOutOfRangeException(nameof(HeaderType));
            }

            NextParseIndex += sizeof(uint);

            RequestId = BitConverter.ToUInt32(bytes.Slice(NextParseIndex, sizeof(uint)));
            NextParseIndex += sizeof(uint);

            DataSize = BitConverter.ToUInt32(bytes.Slice(NextParseIndex, sizeof(uint)));
            NextParseIndex += sizeof(uint);

            Size = (int) (MIN_SIZE + DataSize);
            if (bytes.Length > NextParseIndex + sizeof(uint))
            {
                data = bytes.Slice(NextParseIndex + sizeof(uint)).ToArray();
            }

            IsParsed = true;
        }
    }
    protected int NextParseIndex;

    private const string PROTOCOL_SIGNATURE = "SENSORLICENSE10";
    private const int MIN_SIZE = 16 + 5 * sizeof(uint);
}
