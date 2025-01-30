using System;
using System.IO;

namespace LEAD_OLAP_DESINGER.Helpers
{
    public class DirectoryHelper
    {
        public static string GetResDirectory()
        {
            string? resdir = string.Empty;

            if (OperatingSystem.IsLinux())
            {
                int pos = AppContext.BaseDirectory.LastIndexOf("/");
                string? dir = AppContext.BaseDirectory.Substring(0, pos);
                string? result = Path .GetDirectoryName(dir);
                resdir = result + "//";
            }
            else if (OperatingSystem.IsWindows())
            {
                int pos = AppContext.BaseDirectory.LastIndexOf("\\");
                string? dir = AppContext.BaseDirectory.Remove(pos, AppContext.BaseDirectory.Length - pos);
                string? result = Path.GetDirectoryName(dir);
                resdir = result + "\\";
            }

            return resdir;
        }
        public static string GetFolderDirectory(string path)
        {
            string? resdir = string.Empty;

            if (OperatingSystem.IsLinux())
            {
                resdir = path + "//";
            }
            else if (OperatingSystem.IsWindows())
            {
                resdir = path + "\\";
            }

            return resdir;
        }
    }
}
