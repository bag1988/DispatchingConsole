using SharedLibrary.GlobalEnums.License;

namespace SharedLibrary.Models.License;

public class LicenseMessageRequest : LicenseMessage
{
    public LicenseRequestType RequestType { get; private set; }

    protected LicenseMessageRequest(LicenseRequestType requestType
                                  , LicenseSupportPackType packType
                                  , uint requestId
                                  , uint size
                                  , ReadOnlyMemory<byte>? data = null) : base(packType
                                                                            , LicenseHeaderType.HeaderRequestType
                                                                            , requestId
                                                                            , size
                                                                            , data)
    {
        RequestType = requestType;
    }

    protected LicenseMessageRequest()
    {
    }

    public new static LicenseMessageRequest Parse(ReadOnlySpan<byte> bytes)
    {
        LicenseMessageRequest licenseMessageRequest;
        var requestType = (LicenseRequestType) BitConverter.ToUInt32(bytes.Slice(16 + 4 * sizeof(uint), sizeof(uint)));
        switch (requestType)
        {
            case LicenseRequestType.LoginRequestType:
                throw new NotImplementedException(nameof(LicenseRequestType.LoginRequestType));

            case LicenseRequestType.CheckClientRequestType:
                throw new NotImplementedException(nameof(LicenseRequestType.CheckClientRequestType));

            case LicenseRequestType.CheckServerRequestType:
                throw new NotImplementedException(nameof(LicenseRequestType.CheckServerRequestType));

            case LicenseRequestType.LicenseFutureRequestType:
                licenseMessageRequest = LicenseMessageFutureRequest.Parse(bytes);
                break;

            case LicenseRequestType.UpdateLicenseInfoType:
                licenseMessageRequest = LicenseMessageUpdateInfo.Parse(bytes);
                break;

            case LicenseRequestType.SubscribeFutureNotifyType:
                throw new NotImplementedException(nameof(LicenseRequestType.SubscribeFutureNotifyType));

            case LicenseRequestType.UnsubscribeFutureNotifyType:
                throw new NotImplementedException(nameof(LicenseRequestType.UnsubscribeFutureNotifyType));

            case LicenseRequestType.FutureNotifyType:
                throw new NotImplementedException(nameof(LicenseRequestType.FutureNotifyType));

            case LicenseRequestType.SoftwareLicenseGenerateRequestType:
                throw new NotImplementedException(nameof(LicenseRequestType.SoftwareLicenseGenerateRequestType));

            case LicenseRequestType.UzsActivationGenerateRequestType:
                throw new NotImplementedException(nameof(LicenseRequestType.UzsActivationGenerateRequestType));

            case LicenseRequestType.UnknownRequestType:
                throw new NotImplementedException(nameof(LicenseRequestType.UnknownRequestType));

            default:
                throw new ArgumentOutOfRangeException(nameof(requestType));
        }

        licenseMessageRequest.InternalParse(bytes);
        return licenseMessageRequest;
    }

    public override IEnumerable<byte> GetBytes()
    {
        var bytes = base.GetBytes();
        bytes = bytes.Concat(BitConverter.GetBytes((uint) RequestType));
        return bytes;
    }

    protected override void InternalParse(ReadOnlySpan<byte> bytes)
    {
        if (!IsParsed)
        {
            base.InternalParse(bytes);
            if (HeaderType != LicenseHeaderType.HeaderRequestType)
            {
                throw new ArgumentOutOfRangeException(nameof(HeaderType));
            }

            RequestType = (LicenseRequestType) BitConverter.ToUInt32(bytes.Slice(NextParseIndex, sizeof(uint)));
            NextParseIndex += sizeof(uint);
        }
    }
}
