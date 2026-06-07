using System;
using System.Collections.Generic;
using System.IO;

namespace Tool
{
    public static class Program
    {
        private const string SettingFileName = "Settings.ini";

        // 배치파일 처리를 위한 리턴값
        private const int SUCCESS = 0;
        private const int FAIL = 1;

        private static Dictionary<string, DataLoader> _dataLoaders = new Dictionary<string, DataLoader>();

        public static ToolSettings Settings { private set; get; }
        public static EnumRepository EnumRepository { private set; get; } = new EnumRepository();

        public static DataLoader GetDataLoader(string name)
        {
            if (_dataLoaders.ContainsKey(name) == false) return null;
            return _dataLoaders[name];
        }

        private static int Main(string[] args)
        {
            CommandLineOptions options;
            if (CommandLineOptions.TryParse(args, out options) == false)
            {
                PrintUsage();
                return FAIL;
            }

            if (options.ShowHelp)
            {
                PrintUsage();
                return SUCCESS;
            }

            string settingsPath = ResolveSettingsPath(options.SettingsPath);

            try
            {
                LogMSG("=================================================================================================");
                LogMSG("Table to JSON");
                LogMSG("-------------------------------------------------------------------------------------------------");

                ToolSettings settings;
                if (ToolSettings.TryLoad(settingsPath, options.ToSettingsOverrides(), out settings) == false)
                {
                    return FAIL;
                }

                Settings = settings;

                EnumRepository = new EnumRepository();
                if (EnumRepository.Load(settings.EnumExcelPath) == false)
                {
                    return FAIL;
                }

                var fileNameList = new List<string>();
                if (ReadTableFiles(settings, fileNameList) == false)
                {
                    return FAIL;
                }

                if (fileNameList.Count == 0)
                {
                    LogError("변환할 .xlsx 파일이 없습니다.");
                    return FAIL;
                }

                LogMSG("-------------------------------------------------------------------------------------------------");
                LogMSG($"{fileNameList.Count}개의 엑셀 파일을 변환합니다.");
                foreach (string filename in fileNameList)
                {
                    LogMSG($"[{filename}] 변환");
                    _dataLoaders[filename] = new DataLoader(filename);
                    if (_dataLoaders[filename].Start(filename) == false)
                    {
                        return FAIL;
                    }
                }

                LogMSG("-------------------------------------------------------------------------------------------------");
                LogMSG("참조 Index를 검사합니다.");
                foreach (string filename in fileNameList)
                {
                    LogMSG($"[{filename}] 참조 Index 검사");
                    // 레퍼런스 체크는 프로그램 중지는 하지 않는다.
                    _dataLoaders[filename].CheckReference();
                }

                LogMSG("-------------------------------------------------------------------------------------------------");
                LogMSG($"{fileNameList.Count}개의 JSON 파일을 저장합니다.");
                foreach (string filename in fileNameList)
                {
                    if (_dataLoaders[filename].SaveJSON() == false)
                    {
                        return FAIL;
                    }
                }

                LogMSG("-------------------------------------------------------------------------------------------------");
                LogMSG($"{fileNameList.Count}개 변환/저장 완료");
                LogMSG("=================================================================================================");
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return FAIL;
            }

            return SUCCESS;
        }

        private static string ResolveSettingsPath(string settingsPath)
        {
            if (string.IsNullOrWhiteSpace(settingsPath))
            {
                settingsPath = SettingFileName;
            }

            if (Path.IsPathRooted(settingsPath))
            {
                return Path.GetFullPath(settingsPath);
            }

            string currentDirectoryPath = Path.GetFullPath(settingsPath);
            if (File.Exists(currentDirectoryPath))
            {
                return currentDirectoryPath;
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, settingsPath));
        }

        private static void PrintUsage()
        {
            LogMSG("사용법:");
            LogMSG("  ExcelToJson.exe");
            LogMSG("  ExcelToJson.exe Settings.ini");
            LogMSG("  ExcelToJson.exe --settings Settings.ini --output .\\Json");
            LogMSG("  ExcelToJson.exe --settings Settings.ini --excel . --enum Enum.xlsx --output .\\Json");
            LogMSG("");
            LogMSG("옵션:");
            LogMSG("  --settings, -s   Settings 파일 경로");
            LogMSG("  --excel, --table Excel 테이블 폴더 경로");
            LogMSG("  --enum           Enum Excel 파일 경로");
            LogMSG("  --output, -o     JSON 출력 폴더 경로");
            LogMSG("  --help, -h       도움말 출력");
        }

