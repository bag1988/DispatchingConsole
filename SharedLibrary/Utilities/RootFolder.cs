namespace SharedLibrary.Utilities
{
    public class RootFolder
    {
        public static string GetPath(string RFolder, string nameDir)
        {
            nameDir = nameDir.TrimStart(new char[] { '/', '\\' });
            nameDir = nameDir.TrimEnd(new char[] { '/', '\\' });
            string path = Path.Combine(RFolder, nameDir);
            return path;
        }
    }
}
