using UnityEngine;

namespace Team963
{
    public class TitleScene : MonoBehaviour
    {
        [SerializeField] private TableDataManager _tableDataManager;

        private void Start()
        {
            if (_tableDataManager == null)
            {
                Debug.LogError("TitleScene에 TableDataManager가 연결되지 않았습니다.");
                return;
            }

            _tableDataManager.OnLoadTableData();
        }
    }
}
