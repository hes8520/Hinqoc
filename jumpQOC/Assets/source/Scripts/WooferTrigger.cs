using UnityEngine;

public class WooferTrigger : MonoBehaviour
{
    [Header("연결된 우퍼")]
    public SpeakerWaveEmitter targetWoofer; // 작동시킬 우퍼를 인스펙터에서 드래그

    [Header("발판 애니메이션 (선택)")]
    public Animator stepAnimator;
    public string stepTriggerName = "StepOn";

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 플레이어가 밟았는지 확인
        if (collision.gameObject.CompareTag("Player"))
        {
            // 2. 발판 자체 애니메이션 (있을 경우)
            if (stepAnimator != null)
            {
                stepAnimator.SetTrigger(stepTriggerName);
            }

            // 3. 연결된 우퍼에게 발사 신호 전달
            if (targetWoofer != null)
            {
                // 자신의 태그를 넘겨줌으로써 우퍼가 검사하게 함
                targetWoofer.TriggerByStep(gameObject.tag);
            }
        }
    }
}