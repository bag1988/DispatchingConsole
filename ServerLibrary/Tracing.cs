namespace ServerLibrary;

public static class Tracing
{
    public static string[] GetSources()
    {
        var sources = new List<string>(GetAllEndPoint.Get);
        //sources.AddRange(ServiceLibrary.Diagnostics.Sources);
        return sources.ToArray();
    }

    public static string[] Sources { get; } = GetSources();
}

