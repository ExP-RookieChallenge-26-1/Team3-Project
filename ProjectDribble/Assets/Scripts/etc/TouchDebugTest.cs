using UnityEngine;

public class TouchDebugTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Debug.Log("Touch detected: " + Input.GetTouch(0).position);
        }

        if (Input.GetMouseButton(0))
        {
            Debug.Log("Mouse detected: " + Input.mousePosition);
        }
    }
}