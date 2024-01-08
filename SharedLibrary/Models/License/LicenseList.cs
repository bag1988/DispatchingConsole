using System.Text;
using System.Text.Json;

namespace SharedLibrary.Models.License;

public class LicenseList
{
    public List<LicenseInfo> FutureParamExList { get; set; } = new ();

    public static LicenseList Parse(ReadOnlySpan<byte> bytes)
    {
        return JsonSerializer.Deserialize<LicenseList>(Encoding.UTF8.GetString(bytes))
            ?? throw new InvalidOperationException(nameof(bytes));
    }
}
