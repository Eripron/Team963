using System;
using System.Globalization;
using System.IO;
using LitJson;

namespace Tool
{
    public partial class DataLoader
    {
        public bool SaveJSON()
        {
            if (DataInfo.Count == 0)
            {
                Program.LogError("저장할 데이터가 없습니다.", "", FileName);
                return false;
            }

            int dataCount = -1;
            foreach (var data in DataInfo)
            {
                int count = data.Value.DataList.Count;
                if (dataCount < 0)
                {
                    dataCount = count;
                    continue;
                }

                if (dataCount != count)
                {
                    Program.LogError($"컬럼별 데이터 개수가 다릅니다. Column:{data.Key}", "", FileName);
                    return false;
                }
            }

            var jsonW = new JsonWriter();
            jsonW.WriteArrayStart();

            for (int i = 0; i < dataCount; ++i)
            {
                jsonW.WriteObjectStart();

                foreach (var data in DataInfo)
                {
                    jsonW.WritePropertyName(data.Key);

                    switch (data.Value.Type)
                    {
                        case TYPE_LOCALIZE:
                            if (data.Value.DataList[i] == null)
                            {
                                jsonW.Write(null);
                            }
                            else
                            {
                                jsonW.WriteObjectStart();
                                foreach (Localize localize in data.Value.DataList[i])
                                {
                                    jsonW.WritePropertyName(localize.Charset);
                                    jsonW.Write(localize.Value);
                                }
                                jsonW.WriteObjectEnd();
                            }
                            break;

                        case TYPE_INT_ARRAY:
                        case TYPE_FLOAT_ARRAY:
                        case TYPE_DOUBLE_ARRAY:
                        case TYPE_STRING_ARRAY:
                            if (WriteArray(jsonW, data.Value, i) == false)
                            {
                                return false;
                            }
                            break;

                        default:
                            jsonW.Write(data.Value.DataList[i]);
                            break;
                    }
                }

                jsonW.WriteObjectEnd();
            }

            jsonW.WriteArrayEnd();

            var directoryInfo = new DirectoryInfo(Program.Settings.OutputPath);
            if (directoryInfo.Exists == false) directoryInfo.Create();

            string outputFilePath = Path.Combine(Program.Settings.OutputPath, $"{dataName}.json");
            using (var fs = new FileStream(outputFilePath, FileMode.Create))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(jsonW.ToString());
            }

            Program.LogMSG(outputFilePath);

            return true;
        }

        private bool WriteArray(JsonWriter jsonW, Data data, int dataIndex)
        {
            if (data.DataList[dataIndex] == null)
            {
                jsonW.WriteArrayStart();
                jsonW.WriteArrayEnd();
                return true;
            }

            string rawValue = data.DataList[dataIndex].ToString();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                jsonW.WriteArrayStart();
                jsonW.WriteArrayEnd();
                return true;
            }

            string type = NormalizeType(data.Type);
            string arrayText = rawValue.Trim();
            if (arrayText.StartsWith("[") && arrayText.EndsWith("]"))
            {
                arrayText = arrayText.Substring(1, arrayText.Length - 2).Trim();
            }

            jsonW.WriteArrayStart();

            if (arrayText.Length == 0)
            {
                jsonW.WriteArrayEnd();
                return true;
            }

            string[] values = arrayText.Split(',');
            foreach (string value in values)
            {
                string token = value.Trim();
                if (token.Length == 0)
                {
                    Program.LogError("Array 값이 비어 있습니다.", GetDataCellAddress(data, dataIndex), FileName, Program.Settings.DataSheetName);
                    return false;
                }

                if (type == TYPE_INT_ARRAY)
                {
                    int intValue;
                    if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue) == false)
                    {
                        Program.LogError($"Array int 값을 변환할 수 없습니다. Value:{token}", GetDataCellAddress(data, dataIndex), FileName, Program.Settings.DataSheetName);
                        return false;
                    }

                    jsonW.Write(intValue);
                }
                else if (type == TYPE_FLOAT_ARRAY || type == TYPE_DOUBLE_ARRAY)
                {
                    double doubleValue;
                    if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue) == false)
                    {
                        Program.LogError($"Array number 값을 변환할 수 없습니다. Value:{token}", GetDataCellAddress(data, dataIndex), FileName, Program.Settings.DataSheetName);
                        return false;
                    }

                    jsonW.Write(doubleValue);
                }
                else if (type == TYPE_STRING_ARRAY)
                {
                    jsonW.Write(UnwrapStringToken(token));
                }
            }

            jsonW.WriteArrayEnd();
            return true;
        }

        private string GetDataCellAddress(Data data, int dataIndex)
        {
            return data.columnAddress + (Program.Settings.DataDataRow + dataIndex + 1);
        }

        private string UnwrapStringToken(string token)
        {
            if (token.Length >= 2)
            {
                bool doubleQuoted = token[0] == '"' && token[token.Length - 1] == '"';
                bool singleQuoted = token[0] == '\'' && token[token.Length - 1] == '\'';
                if (doubleQuoted || singleQuoted)
                {
                    return token.Substring(1, token.Length - 2);
                }
            }

            return token;
        }
    }
}
