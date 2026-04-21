using UnityEngine;
using System.Threading.Tasks;

// Vibe Code from Claude, prompt was something like "Make me a script
// with natural looking idle head and eye movement for a Unity character."

/// How it works:
///   - Picks a random "gaze target" point in a cone in front of the character
///   - Smoothly rotates the head toward it (slower, smaller range)
///   - Smoothly rotates the eyes toward it (faster, wider range)
///   - Holds gaze for a random duration before picking a new target
///   - Eyes lead the head slightly, then head catches up (more natural)
///   - Override option to look directly into the Camera
/// </summary>
public class IdleLookAround : MonoBehaviour
{
    [Header("Bones")]
    public Transform headBone;
    public Transform leftEyeBone;
    public Transform rightEyeBone;

    [Header("Gaze Target Range")]
    public float gazeDistance = 2.5f;
    public float horizontalRange = 30f;
    public float verticalRange = 15f;

    [Header("Head Movement")]
    [Range(0f, 1f)]
    public float headContribution = 0.35f;
    public float headSmoothSpeed = 2.5f;
    public float maxHeadYaw = 25f;
    public float maxHeadPitch = 15f;

    [Header("Eye Movement")]
    public float eyeSmoothSpeed = 8f;
    public float maxEyeYaw = 35f;
    public float maxEyePitch = 25f;

    [Header("Gaze Hold Duration")]
    public float minHoldTime = 1.2f;
    public float maxHoldTime = 4.0f;


    // ── Private state ────────────────────────────────────────────────────────
    private Vector3    _lookAtTargetWorld;
    private Quaternion _headNeutralLocal;
    private Quaternion _eyeNeutralLocal;
    private Quaternion _headCurrentLocal;
    private Quaternion _leftEyeCurrentLocal;
    private Quaternion _rightEyeCurrentLocal;

    private float _holdTimer;
    private float _holdDuration;

    private bool _needToLookAtCamera = true;
    
    public void StartLookingAtCamera()
    {
        _needToLookAtCamera = true;
    }

    public void StopLookingAtCamera(int delaySeconds)
    {
        StopLookingAtCamera_async(delaySeconds);
    }

    private async void StopLookingAtCamera_async(int delaySeconds)
    {
        await Task.Delay(delaySeconds * 1000);
        _needToLookAtCamera = false;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        // Capture rest poses so we always rotate relative to neutral
        _headNeutralLocal     = headBone.localRotation;
        _headCurrentLocal     = _headNeutralLocal;
        _leftEyeCurrentLocal  = leftEyeBone.localRotation;
        _rightEyeCurrentLocal = rightEyeBone.localRotation;
        _eyeNeutralLocal      = _leftEyeCurrentLocal;

        StopLookingAtCamera(5);
    }

    private void LateUpdate()
    {
        if (_needToLookAtCamera)
        {
            _lookAtTargetWorld = Camera.main.transform.position;
        }
        else
        {
            // LateUpdate so we run after the animator has posed the skeleton
            _holdTimer += Time.deltaTime;

            if (_holdTimer >= _holdDuration)
            {
                PickNewGazeTarget();
            }
        }

        // Compute desired local offsets for head and eyes
        ComputeDesiredRotations(
            out Quaternion desiredHeadLocal,
            out Quaternion desiredEyeLocal);

        // Smooth head toward desired
        _headCurrentLocal = Quaternion.Slerp(
            _headCurrentLocal,
            desiredHeadLocal,
            Time.deltaTime * headSmoothSpeed);

        headBone.localRotation = _headCurrentLocal;

        _leftEyeCurrentLocal = Quaternion.Slerp(
            _leftEyeCurrentLocal,
            desiredEyeLocal,
            Time.deltaTime * eyeSmoothSpeed);

        _rightEyeCurrentLocal = _leftEyeCurrentLocal; // eyes track together

        leftEyeBone.localRotation  = _leftEyeCurrentLocal;
        rightEyeBone.localRotation = _rightEyeCurrentLocal;
    }

    // ── Core logic ───────────────────────────────────────────────────────────

    private void PickNewGazeTarget()
    {
        _holdTimer    = 0f;
        _holdDuration = Random.Range(minHoldTime, maxHoldTime);

        // Find a random point in front of the character
        float x = Random.Range(-horizontalRange, horizontalRange);
        float y = Random.Range(-verticalRange, verticalRange);

        Vector3 localDir = Quaternion.Euler(y, x, 0f) * Vector3.forward;
        _lookAtTargetWorld = transform.TransformPoint(localDir * gazeDistance);
        // Vertical offset relative to camera position.
        _lookAtTargetWorld += Vector3.up * Camera.main.transform.position.y;
        _lookAtTargetWorld += Vector3.right * Camera.main.transform.position.x;

        // 25% chance we just look AT the camera
        if (Random.value < 0.25f)
        {
            _lookAtTargetWorld = Camera.main.transform.position;
        }
    }

    private void ComputeDesiredRotations(
        out Quaternion desiredHeadLocal,
        out Quaternion desiredEyeLocal)
    {
        // Direction from head to gaze target in world space
        Vector3 toTarget = (_lookAtTargetWorld - headBone.position).normalized;

        // Convert to a rotation in the head bone's parent space
        // so we can clamp it and apply cleanly
        Quaternion worldToHeadParent = Quaternion.Inverse(headBone.parent.rotation);

        Vector3 localDir = worldToHeadParent * toTarget;

        // Decompose into yaw/pitch angles
        float totalYaw   = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float totalPitch = -Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;

        // Split between head and eyes
        float headYaw   = Mathf.Clamp(totalYaw   * headContribution, -maxHeadYaw,   maxHeadYaw);
        float headPitch = Mathf.Clamp(totalPitch  * headContribution, -maxHeadPitch, maxHeadPitch);

        float eyeYaw    = Mathf.Clamp(totalYaw   - headYaw,   -maxEyeYaw,   maxEyeYaw);
        float eyePitch  = Mathf.Clamp(totalPitch  - headPitch, -maxEyePitch, maxEyePitch);

        desiredHeadLocal = _headNeutralLocal * Quaternion.Euler(headPitch, headYaw, 0f);
        desiredEyeLocal  = _eyeNeutralLocal  * Quaternion.Euler(eyePitch,  eyeYaw,  0f);
    }

    // ── Gizmo (scene view debug) ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_lookAtTargetWorld, 0.05f);
        Gizmos.DrawLine(headBone.position, _lookAtTargetWorld);
    }
}