using System;
using System.Collections.Generic;
using System.IO;

namespace Tool
{
    public class ToolSettingsOverrides
    {
        public string ExcelPath { get; set; }
        public string OutputPath { get; set; }
        public string EnumExcelPath { get; set; }
    }

    public class ToolSettings
    {
        // 파일 경로
        public string ExcelPath { get; private set; }
        public string OutputPath { get; private set; }
        public string EnumExcelPath { get; private set; }

        public string IndexSubjectName { get; private set; }
        public string GroupSubjectName { get; private set; }
        public int IndexCell { get; private set; }

        public string DataSheetName { get; private set; }

        public int DataSubjectRow { get; private set; }
        public int DataReferenceRow { get; private set; }
        public int DataTypeRow { get; private set; }
        public int DataDataRow { get; private set; }

        public int LocalizeSubjectRow { get; private set; }
        public int LocalizeDataRow { get; private set; }

        public int EnumNameRow { get; private set; }
        public int EnumDataRow { get; private set; }

        private readonly HashSet<string> excludedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, string>> sections =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);

        private const int DefaultIndexCell = 0;
        private const int DefaultDataSubjectRow = 1;
        private const int DefaultDataReferenceRow = 2;
        private const int DefaultDataTypeRow = 3;
        private const int DefaultDataDataRow = 4;
        private const int DefaultEnumNameRow = 0;
        private const int DefaultEnumDataRow = 1;

        public static bool TryLoad(string path, out ToolSettings settings)
        {
            return TryLoad(path, null, out settings);
        }

