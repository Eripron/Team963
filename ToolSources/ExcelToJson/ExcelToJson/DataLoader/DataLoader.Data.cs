using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;

namespace Tool
{
    public partial class DataLoader
    {
        public bool ParseDataInfo(ISheet dataSheet)
        {
            IRow subjectRow = dataSheet.GetRow(Program.Settings.DataSubjectRow);
            IRow referenceRow = dataSheet.GetRow(Program.Settings.DataReferenceRow);
            IRow typeRow = dataSheet.GetRow(Program.Settings.DataTypeRow);

            if (subjectRow == null || referenceRow == null || typeRow == null)
            {
                Program.LogError("Data Sheet의 기본 Row 설정을 찾을 수 없습니다.", "", FileName, dataSheet.SheetName);
                return false;
            }

            bool unused = false;
            int unusedIndex = 0;

            foreach (ICell cell in subjectRow.Cells)
            {
                if (IsEmptyCell(cell)) continue;

                string subjectName = GetCellString(cell).Trim();
                ICell referenceCell = referenceRow.GetCell(cell.ColumnIndex);
                ICell typeCell = typeRow.GetCell(cell.ColumnIndex);
                string referenceName = IsEmptyCell(referenceCell) ? "" : GetCellString(referenceCell).Trim();

                if (IsEmptyCell(typeCell))
                {
                    Program.LogError("Type 컬럼을 읽을 수 없습니다.", cell.Address.ToString(), FileName, dataSheet.SheetName);
                    return false;
                }

                string type = NormalizeType(GetCellString(typeCell));
                if (IsBuiltInType(type) == false && IsEnumType(type) == false)
                {
                    Program.LogError($"지원하지 않는 Type입니다. Type:{type}", typeCell.Address.ToString(), FileName, dataSheet.SheetName);
                    return false;
                }

                if (string.Equals(subjectName, "Unused", StringComparison.OrdinalIgnoreCase))
                {
                    unused = true;
                    unusedIndex = cell.ColumnIndex;
                }

                Data data = new Data();
                data.Reference = referenceName;
                data.DataList = new List<dynamic>();
                data.Subject = subjectName;
                data.Type = type;
                data.columnAddress = Regex.Replace(cell.Address.ToString(), @"\d", "");
                DataInfo[subjectName] = data;
            }

            ICell indexSubjectCell = subjectRow.GetCell(Program.Settings.IndexCell);
            if (IsEmptyCell(indexSubjectCell))
            {
                Program.LogError("Index 기준 컬럼을 찾을 수 없습니다.", GetCellAddress(Program.Settings.IndexCell, Program.Settings.DataSubjectRow), FileName, dataSheet.SheetName);
                return false;
            }

            string indexSubjectName = GetCellString(indexSubjectCell).Trim();
            if (string.Equals(indexSubjectName, Program.Settings.IndexSubjectName, StringComparison.OrdinalIgnoreCase) == false)
            {
                Program.LogError($"Index 기준 컬럼은 {Program.Settings.IndexSubjectName}이어야 합니다.", indexSubjectCell.Address.ToString(), FileName, dataSheet.SheetName);
                return false;
            }

            int rowCount = GetValidRowCount(dataSheet, Program.Settings.IndexSubjectName, Program.Settings.DataSubjectRow, Program.Settings.DataDataRow);
            int cellCount = subjectRow.LastCellNum;

            for (var rowIndex = Program.Settings.DataDataRow; rowIndex < rowCount + Program.Settings.DataDataRow; ++rowIndex)
            {
                IRow row = dataSheet.GetRow(rowIndex);
                if (IsEmptyRow(row)) break;

                if (unused)
                {
                    ICell cell = row.GetCell(unusedIndex);
                    dynamic unusedValue = cell == null ? false : ConvertType(TYPE_BOOL, cell);
                    if (unusedValue is bool && unusedValue)
                    {
                        continue;
                    }
                }

                for (int cellIndex = 0; cellIndex < cellCount; ++cellIndex)
                {
                    ICell subjectCell = subjectRow.GetCell(cellIndex);
                    if (IsEmptyCell(subjectCell)) continue;

                    string subjectName = subjectCell.ToString();
                    if (subjectName.Length == 0) continue;

                    ICell cell = row.GetCell(cellIndex, MissingCellPolicy.CREATE_NULL_AS_BLANK);

                    dynamic cellValue = ConvertType(DataInfo[subjectName].Type, cell);

                    if (IsLocalizeType(DataInfo[subjectName].Type))
                    {
                        List<Localize> localizeList = GetLocalizeList(DataInfo[subjectName].Subject, cellValue);
                        DataInfo[subjectName].DataList.Add(localizeList);
                    }
                    else if (IsEnumType(DataInfo[subjectName].Type))
                    {
                        int enumIndex = GetEnumValue(DataInfo[subjectName].Type, cellValue);
                        if (enumIndex < 0)
                        {
                            Program.LogError(
                                $"Enum 값을 찾을 수 없습니다. Type:{DataInfo[subjectName].Type}, Value:{cellValue}",
                                cell.Address.ToString(),
                                FileName,
                                dataSheet.SheetName);
                            return false;
                        }

                        DataInfo[subjectName].DataList.Add(enumIndex + 1);
                    }
                    else
                    {
                        DataInfo[subjectName].DataList.Add(cellValue);
                    }
                }
            }

            return true;
        }

        public bool CheckReference()
        {
            foreach (var kv in DataInfo)
            {
                string referenceName = kv.Value.Reference;
                if (referenceName.Length == 0) continue;
                if (kv.Value.Type != TYPE_INT) continue;

                DataLoader referenceLoader = Program.GetDataLoader(referenceName);
                if (referenceLoader == null)
                {
                    Program.LogWarning("참조하고 있는 파일을 찾을 수 없습니다.", "", referenceName, dataName);
                    return false;
                }

                bool indexExist = referenceLoader.DataInfo.ContainsKey(Program.Settings.IndexSubjectName);
                bool groupExist = referenceLoader.DataInfo.ContainsKey(Program.Settings.GroupSubjectName);
                if (indexExist == false && groupExist == false)
                {
                    Program.LogWarning($"{referenceName} 문서에서 {Program.Settings.IndexSubjectName}({Program.Settings.GroupSubjectName}) 컬럼을 찾을 수 없습니다.");
                    return false;
                }

                int row = 0;
                foreach (var value in kv.Value.DataList)
                {
                    if (value == 0)
                    {
                        row++;
                        continue;
                    }

                    if (groupExist)
                    {
                        if (referenceLoader.DataInfo[Program.Settings.GroupSubjectName].DataList.FindIndex(v => v == value) < 0)
                        {
                            Program.LogWarning($"{referenceName} 문서에서 해당 {Program.Settings.GroupSubjectName} - {value} 을(를) 찾을 수 없습니다.", kv.Value.columnAddress + (Program.Settings.DataDataRow + row + 1), dataName);
                        }
                    }
                    else
                    {
                        if (referenceLoader.DataInfo[Program.Settings.IndexSubjectName].DataList.FindIndex(v => v == value) < 0)
                        {
                            Program.LogWarning($"{referenceName} 문서에서 해당 {Program.Settings.IndexSubjectName} - {value} 을(를) 찾을 수 없습니다.", kv.Value.columnAddress + (Program.Settings.DataDataRow + row + 1), dataName);
                        }
                    }

                    row++;
                }
            }

            return true;
        }
    }
}
