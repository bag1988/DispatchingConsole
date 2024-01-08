using SharedLibrary.GlobalEnums.License;

namespace SharedLibrary.Models.License;

public class LicenseMessageFutureRequest : LicenseMessageRequest
{
    public LicenseFuture FutureId { get; private set; }

    public LicenseMessageFutureRequest(LicenseFuture futureId
                                     , LicenseSupportPackType packType
                                     , uint requestId) : base(LicenseRequestType.LicenseFutureRequestType
                                                            , packType
                                                            , requestId
                                                            , sizeof(uint)
                                                            , new ReadOnlyMemory<byte>(BitConverter.GetBytes((uint) futureId)))
    {
        FutureId = futureId;
    }

    protected LicenseMessageFutureRequest()
    {
    }

    public new static LicenseMessageFutureRequest Parse(ReadOnlySpan<byte> bytes)
    {
        var licenseMessageFutureRequest = new LicenseMessageFutureRequest();
        licenseMessageFutureRequest.InternalParse(bytes);
        return licenseMessageFutureRequest;
    }

    public override IEnumerable<byte> GetBytes()
    {
        var bytes = base.GetBytes();
        bytes = bytes.Concat(BitConverter.GetBytes((uint) FutureId));
        return bytes;
    }

    protected override void InternalParse(ReadOnlySpan<byte> bytes)
    {
        if (!IsParsed)
        {
            base.InternalParse(bytes);
            if (RequestType != LicenseRequestType.LicenseFutureRequestType)
            {
                throw new ArgumentOutOfRangeException(nameof(RequestType));
            }

            FutureId = (LicenseFuture) BitConverter.ToUInt32(bytes.Slice(NextParseIndex, sizeof(uint)));
            NextParseIndex += sizeof(uint);
        }
    }
}
