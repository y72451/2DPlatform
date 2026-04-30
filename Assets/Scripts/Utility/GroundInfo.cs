using UnityEngine;
using static UnityEngine.UI.Image;

public class GroundInfo
{
    public bool isGrounded = false;
    public float slopeAngle = 0f;
    public Vector2 slopeNormal = Vector2.up;
    public Vector2 slopeDirection = Vector2.zero;
    public Vector2 hitPoint = Vector2.zero;
}
public static class GroundDetector
{
    public static GroundInfo DecteGround(Transform origin, float checkDistance, LayerMask groundMask)
    {
        GroundInfo info = new GroundInfo();
        RaycastHit2D hit = Physics2D.Raycast(origin.position, Vector2.down, checkDistance, groundMask);
        Debug.DrawRay(origin.position, Vector2.down * checkDistance, hit.collider != null ? Color.green : Color.red);

        if (hit.collider != null)
        {
            info.isGrounded = true;
            info.hitPoint = hit.point;
            info.slopeNormal = hit.normal;
            info.slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            info.slopeDirection = Vector2.Perpendicular(hit.normal).normalized * -Mathf.Sign(hit.normal.x);
        }

        return info;
    }
}
