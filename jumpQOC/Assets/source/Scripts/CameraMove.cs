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
    public Vector2 targetRatio = new Vector2(9, 16); // 예: 9, 16

    [Header("5. 벽 생성")]
    public bool createWalls = true;
    public float wallThickness = 1f;
    public PhysicsMaterial2D wallMaterial;

    private Camera cam;
    private float screenHeight;   
    private Vector3 originPos;    

    void Awake()
    {
        cam = GetComponent<Camera>();

        // [수정 핵심] 화면 비율(Aspect)을 기기 해상도가 아니라, 우리가 정한 비율로 강제 계산
        // 이렇게 해야 에디터에서 창 크기를 맘대로 바꿔도 게임 내 1층 높이가 일정하게 유지됨
        float intendedAspect = targetRatio.x / targetRatio.y;
        
        // 비율 고정 로직 (레터박스)
        if (useFixedRatio)
        {
            float windowAspect = (float)Screen.width / (float)Screen.height;
            float scaleHeight = windowAspect / intendedAspect;

            Rect rect = cam.rect;

            if (scaleHeight < 1.0f) // 위아래 여백
            {
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;
            }
            else // 좌우 여백
            {
                float scaleWidth = 1.0f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;
            }
            cam.rect = rect;
        }

        // [수정 핵심] 줌 크기 계산 시에도 'intendedAspect'를 사용
        // 기존 cam.aspect를 쓰면 레터박스 적용 전/후 타이밍 이슈로 값이 튀는 현상 방지
        float targetWidthUnit = targetWidthPixels / ppu;
        
        // Orthographic Size = (가로폭 / 비율) / 2
        // useFixedRatio가 켜져있으면 우리가 정한 비율로, 꺼져있으면 현재 화면 비율로 계산
        float calcAspect = useFixedRatio ? intendedAspect : cam.aspect;
        cam.orthographicSize = (targetWidthUnit / calcAspect) / 2f;
        
        screenHeight = cam.orthographicSize * 2f;
    }

    void Start()
    {
        // 바닥 위치 보정
        float cameraHalfHeight = cam.orthographicSize;
        float correctedY = mapBottomY + cameraHalfHeight;

        // 시작 위치를 강제로 0층(바닥)에 맞춤
        transform.position = new Vector3(transform.position.x, correctedY, -10);
        originPos = transform.position;

        if (createWalls) CreateBorders();
    }

    void LateUpdate()
    {
        if (target == null) return;

        float bottomLimit = originPos.y - cam.orthographicSize; 
        float playerHeightFromBottom = target.position.y - bottomLimit;
        
        // 혹시 screenHeight가 0이 되는 오류 방지
        if (screenHeight < 0.001f) screenHeight = 1f;

        int currentScreenIndex = Mathf.FloorToInt(playerHeightFromBottom / screenHeight);
        if (currentScreenIndex < 0) currentScreenIndex = 0;

        Vector3 targetPos = new Vector3(originPos.x, originPos.y + (currentScreenIndex * screenHeight), -10);
        transform.position = Vector3.Lerp(transform.position, targetPos, transitionSpeed * Time.deltaTime);
    }
    
    // ... 벽 생성 코드는 기존과 동일 (생략 가능, 그대로 두세요) ...
    void CreateBorders()
    {
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect; // 여기선 cam.aspect 써도 됨 (Awake 후라 안전)
        CreateWall("LeftWall", new Vector2(-width / 2 - wallThickness / 2, 0), new Vector2(wallThickness, height));
        CreateWall("RightWall", new Vector2(width / 2 + wallThickness / 2, 0), new Vector2(wallThickness, height));
    }

    void CreateWall(string name, Vector2 localPos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = transform;
        wall.transform.localPosition = localPos;
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        if (wallMaterial != null) col.sharedMaterial = wallMaterial;
    }
}