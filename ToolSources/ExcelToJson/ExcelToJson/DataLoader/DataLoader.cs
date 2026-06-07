using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;

namespace Tool
{
    public struct Data
    {
        public string Subject;      // 컬럼 이름(2행)
        public string Reference;    // refer table 이름
        public string Type;         // 데이터 타입(4행)
        public string columnAddress;    // 셀 주소
        public List<dynamic> DataList;  // 데이터
    }

    // 하나가 독립적인 문서
    public partial class DataLoader
    {
        public const string TYPE_BOOL = "bool";
        public const string TYPE_FLOAT = "float";
        public const string TYPE_DOUBLE = "double";
        public const string TYPE_INT = "int";
        public const string TYPE_STRING = "string";
        public const string TYPE_LOCALIZE = "localize";
        public const string TYPE_INT_ARRAY = "int[]";
        public const string TYPE_FLOAT_ARRAY = "float[]";
        public const string TYPE_DOUBLE_ARRAY = "double[]";
        public const string TYPE_STRING_ARRAY = "string[]";

        private string dataName;

        public string FileName { get; private set; }

        // localize 정보
        // key - sheet name
        // value - localize sheet datas
        public Dictionary<string, LocalizeSheet> LocalizeSheets;

        // datas (다른 시트 참조를 위해 일단 다 때려 넣어보자)
        // key - subject
        // value - data list
        public Dictionary<string, Data> DataInfo;

        public DataLoader(string filename)
        {
            FileName = filename;

            DataInfo = new Dictionary<string, Data>();
            LocalizeSheets = new Dictionary<string, LocalizeSheet>();
        }

        public bool Start(string dataName)
        {
            try
            {
                if (dataName.Length == 0)
                {
                    Program.LogError("엑셀 파일이 존재 하지 않습니다. - dataName");
                    return false;
                }

                this.dataName = dataName;

                string path = Path.Combine(Program.Settings.ExcelPath, $"{this.dataName}.xlsx");
                IWorkbook workbook = WorkbookLoader.LoadXlsx(path);

                // sheet별 파싱 (data 제외)
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    ISheet sheet = workbook.GetSheetAt(i);

                    if (sheet.SheetName.Contains("#"))
                    {
                        // nothing to do
                    }
                    else if (sheet.SheetName == Program.Settings.DataSheetName)
                    {
                        // Data Sheet는 제일 마지막에
                    }
                    else
                    {
                        //if (ParseLocalizeSheet(sheet) == false) return false;
                    }
                }

                // 다른 시트 다 하고 data sheet 파싱
                ISheet dataSheet = workbook.GetSheet(Program.Settings.DataSheetName);
                if (dataSheet == null)
                {
                    Program.LogError("Data Sheet가 존재하지 않습니다.", "", FileName);
                    return false;
                }

                if (ParseDataInfo(dataSheet) == false) return false;
            }
            catch (Exception ex)
            {
                Program.LogError($"Load {this.dataName} exception - {ex.Message}");
                Console.WriteLine($"Stacktrace - {ex.StackTrace}");
                return false;
            }

            return true;
        }
    }
}
