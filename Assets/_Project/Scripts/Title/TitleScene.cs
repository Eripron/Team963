using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Team963
{
    public class TitleScene : MonoBehaviour
    {
        private const string GameSceneName = "Game";
        private const float TitleDuration = 2f;

        [SerializeField] private TableDataManager _tableDataManager;
        [SerializeField] private UITitle _uiTitle;

        private IEnumerator Start()
        {
            if (_tableDataManager == null)
            {
                Debug.LogError("TitleScene에 TableDataManager가 연결되지 않았습니다.");
                yield break;
            }

            if (_uiTitle == null)
            {
                Debug.LogError("TitleScene에 UITitle이 연결되지 않았습니다.");
                yield break;
            }

            // 데이터 로드
            _tableDataManager.OnLoadTableData();

            if (!_tableDataManager.IsLoaded)
                yield break;

            // 타이틀 연출
            _uiTitle.StartLoading();
            yield return new WaitForSecondsRealtime(TitleDuration);

            // 게임 씬으로 이동
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
