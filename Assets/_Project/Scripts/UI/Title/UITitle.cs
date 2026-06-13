using System.Collections;
using TMPro;
using UnityEngine;

public class UITitle : MonoBehaviour
{
    private const string LoadingText = "Loading";
    private const int LoadingDotCount = 4;
    private const float LoadingTextUpdateInterval = 0.2f;

    [SerializeField] private TextMeshProUGUI _textLoading;

    private Coroutine _loadingCoroutine;

    public void StartLoading()
    {
        if (_textLoading == null)
        {
            Debug.LogError("UITitle에 Loading Text가 연결되지 않았습니다.");
            return;
        }

        if (_loadingCoroutine != null)
            StopCoroutine(_loadingCoroutine);

        _loadingCoroutine = StartCoroutine(PlayLoadingAnimation());
    }

    private IEnumerator PlayLoadingAnimation()
    {
        int dotCount = 0;

        while (true)
        {
            _textLoading.text = LoadingText + new string('.', dotCount);
            dotCount = (dotCount + 1) % LoadingDotCount;

            yield return new WaitForSecondsRealtime(LoadingTextUpdateInterval);
        }
    }
}
