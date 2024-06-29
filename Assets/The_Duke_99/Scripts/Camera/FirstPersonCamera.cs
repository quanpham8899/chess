using Cinemachine;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;

[System.Serializable]
public class RunningCamera {
    public bool Enabled = false;
    [Range(0f, .1f)]
    public float Amplitude = .015f;
    [Range(0, 30)]
    public float Frequency = 10;
    public float ToggleSpeed = 5;
    public float ResetTimeSmooth = 20;

    [Space]
    public Transform MainCamera;
    public Transform CameraHolder;

    //-----------------------------------------------------

    private Vector3 m_startPosition;

    //-----------------------------------------------------

    public void Init(Transform camera, Transform holder) {
        MainCamera = camera;
        CameraHolder = holder;
        m_startPosition = camera.localPosition;
    }


    //-----------------------------------------------------

    public void CheckMotion(float speed, bool IsGrounded) {
        if (!Enabled) return;
        if (speed < ToggleSpeed || !IsGrounded) return; 
        PlayMotion(FootStepMotion());
    }

    private Vector3 FootStepMotion() {
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Sin(Time.time * Frequency) * Amplitude;
        pos.x += Mathf.Cos(Time.time * Frequency / 2) * Amplitude * 2;
        return pos;
    }

    public void ResetPosition() {
        if (MainCamera.localPosition == m_startPosition) { return; }
        MainCamera.localPosition = Vector3.Lerp(MainCamera.localPosition, m_startPosition, ResetTimeSmooth * Time.deltaTime);
    }

    void PlayMotion(Vector3 motion) {
        MainCamera.localPosition += motion;
    }

}

public class FirstPersonCamera : MonoBehaviour {
    public Camera mainCamera;
    public CinemachineVirtualCamera vCamera;

    [Header("Initialize")]
    [SerializeField]
    private Vector3 offsetFromPlayer = Vector3.zero;
    [SerializeField]
    private LayerMask nonRenderMask;

    [Header("Camera rotation")]
    public Vector2 MouseSensitive = Vector2.one * 5;
    public Vector2 ClampVertical = new(-85, 85);
    public float RotationSmoothness = 0;

    [Header("Head camera")]
    public RunningCamera HeadBobCamera;

    //-----------------------------------------------------------------------

    public Vector2 MouseDelta { get; set; } = Vector2.zero;

    public Vector2 RotationSpeedMultiplier { get; set; } = Vector2.one;

    public Transform MoveObjectHolder { get; private set; }

    //-----------------------------------------------------------------------

    private Quaternion m_characterTargetRot;
    private Quaternion m_cameraTargetRot;

    private Transform m_camera;
    private Transform m_holder;

    private bool m_lockCursor = true;
    private bool m_enabledRotation = true;

    //-----------------------------------------------------------------------

    private void Awake() {
        mainCamera = Camera.main;
        Init();
        HeadBobCamera.Init(mainCamera.transform, m_holder);
    }

    private void Update() {
        Rotation();
    }

    //-----------------------------------------------------------------------

    public void UnlockCursor() { m_lockCursor = false; }
    public void LockCursor() { m_lockCursor = true; }   

    public void EnabledRotation() {
        LockCursor();
        m_enabledRotation = true;
    }
    public void DisabledRotation() {
        UnlockCursor();
        m_enabledRotation = false;
    }

    public void PerformCameraHeadBob(float currentSpeed, bool isGrounded) {
        if (currentSpeed <= HeadBobCamera.ToggleSpeed || !isGrounded) HeadBobCamera.ResetPosition();
        HeadBobCamera.CheckMotion(currentSpeed, isGrounded);
    }

    public void SetCameraAngleX(float X) {
        if (m_enabledRotation) {
            Debug.LogWarning("Try to set static angle for camera controller but camera rotation is active");
            return;
        }

        m_camera.localRotation = Quaternion.Euler(0, m_camera.localEulerAngles.y, m_camera.localEulerAngles.z);
    }

    //-----------------------------------------------------------------------

    void Init() {
        if (vCamera == null) {
            Debug.LogError("Cinemachine Virtual Camera hasn't assigned yet in " + name);
            return;
        }

        mainCamera.cullingMask -= nonRenderMask;

        GameObject MoveObjectHolder = new GameObject("Object move holder");
        MoveObjectHolder.transform.SetParent(mainCamera.transform);
        MoveObjectHolder.transform.localPosition = Vector3.zero;
        this.MoveObjectHolder = MoveObjectHolder.transform;

        GameObject holder = new("Camera holder");
        holder.transform.position = mainCamera.transform.position;
        //mainCamera.transform.SetParent(holder.transform);
        //mainCamera.transform.localPosition = offsetFromPlayer;
        vCamera.transform.SetParent(holder.transform);
        vCamera.transform.localPosition = offsetFromPlayer;
        holder.AddComponent<CameraFollowPlayer>().SetTargetToFollow = transform;
        holder.transform.SetAsFirstSibling();

        //m_camera = mainCamera.transform;
        m_camera = vCamera.transform;
        m_holder = holder.transform;

        m_characterTargetRot = transform.localRotation;
        //m_cameraTargetRot = mainCamera.transform.localRotation;
        m_cameraTargetRot = vCamera.transform.localRotation;

        m_enabledRotation = true;
    }

    void Rotation() {
        if (m_camera == null || m_holder == null) {
            Debug.LogError("Missing some transform reference(s)");
            return;
        }

        UpdateCursorLock();

        if (!m_enabledRotation) return;

        float xRot = MouseDelta.y * MouseSensitive.y * Time.fixedDeltaTime * 10 * RotationSpeedMultiplier.x;
        float yRot = MouseDelta.x * MouseSensitive.x * Time.fixedDeltaTime * 10 * RotationSpeedMultiplier.y;

        m_characterTargetRot *= Quaternion.Euler(0f, yRot, 0f);
        m_cameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);

        // Clamp vertical rotation
        m_cameraTargetRot = ClampRotationAroundXAxis(m_cameraTargetRot);

        if (RotationSmoothness > 0) {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, m_characterTargetRot, RotationSmoothness * Time.deltaTime);
            m_camera.localRotation = Quaternion.Slerp(m_camera.transform.localRotation, m_cameraTargetRot, RotationSmoothness * Time.deltaTime);
        } else {
            transform.rotation = m_characterTargetRot;
            m_camera.transform.localRotation = m_cameraTargetRot;
        }

        m_holder.rotation = transform.rotation;

    }

    void UpdateCursorLock() {
        if (m_lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        } else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    Quaternion ClampRotationAroundXAxis(Quaternion q) {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        angleX = Mathf.Clamp(angleX, ClampVertical.x, ClampVertical.y);

        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }
}