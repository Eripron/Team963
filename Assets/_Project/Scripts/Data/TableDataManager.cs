using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace Team963
{
    public class TableDataManager : MonoBehaviour
    {
        private const string TableResourcePath = "Tables";

        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                IncludeFields = true
            };

        // 새 테이블을 만들면 이 목록에 타입만 한 줄 추가합니다.
        private static readonly Type[] TableTypes =
        {
            typeof(ItemInfoTable),
        };

        private readonly Dictionary<Type, ITable> _tablesByType = new Dictionary<Type, ITable>();
        private bool _isLoaded;


        public void OnLoadTableData()
        {
            if (_isLoaded)
                return;

            try
            {
                CreateTables();     // 테이블 생성
                LoadTableData();    // 테이블 데이터 로드
                AfterLoadTableData();   // 테이블 데이터 후처리

                _isLoaded = true;
                Debug.Log("테이블 데이터 로딩을 완료했습니다.");
            }
            catch (Exception exception)
            {
                _tablesByType.Clear();
                _isLoaded = false;
                Debug.LogError($"테이블 데이터 로딩에 실패했습니다.\n{exception}");
            }
        }

        private void CreateTables()
        {
            _tablesByType.Clear();

            foreach (Type tableType in TableTypes)
            {
                if (!typeof(ITable).IsAssignableFrom(tableType) || tableType.IsAbstract)
                    throw new InvalidOperationException($"ITable을 구현한 구체 테이블 타입만 등록할 수 있습니다. TableType: {tableType.FullName}");

                if (Activator.CreateInstance(tableType) is not ITable table)
                    throw new InvalidOperationException($"테이블을 생성하지 못했습니다. 매개변수 없는 생성자가 필요합니다. TableType: {tableType.FullName}");

                if (_tablesByType.ContainsKey(tableType))
                    throw new InvalidOperationException($"같은 테이블 타입이 중복 등록되었습니다. TableType: {tableType.FullName}");

                _tablesByType.Add(tableType, table);
            }
        }

        private void LoadTableData()
        {
            foreach (Type tableType in TableTypes)
                LoadTable(_tablesByType[tableType]);
        }

        private void LoadTable(ITable table)
        {
            ValidateTableName(table.TableName);

            string resourcePath = $"{TableResourcePath}/{table.TableName}";
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

            if (jsonAsset == null)
                throw new InvalidOperationException($"테이블 JSON 파일을 찾을 수 없습니다. 경로: Resources/{resourcePath}.json");

            table.Parse(jsonAsset.text, JsonOptions);
        }

        private static void ValidateTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException("TableName은 비어 있을 수 없습니다.");

            if (tableName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"TableName에는 .json 확장자를 포함할 수 없습니다. TableName: {tableName}");
            }

            if (tableName.Contains("/") || tableName.Contains("\\"))
            {
                throw new InvalidOperationException($"TableName에는 경로를 포함할 수 없습니다. 파일 이름만 입력하세요. TableName: {tableName}");
            }
        }

        private void AfterLoadTableData()
        {
            foreach (Type tableType in TableTypes)
                _tablesByType[tableType].OnAfterLoaded();
        }

        public TTable GetTable<TTable>() where TTable : class, ITable
        {
            if (!_tablesByType.TryGetValue(typeof(TTable), out ITable table))
            {
                throw new InvalidOperationException($"등록되거나 로드되지 않은 테이블입니다. TableType: {typeof(TTable).Name}");
            }

            return (TTable)table;
        }
    }
}