        public static bool TryLoad(string path, ToolSettingsOverrides overrides, out ToolSettings settings)
        {
            settings = null;

            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath) == false)
            {
                Program.LogError($"Settings 파일을 읽을 수 없습니다. - {fullPath}");
                return false;
            }

            Program.LogMSG($"Settings 파일을 읽는중... {fullPath}");

            var loaded = new ToolSettings();
            loaded.LoadSettingsFile(fullPath);

            string settingsDirectory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();

            loaded.ExcelPath = ResolveDirectoryPath(
                loaded.GetFirstValue("Path", "ExcelPath", "TablePath"),
                settingsDirectory);
            loaded.OutputPath = ResolveDirectoryPath(
                loaded.GetFirstValue("Path", "OutputPath", "JsonOutputPath", "TableOutputPath"),
                settingsDirectory);

            string enumExcel = loaded.GetFirstValue("Path", "EnumExcelPath", "EnumExcel", "EnumFile");
            if (enumExcel.Length == 0)
            {
                enumExcel = loaded.GetFirstValue("Enum", "ExcelPath", "FileName", "File");
            }
            loaded.EnumExcelPath = ResolveEnumExcelPath(enumExcel, loaded.ExcelPath, settingsDirectory);

            loaded.IndexSubjectName = loaded.GetFirstValue("Setting", "IndexSubjectName");
            loaded.GroupSubjectName = loaded.GetFirstValue("Setting", "GroupSubjectName");
            loaded.IndexCell = loaded.ReadInt("Setting", "IndexCell", DefaultIndexCell);

            loaded.DataSheetName = loaded.GetFirstValue("Sheet", "DataSheetName", "DataSheet");

            loaded.DataSubjectRow = loaded.ReadInt("Data", "SubjectRow", DefaultDataSubjectRow);
            loaded.DataReferenceRow = loaded.ReadInt("Data", "ReferenceRow", DefaultDataReferenceRow);
            loaded.DataTypeRow = loaded.ReadInt("Data", "TypeRow", DefaultDataTypeRow);
            loaded.DataDataRow = loaded.ReadInt("Data", "DataRow", DefaultDataDataRow);

            loaded.LocalizeSubjectRow = loaded.ReadInt("Localize", "SubjectRow", 0);
            loaded.LocalizeDataRow = loaded.ReadInt("Localize", "DataRow", 1);

            loaded.EnumNameRow = loaded.ReadInt("Enum", "NameRow", DefaultEnumNameRow);
            loaded.EnumDataRow = loaded.ReadInt("Enum", "DataRow", DefaultEnumDataRow);

            loaded.LoadExcludedTables();
            loaded.ApplyOverrides(overrides);

            if (loaded.Validate() == false)
            {
                return false;
            }

            settings = loaded;
            return true;
        }

        private void ApplyOverrides(ToolSettingsOverrides overrides)
        {
            if (overrides == null) return;

            string argumentDirectory = Directory.GetCurrentDirectory();
            if (string.IsNullOrWhiteSpace(overrides.ExcelPath) == false)
            {
                ExcelPath = ResolveDirectoryPath(overrides.ExcelPath, argumentDirectory);
            }

            if (string.IsNullOrWhiteSpace(overrides.OutputPath) == false)
            {
                OutputPath = ResolveDirectoryPath(overrides.OutputPath, argumentDirectory);
            }

            if (string.IsNullOrWhiteSpace(overrides.EnumExcelPath) == false)
            {
                EnumExcelPath = ResolveEnumExcelPath(overrides.EnumExcelPath, ExcelPath, argumentDirectory);
            }
        }

        public bool IsExcludedTable(string name)
        {
            string withoutExtension = Path.GetFileNameWithoutExtension(name);
            return excludedTables.Contains(name) || excludedTables.Contains(withoutExtension);
        }

        private bool Validate()
        {
            if (ExcelPath.Length == 0)
            {
                Program.LogError("[Path] ExcelPath 또는 TablePath가 필요합니다.");
                return false;
            }

            if (OutputPath.Length == 0)
            {
                Program.LogError("[Path] OutputPath가 필요합니다.");
                return false;
            }

            if (EnumExcelPath.Length == 0)
            {
                Program.LogError("[Path] EnumExcelPath 또는 EnumExcel이 필요합니다.");
                return false;
            }

            if (DataSheetName.Length == 0)
            {
                Program.LogError("[Sheet] DataSheetName이 필요합니다.");
                return false;
            }

            if (IndexSubjectName.Length == 0)
            {
                Program.LogError("[Setting] IndexSubjectName이 필요합니다.");
                return false;
            }

            return true;
        }

        private void LoadExcludedTables()
        {
            AddExcludedTableNames(GetFirstValue("Exclude", "Files", "Tables"));

            foreach (string key in GetKeys("Exclude"))
            {
                if (string.Equals(key, "Files", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(key, "Tables", StringComparison.OrdinalIgnoreCase)) continue;

                string value = GetValue(key, "Exclude");
                if (IsTrue(value))
                {
                    AddExcludedTableNames(key);
                }
                else
                {
                    AddExcludedTableNames(value);
                }
            }
        }

        private void AddExcludedTableNames(string rawValue)
        {
            if (rawValue == null || rawValue.Length == 0) return;

            string[] tokens = rawValue.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string name = token.Trim();
                if (name.Length == 0) continue;

                excludedTables.Add(name);
                excludedTables.Add(Path.GetFileNameWithoutExtension(name));
            }
        }

        private string GetFirstValue(string section, params string[] keys)
        {
            foreach (string key in keys)
            {
                string value = GetValue(key, section);
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    return value.Trim();
                }
            }

            return "";
        }

        private int ReadInt(string section, string key, int defaultValue)
        {
            string value = GetValue(key, section);
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            int result;
            if (int.TryParse(value.Trim(), out result))
            {
                return result;
            }

            Program.LogWarning($"정수 설정값을 읽을 수 없어 기본값을 사용합니다. {section}.{key}={value}");
            return defaultValue;
        }

        private static string ResolveDirectoryPath(string path, string settingsDirectory)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
            return Path.GetFullPath(Path.Combine(settingsDirectory, path));
        }

        private static string ResolveEnumExcelPath(string path, string excelPath, string settingsDirectory)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            if (Path.IsPathRooted(path)) return Path.GetFullPath(path);

            string excelRelativePath = Path.GetFullPath(Path.Combine(excelPath, path));
            if (File.Exists(excelRelativePath)) return excelRelativePath;

            return Path.GetFullPath(Path.Combine(settingsDirectory, path));
        }

        private static bool IsTrue(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "y", StringComparison.OrdinalIgnoreCase);
        }

        private string GetValue(string key, string section)
        {
            Dictionary<string, string> values;
            if (sections.TryGetValue(section, out values) == false)
            {
                return "";
            }

            string value;
            if (values.TryGetValue(key, out value) == false)
            {
                return "";
            }

            return value;
        }

        private string[] GetKeys(string section)
        {
            Dictionary<string, string> values;
            if (sections.TryGetValue(section, out values) == false)
            {
                return new string[0];
            }

            string[] keys = new string[values.Keys.Count];
            values.Keys.CopyTo(keys, 0);
            return keys;
        }

        private void LoadSettingsFile(string filePath)
        {
            sections.Clear();

            Dictionary<string, string> currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            sections[""] = currentSection;

            string[] lines = File.ReadAllLines(filePath);
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(";"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string sectionName = line.Substring(1, line.Length - 2).Trim();
                    currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    sections[sectionName] = currentSection;
                    continue;
                }

                int separatorIndex = line.IndexOf("=");
                if (separatorIndex < 0)
                {
                    currentSection[line] = "";
                    continue;
                }

                string key = line.Substring(0, separatorIndex).Trim();
                string value = line.Substring(separatorIndex + 1).Trim();
                currentSection[key] = value;
            }
        }
    }
}
