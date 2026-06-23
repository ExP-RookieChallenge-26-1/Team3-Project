using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraLetterBox : MonoBehaviour
{
    [SerializeField] private float targetWidth = 1080f;
    [SerializeField] private float targetHeight = 2160f;

    private Camera cam;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyLetterbox();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyLetterbox();
        }
    }

    private void ApplyLetterbox()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float targetAspect = targetWidth / targetHeight;
        float screenAspect = (float)Screen.width / Screen.height;

        Rect rect = new Rect(0f, 0f, 1f, 1f);

        if (screenAspect > targetAspect)
        {
            // 실제 화면이 기준보다 넓음
            // 좌우에 검은 여백
            float width = targetAspect / screenAspect;
            float x = (1f - width) / 2f;

            rect = new Rect(x, 0f, width, 1f);
        }
        else
        {
            // 실제 화면이 기준보다 좁거나 덜 김
            // 위아래에 검은 여백
            float height = screenAspect / targetAspect;
            float y = (1f - height) / 2f;

            rect = new Rect(0f, y, 1f, height);
        }

        cam.rect = rect;
    }
}