using SharedLibrary.GlobalEnums.License;

namespace SharedLibrary.Models.License;

public class LicenseMessageUpdateInfo : LicenseMessageRequest
{
    public LicenseFuture FutureId { get; private set; }

    public LicenseMessageUpdateInfo(LicenseFuture futureId
                                  , LicenseSupportPackType packType
                                  , uint requestId) : base(LicenseRequestType.UpdateLicenseInfoType
                                                         , packType
                                                         , requestId
                                                         , sizeof(uint)
                                                         , new ReadOnlyMemory<byte>(BitConverter.GetBytes((uint) futureId)))
    {
        FutureId = futureId;
    }

    protected LicenseMessageUpdateInfo()
    {
    }

    public new static LicenseMessageUpdateInfo Parse(ReadOnlySpan<byte> bytes)
    {
        var licenseMessageUpdateInfo = new LicenseMessageUpdateInfo();
        licenseMessageUpdateInfo.InternalParse(bytes);
        return licenseMessageUpdateInfo;
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
            if (RequestType != LicenseRequestType.UpdateLicenseInfoType)
            {
                throw new ArgumentOutOfRangeException(nameof(RequestType));
            }

            FutureId = (LicenseFuture) BitConverter.ToUInt32(bytes.Slice(NextParseIndex, sizeof(uint)));
            NextParseIndex += sizeof(uint);
        }
    }
}
