using System;
using System.Globalization;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace Tool
{
    public partial class DataLoader
    {

        public bool IsEmptyRow(IRow row)
        {
            return row == null;
        }

        public bool IsEmptyCell(ICell cell)
        {
            return cell == null || cell.ToString().Length == 0;
        }

        private string GetCellAddress(int columnIndex, int rowIndex)
        {
            return CellReference.ConvertNumToColString(columnIndex) + (rowIndex + 1);
        }
        
        private bool IsLocalizeType(string typeString)
        {
            return string.Equals(NormalizeType(typeString), TYPE_LOCALIZE, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsEnumType(string typeString)
        {
            return Program.EnumRepository.Contains(NormalizeType(typeString));
        }

        private bool IsBuiltInType(string typeString)
        {
            string type = NormalizeType(typeString);
            return type == TYPE_BOOL ||
                   type == TYPE_FLOAT ||
                   type == TYPE_DOUBLE ||
                   type == TYPE_INT ||
                   type == TYPE_STRING ||
                   type == TYPE_LOCALIZE ||
                   type == TYPE_INT_ARRAY ||
                   type == TYPE_FLOAT_ARRAY ||
                   type == TYPE_DOUBLE_ARRAY ||
                   type == TYPE_STRING_ARRAY;
        }

        private string NormalizeType(string typeString)
        {
            if (typeString == null) return "";

            string normalized = typeString.Trim().ToLowerInvariant();
            string compact = normalized.Replace(" ", "");

            switch (compact)
            {
                case "int[]":
                case "array[int]":
                case "int[array]":
                    return TYPE_INT_ARRAY;

                case "float[]":
                case "array[float]":
                case "float[array]":
                    return TYPE_FLOAT_ARRAY;

                case "double[]":
                case "array[double]":
                case "double[array]":
                    return TYPE_DOUBLE_ARRAY;

                case "string[]":
                case "array[string]":
                case "string[array]":
                    return TYPE_STRING_ARRAY;
            }

            return normalized;
        }
        
        // 첫 번째 row에서 subjectName으로 전달된 네임의 컬럼을 찾아서 valid한 row의 개수를 읽는다.
        // (제목이 위치한 row는 카운팅에서 제거됨.)
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="subjectName"></param>
        /// <param name="indexRow">인덱스 열 위치</param>
        /// <param name="startRaw">시작 열 위치</param>
        /// <returns></returns>
        private int GetValidRowCount(ISheet sheet, string subjectName, int indexRow, int dataRaw)
        {
            // 첫번째 줄에서 subjectName으로 지정된 ComumnIndex를 찾는다.
            var firstRow = sheet.GetRow(indexRow);
            if (firstRow == null)
                return 0;

            // subjectName이 있는 컬럼의 인덱스를 찾는다.
            var countColumnIndex = -1;
            foreach (var firstRowCell in firstRow.Cells)
                if (string.Compare(firstRowCell.ToString(), subjectName, true) == 0)
                {
                    countColumnIndex = firstRowCell.ColumnIndex;
                    break;
                }

            if (countColumnIndex == -1)
                return 0;

            var validRowCount = 0;

            for (var i = sheet.FirstRowNum + dataRaw; i <= sheet.LastRowNum; ++i)
            {
                // 실제 데이터(row)가 몇 개 존재하는지 체크하기 위함.
                // index 컬럼의 값이 있는 것만 실제 유효한 데이터
                var row = sheet.GetRow(i);
                if (IsEmptyRow(row)) break;
                
                
                // 셀 인덱스를 직접 가져와서 처리하자
                var indexCell = row.GetCell(countColumnIndex);

                // 없으면 그만
                if (indexCell == null) break;
                if (indexCell.ToString().Length == 0) break;
                
                ++validRowCount;

                // foreach (var cell in row.Cells)
                // {
                //     if (cell == null)
                //         continue;
                //
                //     if (cell.ColumnIndex != countColumnIndex)
                //         continue;
                //
                //     var cellString = cell.ToString();
                //
                //     // 값이 없어지는 순간 카운팅 종료
                //     if (cellString.Length == 0)
                //         break;
                //
                //     ++validRowCount;
                // }
            }

            return validRowCount;
        }

        // TODO 이거랑 이 아래거 합쳐야 한다.. (일단 다른거 하고 하자)
        
        public dynamic ConvertType(string type, ICell cell)
        {
            // 공식이면 
            if (cell.CellType == CellType.Formula)
            {
                if (cell.CachedFormulaResultType == CellType.Error)
                {
                    Program.LogError($"Data sheet error. Formula type cell error. Cell address - {cell.Address}");
                    return null;
                }
                
                if (cell.CellFormula.Contains("__xludf.DUMMYFUNCTION"))
                {
                    // 할게 있을거야..
                }
            }
            
            switch(NormalizeType(type))
            {
                case TYPE_BOOL:
                    if(cell.CellType == CellType.Boolean) return cell.BooleanCellValue;
                    if(cell.CellType == CellType.Numeric) return cell.NumericCellValue != 0;
                    if (cell.CellType == CellType.Formula)
                    {
                        if (cell.CachedFormulaResultType == CellType.Boolean) return cell.BooleanCellValue;
                        if (cell.CachedFormulaResultType == CellType.Numeric) return cell.NumericCellValue != 0;
                    }
                    if (cell.CellType == CellType.Blank) return false;
                    
                    Program.LogError($"Convert error. Type is not bool or numeric. Cell address - {cell.Address}");
                    return null;
                
                case TYPE_INT:
                case TYPE_LOCALIZE:
                    if(cell.CellType == CellType.Numeric) return Convert.ToInt32(cell.NumericCellValue);
                    if (cell.CellType == CellType.Formula)
                    {
                        if (cell.CachedFormulaResultType == CellType.Numeric) return Convert.ToInt32(cell.NumericCellValue);
                        if (cell.CachedFormulaResultType == CellType.String) return StringToInt32(cell.StringCellValue);
                    }
                    if (cell.CellType == CellType.Blank) return 0;
                    
                    Program.LogError($"Convert error. Type is not numeric. Cell address - {cell.Address}");
                    return null;
                
                case TYPE_FLOAT:
                case TYPE_DOUBLE:
                    if(cell.CellType == CellType.Numeric) return cell.NumericCellValue;
                    if (cell.CellType == CellType.Formula)
                    {
                        if (cell.CachedFormulaResultType == CellType.Numeric) return cell.NumericCellValue;
                        if (cell.CachedFormulaResultType == CellType.String)
                            return StringToDouble(cell.StringCellValue);
                    }
                    if (cell.CellType == CellType.Blank) return 0;
                    
                    Program.LogError($"Convert error. Type is not numeric. Cell address - {cell.Address}");
                    return null;
                
                case TYPE_STRING:
                    if(cell.CellType == CellType.Numeric) return cell.NumericCellValue.ToString();
                    
                    if (cell.CellType == CellType.Formula)
                    {
                        if(cell.CachedFormulaResultType == CellType.Numeric) return cell.NumericCellValue.ToString();
                        if(cell.CachedFormulaResultType == CellType.String) return cell.StringCellValue;
                    }
                    if(cell.CellType == CellType.String) return cell.StringCellValue;
                    if (cell.CellType == CellType.Blank) return "";
                    
                    Program.LogError($"Convert error. Type is not string. Cell address - {cell.Address}");
                    return null;
                
                case TYPE_INT_ARRAY:
                case TYPE_FLOAT_ARRAY:
                case TYPE_DOUBLE_ARRAY:
                case TYPE_STRING_ARRAY:
                    // 그냥 스트링으로 저장하고 나중에 쓸때 파싱하는걸로
                    if (cell.CellType == CellType.Numeric) return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                    if (cell.CellType == CellType.Formula)
                    {
                        if (cell.CachedFormulaResultType == CellType.Numeric) return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                        if (cell.CachedFormulaResultType == CellType.String) return cell.StringCellValue;
                    }
                    if (cell.CellType == CellType.String) return cell.StringCellValue;
                    if (cell.CellType == CellType.Blank) return null;
                    
                    Program.LogError($"Convert error. Type is not string. Cell address - {cell.Address}");
                    return null;
            }

            // 그외에는 다 string으로 처리한다. (Enum 등등)
            return GetCellString(cell);
        }

        public int StringToInt32(string value)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch (FormatException)
            {
                return 0;
            }
        }
        
        public float StringToFloat(string value)
        {
            try
            {
                return Convert.ToSingle(value);
            }
            catch (FormatException)
            {
                return 0;
            }
        }

        public double StringToDouble(string value)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public string GetCellString(ICell cell)
        {
            if(cell.CellType == CellType.Numeric) return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
            if(cell.CellType == CellType.Formula)
            {
                if(cell.CachedFormulaResultType == CellType.Numeric) return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                if(cell.CachedFormulaResultType == CellType.String) return cell.StringCellValue;
            }
            if(cell.CellType == CellType.String) return cell.StringCellValue;
            if(cell.CellType == CellType.Blank) return "";

            return "";
        }

    }
}