        private static bool ReadTableFiles(ToolSettings settings, List<string> filenameList)
        {
            if (false == Directory.Exists(settings.ExcelPath))
            {
                LogError($"엑셀 경로를 찾을 수 없습니다. - {settings.ExcelPath}");
                return false;
            }

            string enumFileName = Path.GetFileNameWithoutExtension(settings.EnumExcelPath);
            string[] files = Directory.GetFiles(settings.ExcelPath, "*.xlsx");

            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                if (filename.Contains("~$")) continue;

                string tableName = Path.GetFileNameWithoutExtension(file);
                if (string.Equals(tableName, enumFileName, StringComparison.OrdinalIgnoreCase)) continue;
                if (settings.IsExcludedTable(tableName)) continue;
                if (settings.IsExcludedTable(filename)) continue;

                filenameList.Add(tableName);
            }

            return true;
        }

        public static void LogMSG(string logString, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(logString);
            Console.ResetColor();
        }

        public static void LogWarning(string logString, string address = "", string fileName = "", string sheetname = "")
        {
            string msg = "[Warning] " + logString;
            if (address.Length > 0) msg += $" - Address [{address}]";
            if (fileName.Length > 0) msg += $" / {fileName}";
            if (sheetname.Length > 0) msg += $" - {sheetname}";

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void LogError(string logString, string address = "", string fileName = "", string sheetname = "")
        {
            string msg = "[ERROR] " + logString;
            if (address.Length > 0) msg += $" - Address [{address}]";
            if (fileName.Length > 0) msg += $" / {fileName}";
            if (sheetname.Length > 0) msg += $" - {sheetname}";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        private class CommandLineOptions
        {
            public string SettingsPath;
            public string ExcelPath;
            public string OutputPath;
            public string EnumExcelPath;
            public bool ShowHelp;

            public ToolSettingsOverrides ToSettingsOverrides()
            {
                return new ToolSettingsOverrides
                {
                    ExcelPath = ExcelPath,
                    OutputPath = OutputPath,
                    EnumExcelPath = EnumExcelPath
                };
            }

            public static bool TryParse(string[] args, out CommandLineOptions options)
            {
                options = new CommandLineOptions();
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (IsHelpOption(arg))
                    {
                        options.ShowHelp = true;
                        return true;
                    }

                    if (IsSettingsOption(arg))
                    {
                        if (ReadOptionValue(args, ref i, out options.SettingsPath) == false) return false;
                    }
                    else if (IsExcelPathOption(arg))
                    {
                        if (ReadOptionValue(args, ref i, out options.ExcelPath) == false) return false;
                    }
                    else if (IsEnumPathOption(arg))
                    {
                        if (ReadOptionValue(args, ref i, out options.EnumExcelPath) == false) return false;
                    }
                    else if (IsOutputPathOption(arg))
                    {
                        if (ReadOptionValue(args, ref i, out options.OutputPath) == false) return false;
                    }
                    else if (arg.StartsWith("-"))
                    {
                        LogError($"알 수 없는 옵션입니다. - {arg}");
                        return false;
                    }
                    else if (string.IsNullOrWhiteSpace(options.SettingsPath))
                    {
                        options.SettingsPath = arg;
                    }
                    else
                    {
                        LogError($"알 수 없는 인자입니다. - {arg}");
                        return false;
                    }
                }

                return true;
            }

            private static bool ReadOptionValue(string[] args, ref int index, out string value)
            {
                value = "";
                if (index + 1 >= args.Length)
                {
                    LogError($"옵션 값이 없습니다. - {args[index]}");
                    return false;
                }

                value = args[++index];
                if (value.StartsWith("-"))
                {
                    LogError($"옵션 값이 올바르지 않습니다. - {args[index - 1]}");
                    return false;
                }

                return true;
            }

            private static bool IsHelpOption(string arg)
            {
                return string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsSettingsOption(string arg)
            {
                return string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(arg, "-s", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsExcelPathOption(string arg)
            {
                return string.Equals(arg, "--excel", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(arg, "--table", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsEnumPathOption(string arg)
            {
                return string.Equals(arg, "--enum", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(arg, "--enum-excel", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsOutputPathOption(string arg)
            {
                return string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(arg, "-o", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
