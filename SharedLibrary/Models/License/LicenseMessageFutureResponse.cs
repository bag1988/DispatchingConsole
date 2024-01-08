using SharedLibrary.GlobalEnums.License;

namespace SharedLibrary.Models.License;

public class LicenseMessageFutureResponse : LicenseMessageResponse
{
    public LicenseList LicenseList { get; private set; } = new ();

    public LicenseMessageFutureResponse(LicenseSupportPackType packType
                                      , uint requestId
                                      , uint size
                                      , ReadOnlyMemory<byte> data) : base(LicenseErrorCode.ErrorSuccess
                                                                        , packType
                                                                        , requestId
                                                                        , size
                                                                        , data)
    {
    }

    protected LicenseMessageFutureResponse()
    {
    }

    public new static LicenseMessageFutureResponse Parse(ReadOnlySpan<byte> bytes)
    {
        var licenseMessageFutureRequest = new LicenseMessageFutureResponse();
        licenseMessageFutureRequest.InternalParse(bytes);
        return licenseMessageFutureRequest;
    }

    public override IEnumerable<byte> GetBytes()
    {
        throw new NotImplementedException();

        //var bytes = base.GetBytes();
        //bytes = bytes.Concat(BitConverter.GetBytes((uint) ErrorCode));
        //return bytes;
    }

    protected override void InternalParse(ReadOnlySpan<byte> bytes)
    {
        if (!IsParsed)
        {
            base.InternalParse(bytes);
            if (NextParseIndex < bytes.Length)
            {
                LicenseList = LicenseList.Parse(bytes.Slice(NextParseIndex));

                NextParseIndex = bytes.Length;
            }
        }
    }
}
