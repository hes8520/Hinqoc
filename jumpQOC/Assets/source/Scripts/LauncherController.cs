using UnityEngine;

public class LauncherController : MonoBehaviour
{
    [Header("1. 연결 필요")]
    public GameObject projectilePrefab; // 쪽파 프리팹
    public Transform firePoint;         // 발사 위치 (총구)

    [Header("2. 자동 발사 설정")]
    public float detectionRange = 10f;  // 플레이어 인식 범위 (반경)
    public float fireInterval = 1.5f;   // 발사 간격 (초 단위)
    
    // 내부 변수
    private Transform player;           // 플레이어 위치
    private float timer = 0f;           // 쿨타임 계산용

    void Start()
    {
        // 1. "Player" 태그가 붙은 오브젝트를 찾아서 타겟으로 설정
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("경고: 씬에 'Player' 태그를 가진 오브젝트가 없습니다! 자동 발사가 작동하지 않습니다.");
        }
    }

    void Update()
    {
        if (player == null) return;

        // 2. 거리 계산
        float distance = Vector2.Distance(transform.position, player.position);

        // 3. 사거리 안에 들어왔는지 확인
        if (distance <= detectionRange)
        {
            // 타이머 흐름
            timer += Time.deltaTime;

            // 발사 주기가 되었으면 발사
            if (timer >= fireInterval)
            {
                Fire();
                timer = 0f; // 타이머 초기화
            }
        }
        else
        {
            // 사거리를 벗어나면 타이머 초기화 (다시 들어오면 0초부터 시작)
            // (만약 들어오자마자 쏘게 하고 싶다면 timer = fireInterval; 로 변경하세요)
            timer = 0f; 
        }
    }

    void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;

        // 발사기가 바라보는 방향(FirePoint의 회전값) 그대로 날아감 = 정해진 궤도
        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }

    // 에디터에서 사거리를 눈으로 보여주는 기능 (게임 화면엔 안 보임)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}