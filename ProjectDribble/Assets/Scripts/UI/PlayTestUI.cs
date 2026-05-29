using TMPro;
using UnityEngine;

public class PlayTestUI : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private BallMovement ballMovement;
    [SerializeField] private TMP_Text speedText;

    [Header("Display")]
    [SerializeField] private string prefix = "Speed: ";
    [SerializeField] private int decimalPlaces = 1;

    private void Update()
    {
        if (ballMovement == null || speedText == null)
            return;

        float speed = ballMovement.speed;

        speedText.text = prefix + speed.ToString($"F{decimalPlaces}");
    }
}