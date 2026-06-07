using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Tool
{
    public static class WorkbookLoader
    {
        public static IWorkbook LoadXlsx(string path)
        {
            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                fileStream.CopyTo(memoryStream);
            }

            memoryStream.Position = 0;
            return new XSSFWorkbook(memoryStream);
        }
    }
}
