using UnityEngine;

public class JumpKingCamera : MonoBehaviour
{
    [Header("1. 해상도(줌) 설정")]
    public float targetWidthPixels = 1080f;
    public float ppu = 100f;

    [Header("2. 스크롤 추적 설정")]
    public Transform target;
    public float smoothSpeed = 10f;     // 화면 전환 속도
    public float yOffset = 0f;          // 전체 맵의 Y축 보정값

    [Header("3. 비율 강제 고정")]
    public bool useFixedRatio = true;
    public Vector2 targetRatio = new Vector2(9, 16);

    [Header("4. 벽 설정")]
    public bool createWalls = true;
    public float wallThickness = 1f;
    public PhysicsMaterial2D wallMaterial;

    private Camera cam;
    private bool isOverridden = false; 
    private Vector3 overridePosition;  
    private GameObject leftWallObj;    
    private GameObject rightWallObj;
    private float screenHeightWorld; // 카메라가 비추는 화면의 세로 높이

    void Awake()
    {
        cam = GetComponent<Camera>();

        // 비율 강제 고정
        float intendedAspect = targetRatio.x / targetRatio.y;
        if (useFixedRatio)
        {
            float windowAspect = (float)Screen.width / (float)Screen.height;
            float scaleHeight = windowAspect / intendedAspect;
            Rect rect = cam.rect;

            if (scaleHeight < 1.0f)
            {
                rect.width = 1.0f; rect.height = scaleHeight;
                rect.x = 0; rect.y = (1.0f - scaleHeight) / 2.0f;
            }
            else
            {
                float scaleWidth = 1.0f / scaleHeight;
                rect.width = scaleWidth; rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f; rect.y = 0;
            }
            cam.rect = rect;
        }

        // 줌 설정
        float targetWidthUnit = targetWidthPixels / ppu;
        float calcAspect = useFixedRatio ? intendedAspect : cam.aspect;
        cam.orthographicSize = (targetWidthUnit / calcAspect) / 2f;
        
        // [핵심] 화면의 세로 크기 계산 (오소그래픽 사이즈 * 2)
        screenHeightWorld = cam.orthographicSize * 2f;
    }

    void Start()
    {
        if (createWalls) CreateBorders();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition;

        if (isOverridden)
        {
            // 숨겨진 방 모드
            targetPosition = new Vector3(overridePosition.x, overridePosition.y, -10f);
        }
        else
        {
            // [스크롤 모드 복구] 
            // 플레이어의 Y 위치를 화면 높이로 나누어 '몇 번째 방'인지 계산
            // Mathf.RoundToInt를 쓰면 반올림되어 방의 중앙을 기준으로 전환됨
            // 0층, 1층, 2층... 식으로 계산
            
            float currentRoomIndex = Mathf.Round((target.position.y - yOffset) / screenHeightWorld);
            float targetY = (currentRoomIndex * screenHeightWorld) + yOffset;

            targetPosition = new Vector3(transform.position.x, targetY, -10f);
        }

        // 부드럽게 스크롤 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }

    void CreateBorders()
    {
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        
        leftWallObj = CreateWall("LeftWall", new Vector2(-width / 2 - wallThickness / 2, 0), new Vector2(wallThickness, height * 10f));
        rightWallObj = CreateWall("RightWall", new Vector2(width / 2 + wallThickness / 2, 0), new Vector2(wallThickness, height * 10f));
    }

    GameObject CreateWall(string name, Vector2 localPos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = transform;
        wall.transform.localPosition = localPos;
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        if (wallMaterial != null) col.sharedMaterial = wallMaterial;
        return wall;
    }

    public void EnterHiddenZone(Vector3 newCameraPos, bool disableLeftWall)
    {
        isOverridden = true;
        overridePosition = new Vector3(newCameraPos.x, newCameraPos.y, -10f);
        if (disableLeftWall && leftWallObj != null) leftWallObj.SetActive(false);
    }

    public void ExitHiddenZone()
    {
        isOverridden = false;
        if (leftWallObj != null) leftWallObj.SetActive(true);
    }
}