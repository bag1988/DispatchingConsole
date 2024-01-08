using SharedLibrary.GlobalEnums.License;

namespace SharedLibrary.Models.License;

public class LicenseMessageResponse : LicenseMessage
{
    public LicenseErrorCode ErrorCode { get; private set; }

    protected LicenseMessageResponse(LicenseErrorCode errorCode
                                   , LicenseSupportPackType packType
                                   , uint requestId
                                   , uint size
                                   , ReadOnlyMemory<byte>? data = null) : base(packType
                                                                             , LicenseHeaderType.HeaderResponseType
                                                                             , requestId
                                                                             , size
                                                                             , data)
    {
        ErrorCode = errorCode;
    }

    protected LicenseMessageResponse()
    {
    }

    public new static LicenseMessageResponse Parse(ReadOnlySpan<byte> bytes)
    {
        return LicenseMessageFutureResponse.Parse(bytes);
    }

    public override IEnumerable<byte> GetBytes()
    {
        var bytes = base.GetBytes();
        bytes = bytes.Concat(BitConverter.GetBytes((uint) ErrorCode));
        return bytes;
    }

    protected override void InternalParse(ReadOnlySpan<byte> bytes)
    {
        if (!IsParsed)
        {
            base.InternalParse(bytes);
            if (HeaderType != LicenseHeaderType.HeaderResponseType)
            {
                throw new ArgumentOutOfRangeException(nameof(HeaderType));
            }

            ErrorCode = (LicenseErrorCode) BitConverter.ToUInt32(bytes.Slice(NextParseIndex, sizeof(uint)));
            NextParseIndex += sizeof(uint);
        }
    }
}
