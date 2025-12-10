using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; 

    [Header("Camera Settings (Tweak in Play Mode)")]
    public float followSpeed = 10f; // High number = Snappy, Low = Smooth drift
    public float rotationSpeed = 10f; 

    [Header("Position & Angle")]
    // X = Left/Right shift, Y = Height, Z = Distance behind
    public Vector3 offset = new Vector3(0, 2.5f, -4.0f); 
    
    [Range(-60f, 60f)]
    public float viewAngle = 15f; // Positive = Look Down, Negative = Look Up

    void LateUpdate()
    {
        if (target == null) return;

        // "TransformPoint" converts the local offset to world space based on the Player's rotation.
        // This keeps the camera locked behind the player's back, even when gravity changes.
        Vector3 desiredPosition = target.TransformPoint(offset);

        // Match the player's rotation 
        Quaternion playerRotation = target.rotation;
        // Apply the view angle (pitch)
        Quaternion lookAngle = Quaternion.Euler(viewAngle, 0, 0);
        Quaternion desiredRotation = playerRotation * lookAngle;

        // Apply changes 
        float dt = Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * dt);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, rotationSpeed * dt);
    }
}