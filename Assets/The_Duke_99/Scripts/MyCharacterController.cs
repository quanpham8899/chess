using MyBox;
using UnityEngine;

public class MyCharacterController : MonoBehaviour {
    public Rigidbody rb;
    public Camera mainCamera;

    [Space]
    public CharacterGroundCheck GroundCheck;
    [SerializeField][ReadOnly]
    private bool isGrounded;
    public CharacterMovement Movement;

    [Header("Debug")]
    public bool DrawDirection = false;

    //--------------------------------------------------------------------------------------------------

    public bool IsGrounded { get; private set; }

    //--------------------------------------------------------------------------------------------------

    private bool m_enabledRotation = true;
    private bool m_enabledMovement = true;

    private float m_targetMoveSpeed = 0;

    //--------------------------------------------------------------------------------------------------

    private void Awake() {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        GroundCheck.Init(transform);
    }

    private void Update() {
        IsGrounded = GroundCheck.IsGrounded;
        GroundCheck.PlayerClipThroughGround();

        Movement.UpdateDebug(rb);
        Movement.SpeedControl(rb, m_targetMoveSpeed, IsGrounded);   // For AddForce MovementMethod

        // Debug
        isGrounded = IsGrounded;
    }

    private void FixedUpdate() {
        if (m_enabledMovement) {
            Movement.MoveCharacter(rb, mainCamera, m_targetMoveSpeed, IsGrounded);
        }
    }

    //--------------------------------------------------------------------------------------------------

    private void OnCollisionStay(Collision collision) {
        if (collision != null) {
            GroundCheck.StoreCollisionData(collision);
        }
    }

    private void OnCollisionExit(Collision collision) {
        if (collision != null) {
            GroundCheck.StoreCollisionData(collision, false);
        } 
    }

    //--------------------------------------------------------------------------------------------------

    public void EnabledMovement() { m_enabledMovement = true; }
    public void DisabledMovement() { m_enabledMovement = false; }

    public void EnabledRotation() { m_enabledRotation = true; }
    /// <summary>
    /// Not affect with First Person controller. Do FirstPersonCamera.DisableRotation() instead
    /// </summary>
    public void DisabledRotation() { m_enabledRotation = false; }

    public void FirstPersonMovement(Vector2 MovementInput, float moveSpeed) {
        Movement.MovementInput = MovementInput;
        m_targetMoveSpeed = moveSpeed;
    }

    public void ThirdPersonMovement(Vector2 MovementInput, float moveSpeed) {
        Movement.MovementInput = MovementInput;
        m_targetMoveSpeed = moveSpeed;

        if (m_enabledRotation) {
            Movement.RotateByInput(transform, mainCamera);
        }
    }

    public void Jump(float jumpStrength) {
        Movement.CharacterJump(rb, IsGrounded, jumpStrength);
    }

    public void LookAt(Vector3 target, float SmoothRotation) {
        if (m_enabledRotation) {
            Debug.LogWarning("May need to DisableRotation.");
        }

        Vector3 direction = target - transform.position;
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        transform.eulerAngles = Vector3.up * Mathf.LerpAngle(transform.eulerAngles.y, angle, SmoothRotation * Time.deltaTime);
    }

    //--------------------------------------------------------------------------------------------------

    private void OnDrawGizmos() {
        if (Application.isPlaying) {
            if (DrawDirection && Movement.SmoothVelocity.magnitude >= .25f) {
                Gizmos.color = Color.green;
                Vector3 direction = transform.position + Movement.MoveDirection.normalized;
                direction.y = transform.position.y + 1.5f;
                Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, direction);
            }

        }
    }
}