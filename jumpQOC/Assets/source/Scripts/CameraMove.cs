using UnityEngine;

public class JumpKingCamera : MonoBehaviour
{
    [Header("1. 해상도(줌) 설정")]
    public float targetWidthPixels = 1080f;
    public float ppu = 100f;

    [Header("2. 점프킹 이동 설정")]
    public Transform target;
    public float transitionSpeed = 10f;

    [Header("3. 바닥 높이 설정")]
    public float mapBottomY = 0f;

    [Header("4. 비율 강제 고정")]
    public bool useFixedRatio = true;
    public Vector2 targetRatio = new Vector2(9, 16);

    [Header("5. 벽 생성")]
    public bool createWalls = true;
    public float wallThickness = 1f;
    public PhysicsMaterial2D wallMaterial;

    private Camera cam;
    private float screenHeight;
    private Vector3 originPos;

    // --- [추가된 변수] ---
    private bool isOverridden = false; // 숨겨진 방 모드인지?
    private Vector3 overridePosition;  // 숨겨진 방 카메라 위치
    private GameObject leftWallObj;    // 왼쪽 벽 제어용
    private GameObject rightWallObj;   // 오른쪽 벽 제어용
    // -------------------

    void Awake()
    {
        cam = GetComponent<Camera>();

        // (기존 비율 계산 로직 동일)
        float intendedAspect = targetRatio.x / targetRatio.y;

        if (useFixedRatio)
        {
            float windowAspect = (float)Screen.width / (float)Screen.height;
            float scaleHeight = windowAspect / intendedAspect;

            Rect rect = cam.rect;

            if (scaleHeight < 1.0f)
            {
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;
            }
            else
            {
                float scaleWidth = 1.0f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;
            }
            cam.rect = rect;
        }

        float targetWidthUnit = targetWidthPixels / ppu;
        float calcAspect = useFixedRatio ? intendedAspect : cam.aspect;
        cam.orthographicSize = (targetWidthUnit / calcAspect) / 2f;

        screenHeight = cam.orthographicSize * 2f;
    }

    void Start()
    {
        float cameraHalfHeight = cam.orthographicSize;
        float correctedY = mapBottomY + cameraHalfHeight;

        transform.position = new Vector3(transform.position.x, correctedY, -10);
        originPos = transform.position;

        if (createWalls) CreateBorders();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos;

        // --- [수정된 로직] ---
        if (isOverridden)
        {
            // 1. 숨겨진 방 모드일 때: 설정된 오버라이드 위치로 이동
            targetPos = overridePosition;
        }
        else
        {
            // 2. 일반 모드일 때: 기존 수직 스크롤 로직 사용
            float bottomLimit = originPos.y - cam.orthographicSize;
            float playerHeightFromBottom = target.position.y - bottomLimit;

            if (screenHeight < 0.001f) screenHeight = 1f;

            int currentScreenIndex = Mathf.FloorToInt(playerHeightFromBottom / screenHeight);
            if (currentScreenIndex < 0) currentScreenIndex = 0;

            targetPos = new Vector3(originPos.x, originPos.y + (currentScreenIndex * screenHeight), -10);
        }
        // -------------------

        transform.position = Vector3.Lerp(transform.position, targetPos, transitionSpeed * Time.deltaTime);
    }

    void CreateBorders()
    {
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect; // Awake 후라 안전
        
        // 벽 객체를 변수에 저장해둡니다 (나중에 껐다 켰다 하기 위해)
        leftWallObj = CreateWall("LeftWall", new Vector2(-width / 2 - wallThickness / 2, 0), new Vector2(wallThickness, height));
        rightWallObj = CreateWall("RightWall", new Vector2(width / 2 + wallThickness / 2, 0), new Vector2(wallThickness, height));
    }

    GameObject CreateWall(string name, Vector2 localPos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = transform;
        wall.transform.localPosition = localPos;
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        if (wallMaterial != null) col.sharedMaterial = wallMaterial;
        
        return wall; // 생성된 벽 리턴
    }

    // --- [외부에서 호출할 함수들] ---
    
    // 숨겨진 방으로 들어갈 때 호출
    public void EnterHiddenZone(Vector3 newCameraPos, bool disableLeftWall)
    {
        isOverridden = true;
        overridePosition = new Vector3(newCameraPos.x, newCameraPos.y, -10); // Z축 고정

        // 왼쪽 벽을 통과해야 한다면 비활성화
        if (disableLeftWall && leftWallObj != null) leftWallObj.SetActive(false);
    }

    // 숨겨진 방에서 나올 때 호출
    public void ExitHiddenZone()
    {
        isOverridden = false;
        
        // 벽 다시 활성화
        if (leftWallObj != null) leftWallObj.SetActive(true);
    }
}