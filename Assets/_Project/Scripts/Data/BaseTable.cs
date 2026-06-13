using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Team963
{
    public interface ITable
    {
        public string TableName { get; }

        public void Parse(string json, JsonSerializerOptions options);

        public void OnAfterLoaded();
    }

    public abstract class BaseTable<TItem> : ITable 
        where TItem : BaseTableItem
    {
        public abstract string TableName { get; }
        
        private Dictionary<int, TItem> _items = new Dictionary<int, TItem>();


        public void Parse(string json, JsonSerializerOptions options)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new JsonException($"'{TableName}' 테이블의 JSON 내용이 비어 있습니다.");

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            TItem[] parsedItems = JsonSerializer.Deserialize<TItem[]>(json, options);

            if (parsedItems == null)
                throw new JsonException($"'{TableName}' 테이블 JSON을 역직렬화하지 못했습니다.");

            var itemsByIndex = new Dictionary<int, TItem>(parsedItems.Length);

            foreach (TItem item in parsedItems)
            {
                if (item == null)
                    throw new JsonException($"'{TableName}' 테이블에 null 데이터가 포함되어 있습니다.");

                if (!itemsByIndex.TryAdd(item.Index, item))
                    throw new InvalidOperationException($"'{TableName}' 테이블에 중복된 Index가 있습니다. Index: {item.Index}");
            }

            _items = itemsByIndex;
        }

        /// <summary>
        /// 모든 테이블 로드된 이후 후처리 필요한 경우 override해서 사용
        /// </summary>
        public virtual void OnAfterLoaded()
        {
            
        }

        public TItem GetItem(int index)
        {
            _items.TryGetValue(index, out TItem item);
            return item;
        }
    }
}
