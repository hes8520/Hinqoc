using UnityEngine;

public class HiddenZoneTrigger : MonoBehaviour
{
    [Header("연결 정보")]
    public JumpKingCamera cameraScript;
    public Transform hiddenZoneCameraPoint;

    [Header("설정")]
    public bool isEntrance = true; // 체크=입구, 해제=출구
    public bool openLeftWall = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            // [핵심 수정] 이동 방향을 체크합니다!
            // Unity 6 이상이면 linearVelocity, 구버전이면 velocity 사용
            // 사용자의 PlayerController에 linearVelocity가 쓰였으므로 그대로 씁니다.
            float moveX = rb.linearVelocity.x; 

            if (isEntrance)
            {
                // 입구: 플레이어가 '왼쪽'으로 가고 있어야 함 (moveX < -0.1f)
                if (moveX < -0.1f) 
                {
                    Debug.Log("<< 방으로 입장 (왼쪽 이동 중)");
                    cameraScript.EnterHiddenZone(hiddenZoneCameraPoint.position, openLeftWall);
                }
            }
            else
            {
                // 출구: 플레이어가 '오른쪽'으로 가고 있어야 함 (moveX > 0.1f)
                if (moveX > 0.1f)
                {
                    Debug.Log(">> 방에서 퇴장 (오른쪽 이동 중)");
                    cameraScript.ExitHiddenZone();
                }
            }
        }
    }
}