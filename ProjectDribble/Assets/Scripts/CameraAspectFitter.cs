using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectFitter : MonoBehaviour
{
    [Header("Target Aspect")]
    [SerializeField] private float targetWidth = 5f;
    [SerializeField] private float targetHeight = 10f;

    private Camera cam;

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    private void Update()
    {
        if (Screen.width == lastScreenWidth &&
            Screen.height == lastScreenHeight)
            return;

        Apply();
    }

    private void Apply()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float targetAspect = targetWidth / targetHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        float baseOrthographicSize = targetHeight * 0.5f;

        if (currentAspect < targetAspect)
        {
            // 화면이 기준보다 더 좁음
            // 가로가 잘리지 않도록 세로 시야를 늘림
            cam.orthographicSize = baseOrthographicSize * (targetAspect / currentAspect);
        }
        else
        {
            // 화면이 기준보다 같거나 넓음
            // 기준 세로 유지
            cam.orthographicSize = baseOrthographicSize;
        }
    }
}