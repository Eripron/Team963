using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;

namespace Tool
{
    public class EnumRepository
    {
        private readonly Dictionary<string, List<string>> enumInfo = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public bool Load(string enumExcelPath)
        {
            if (File.Exists(enumExcelPath) == false)
            {
                Program.LogError($"Enum 엑셀 파일을 읽을 수 없습니다. - {enumExcelPath}");
                return false;
            }

            Program.LogMSG($"Enum 엑셀 로드중... {enumExcelPath}");

            try
            {
                IWorkbook workbook = WorkbookLoader.LoadXlsx(enumExcelPath);
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    ISheet sheet = workbook.GetSheetAt(i);
                    if (ParseSheet(sheet, Path.GetFileName(enumExcelPath)) == false)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogError($"Enum 엑셀 로드 예외 - {ex.Message}");
                Console.WriteLine($"Stacktrace - {ex.StackTrace}");
                return false;
            }

            return true;
        }

        public bool Contains(string enumName)
        {
            return enumInfo.ContainsKey(enumName);
        }

        public int GetValueIndex(string enumName, string value)
        {
            if (value == null || value.Length == 0) return -1;

            List<string> enumValueList;
            if (enumInfo.TryGetValue(enumName, out enumValueList) == false)
            {
                return -1;
            }

            return enumValueList.FindIndex(enumValue => enumValue.Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        private bool ParseSheet(ISheet sheet, string fileName)
        {
            IRow nameRow = sheet.GetRow(Program.Settings.EnumNameRow);
            if (nameRow == null) return true;

            foreach (ICell cell in nameRow.Cells)
            {
                if (IsEmptyCell(cell)) continue;

                string enumName = GetCellString(cell);
                if (enumInfo.ContainsKey(enumName))
                {
                    Program.LogError($"중복된 Enum 이름이 존재합니다. Name:{enumName}", cell.Address.ToString(), fileName, sheet.SheetName);
                    return false;
                }

                enumInfo.Add(enumName, new List<string>());
            }

            for (int rowIndex = sheet.FirstRowNum + Program.Settings.EnumDataRow; rowIndex <= sheet.LastRowNum; ++rowIndex)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) break;

                foreach (ICell cell in row.Cells)
                {
                    if (IsEmptyCell(cell)) continue;

                    ICell nameCell = nameRow.GetCell(cell.ColumnIndex);
                    if (IsEmptyCell(nameCell)) continue;

                    string enumName = GetCellString(nameCell);
                    string enumValue = GetCellString(cell);

                    List<string> enumValueList;
                    if (enumInfo.TryGetValue(enumName, out enumValueList) == false)
                    {
                        Program.LogError($"Enum 이름을 찾을 수 없습니다. Name:{enumName}", cell.Address.ToString(), fileName, sheet.SheetName);
                        return false;
                    }

                    if (ContainsEnumValue(enumValueList, enumValue))
                    {
                        Program.LogError($"중복된 Enum 값이 존재합니다. Value:{enumValue}", cell.Address.ToString(), fileName, sheet.SheetName);
                        return false;
                    }

                    enumValueList.Add(enumValue);
                }
            }

            return true;
        }

        private static bool ContainsEnumValue(List<string> enumValueList, string enumValue)
        {
            foreach (string value in enumValueList)
            {
                if (value.Equals(enumValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEmptyCell(ICell cell)
        {
            return cell == null || GetCellString(cell).Length == 0;
        }

        private static string GetCellString(ICell cell)
        {
            if (cell.CellType == CellType.Numeric) return cell.NumericCellValue.ToString();
            if (cell.CellType == CellType.Formula)
            {
                if (cell.CachedFormulaResultType == CellType.Numeric) return cell.NumericCellValue.ToString();
                if (cell.CachedFormulaResultType == CellType.String) return cell.StringCellValue;
            }
            if (cell.CellType == CellType.String) return cell.StringCellValue;
            if (cell.CellType == CellType.Blank) return "";

            return cell.ToString();
        }
    }
}
