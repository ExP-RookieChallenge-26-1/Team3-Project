using UnityEngine;

namespace Interfaces
{
    public interface IBallReflector
    {
        Vector2 GetReflectDirection(
            BallController ball,
            RaycastHit2D hit,
            Vector2 incomingDirection
        );
    }
}