using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해 필수
using System.Collections;

public class TypewriterUI : MonoBehaviour
{
    public TMP_Text targetText; // 타자 칠 텍스트 컴포넌트
    public float typingSpeed = 0.1f; // 글자 나타나는 속도
    public float displayTime = 2.0f; // 다 나타난 후 유지되는 시간

    private Coroutine currentCoroutine;

    // 외부에서 이 함수를 호출하면 타자기 효과 시작
    public void ShowMessage(string message)
    {
        if (targetText == null) return;

        // 혹시 이전에 실행 중이던 코루틴이 있으면 멈춤
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        targetText.text = message;
        targetText.gameObject.SetActive(true); // 텍스트 오브젝트 켜기
        
        // 코루틴 시작
        currentCoroutine = StartCoroutine(TypeTextRoutine());
    }

    IEnumerator TypeTextRoutine()
    {
        // TMP의 기능을 이용해 글자를 하나씩 보이게 함
        targetText.maxVisibleCharacters = 0; // 처음엔 0글자만 보임
        int totalLength = targetText.text.Length;

        for (int i = 1; i <= totalLength; i++)
        {
            targetText.maxVisibleCharacters = i; // i번째 글자까지 보이게 설정
            yield return new WaitForSeconds(typingSpeed); // 대기
        }

        // 다 보여준 후 잠시 대기
        yield return new WaitForSeconds(displayTime);

        // 텍스트 끄기
        targetText.gameObject.SetActive(false);
        currentCoroutine = null;
    }
}