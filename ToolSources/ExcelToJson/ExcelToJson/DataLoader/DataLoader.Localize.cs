using System.Collections.Generic;
using NPOI.SS.UserModel;

namespace Tool;

public struct Localize
{
    public string Charset;
    public string Value;
}

public struct LocalizeInfo
{
    public List<Localize> Localizes;
}

public struct LocalizeSheet
{
    public Dictionary<int, LocalizeInfo> LocalizeInfos;
}

public partial class DataLoader
{
    public bool ParseLocalizeSheet(ISheet sheet)
    {
        // temp charset
        Dictionary<int, string> charsetIndex = new Dictionary<int, string>();
        
        // 1. Subject 파싱
        
        // Subject Row를 가져온다.
        IRow row = sheet.GetRow(Program.Settings.LocalizeSubjectRow);
        
        // Subject Row가 없음 종료 (데이터 없음)
        if(IsEmptyRow(row)) return true;

        foreach (ICell cell in row.Cells)
        {
            if(IsEmptyCell(cell)) continue;

            string cellName = cell.ToString();

            // Index 체크
            if (cell.ColumnIndex == Program.Settings.IndexCell)
            {
                
                if (string.Compare(cellName, Program.Settings.IndexSubjectName, true) != 0)
                {
                    Program.LogError($"Index가 {Program.Settings.IndexCell}번째 컬럼이 아닙니다.", cell.Address.ToString());
                    return false;
                }
            }
            // 나머지 행은 charsetIndex 에서 인덱스 저장
            else
            {
                // 대문자로 가자
                charsetIndex[cell.ColumnIndex] = cell.ToString().ToUpper();
            }
        }

        
        LocalizeSheet localizeSheet = new LocalizeSheet();
        localizeSheet.LocalizeInfos = new Dictionary<int, LocalizeInfo>();
        var rowCount = GetValidRowCount(sheet, Program.Settings.IndexSubjectName, Program.Settings.LocalizeSubjectRow, Program.Settings.LocalizeDataRow);

        for (var i = Program.Settings.LocalizeDataRow ; i < rowCount+Program.Settings.LocalizeDataRow ; ++i)
        {
            row = sheet.GetRow(i);
            
            if(IsEmptyRow(row)) break;

            ICell indexCell = row.GetCell(Program.Settings.IndexCell);
            if (indexCell == null)
            {
                Program.LogError("인덱스 컬럼를 읽을 수 없습니다.", GetCellAddress(Program.Settings.IndexCell, row.RowNum), FileName, sheet.SheetName);
                return false;
            }
            
            // 데이터 없으면 종료
            if (indexCell.CellType == CellType.Blank || indexCell.ToString().Length == 0)
            {
                break;
            }
            
            int index = ConvertType(TYPE_INT, indexCell);
            
            if (localizeSheet.LocalizeInfos.ContainsKey(index))
            {
                Program.LogError($"중복된 Index가 존재합니다. Index:{index}", indexCell.Address.ToString(), FileName, sheet.SheetName);
                return false;
            }

            LocalizeInfo localizeInfo = new LocalizeInfo();
            localizeInfo.Localizes = new List<Localize>();

            // 각 CharSet 별로 읽는다.
            ICell charCell;
            
            foreach (var charset in charsetIndex)
            {
                charCell = row.GetCell(charset.Key);
                
                if (charCell == null)
                {
                    // 빈 셀일때 못가져오는 경우가 있다.
                    // 어짜피 빈거니까 걍 넘어가주자
                    continue;
                }
                Localize localize = new Localize();
                localize.Charset = charset.Value;
                localize.Value = GetCellString(charCell);
                localizeInfo.Localizes.Add(localize);
            }
            
            localizeSheet.LocalizeInfos[index] = localizeInfo;
        }

        LocalizeSheets[sheet.SheetName] = localizeSheet;

        return true;
    }
    
    public List<Localize> GetLocalizeList(string sheetName, int index)
    {
        LocalizeSheet localizeSheet;
        if (LocalizeSheets.TryGetValue(sheetName, out localizeSheet) == false)
            return null;

        LocalizeInfo localizeInfo;
        if (localizeSheet.LocalizeInfos.TryGetValue(index, out localizeInfo) == false)
            return null;

        return localizeInfo.Localizes;
    }
}
