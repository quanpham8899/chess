using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyBox;


[System.Serializable]
public class ControllerDebug {
    public bool GroundCheckDebug = false;
    public bool IsGrounded = false;

    [Space]

    public bool MovementDebug = false;
    public Vector3 RigidbodyVelocity;
    public float FlatPhysicsSpeed;
    public float PhysicsSpeed;
    public float DesireSpeed;
}

[RequireComponent(typeof(Rigidbody))]
public class MyCharacterController : MonoBehaviour {
    public enum MovementMethod {
        MovePosition,       // Use Rigidbody.MovePosition 
        VelocityChange,     // Use Rigidbody.velocity
        AddForce            // Use Rigidbody.AddForce to move character then control the max speed as MoveSpeed 
    }
    public enum CameraPerspective {
        FirstPerson,
        ThirdPeron
    }

    public enum JumpMethod {
        AddForce,
        ChangeVelocity
    }

    // ---------------------------------------------------------------------------------------------------------------

    public Rigidbody Rb;
    public Camera MainCamera;

    [Header("Ground Check")]
    public bool EnabledGroundCheck = true;
    public Vector3 CenterPointOffset = new(0, .3f, 0);

    [Header("Character movement")]
    public MovementMethod MoveMethod = MovementMethod.AddForce;
    [ConditionalField(nameof(MoveMethod), false, MovementMethod.AddForce)]
    public float RigidbodyDrag = 7;
    [Range(0,1)][Tooltip("The resilient input while character airborne. [0] player can't not control while airborne, [1] player freely move while airborne")]
    [ConditionalField(nameof(MoveMethod), false, MovementMethod.AddForce)]
    public float AirDecelerate = .3f;
    [ConditionalField(nameof(MoveMethod), true, MovementMethod.AddForce)]
    public float SmoothGroundMovement = .1f;
    [ConditionalField(nameof(MoveMethod), true, MovementMethod.AddForce)]
    public float SmoothAirMovement = 0;
    [Range(0, 1)]
    public float RotationSmoothness = .1f;

    [Header("Dashing")]
    public float DashStrength = 5;
    public float DashDuration = .5f;
    public float DashCoolDown = 2;
    [Tooltip("FALSE: Disable movement during the dash duration, TRUE: cancel dash by movement input press")]
    public bool DashCanceled = false;
    public uint DashCount = 1;


    [Header("Debug")]

    public ControllerDebug m_Debug;

    // ---------------------------------------------------------------------------------------------------------------

    CameraPerspective m_perspective;

    Transform m_player;

    Dictionary<GameObject, List<Vector3>> collisionInfos = new();

    Coroutine cor_dash;

    Vector3 m_targetVelocity;
    Vector3 m_smoothVelocity;
    Vector3 ref_moveVelocity;

    bool m_enableMovement, m_enableRotation;
    bool m_canDash, m_isDashing;

    float m_moveSpeed;
    float ref_rotateVelocity;
    float m_dashCoolDownTimer;

    uint m_dashCount;

    // ---------------------------------------------------------------------------------------------------------------

    public Vector2 MovementInput { get; set; } = Vector2.zero;

    public float SetGravity { set => Physics.gravity = Vector3.down * value * (value < 0 ? -1 : 1); }

    public bool NormalizeMovementInput { get; set; } = true;
    public bool FreezeRigidbodyRotation {
        set {
            if (GetComponent<Rigidbody>() == null) {
                Rb = gameObject.AddComponent<Rigidbody>();
            }
            Rb = GetComponent<Rigidbody>();

            if (value)
                Rb.constraints = RigidbodyConstraints.FreezeRotation;
            else
                Rb.constraints = RigidbodyConstraints.None;
        }
    }
    public bool IsGrounded { get; private set; } = false;

    // ---------------------------------------------------------------------------------------------------------------

    private void Awake() {
        Init();
    }

    private void Update() {
        m_Debug.IsGrounded = IsGrounded = GroundCheck();
        CharacterRotation();
        StartDashCoolDown();
    }

    private void FixedUpdate() {
        switch (MoveMethod) {
            case MovementMethod.AddForce:
                MoveByAddForce();
                SpeedControl();
                break;

            case MovementMethod.VelocityChange:
                MoveByVelocity();
                break;

            case MovementMethod.MovePosition:
                MoveByPosition();
                break;
        }
        if (m_Debug.MovementDebug) {
            int round = 100;
            m_Debug.PhysicsSpeed = (float)(System.Math.Truncate(new Vector2(Rb.velocity.x, Rb.velocity.z).magnitude * round) / round);
            m_Debug.FlatPhysicsSpeed = (float)(System.Math.Truncate(Rb.velocity.magnitude * round) / round);
            m_Debug.DesireSpeed = m_moveSpeed;
            m_Debug.RigidbodyVelocity = Rb.velocity;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------

    private void OnCollisionStay(Collision collision) {
        StoreCollisionData(collision);
    }

    private void OnCollisionExit(Collision collision) {
        StoreCollisionData(collision, false);
    }

    // ---------------------------------------------------------------------------------------------------------------

    public void EnableMovement() {
        m_enableMovement = true;
    }

    public void DisableMovement() {
        m_enableMovement = false;
        if (cor_dash != null) {
            StopCoroutine(cor_dash);
            cor_dash = null;
        }
    }

    public void EnableRotation() {
        m_enableRotation = true;
    }

    public void DisableRotation() {
        m_enableRotation = false;
    }

    // ---------------------------------------------------------------------------------------------------------------

    void Init() {
        if (GetComponent<Rigidbody>() == null) {
            Rb = gameObject.AddComponent<Rigidbody>();
        }
        Rb = GetComponent<Rigidbody>();

        Rb.useGravity = true;
        Rb.constraints = RigidbodyConstraints.FreezeRotation;

        m_player = transform;

        if (MainCamera == null) MainCamera = Camera.main;

        m_enableMovement = m_enableRotation = true;

        m_canDash = true;
        m_dashCount = DashCount;
        m_dashCoolDownTimer = -1;
    }

    #region Ground Check

    bool GroundCheck() {
        if (collisionInfos == null || collisionInfos.Count == 0 || m_player == null) {
            if (m_player == null) Debug.LogWarning("Missing player reference");
            return false;
        } else if (!EnabledGroundCheck) {
            return false;
        } else if (Rb != null) {
            Rb.drag = 0;
        }

        float checkHeight = m_player.position.y + CenterPointOffset.y;

        foreach (var info in collisionInfos) {
            // Skip empty info(s)
            if (info.Value == null || info.Value.Count == 0) continue;

            foreach (Vector3 point in info.Value) {
                if (point.y < checkHeight) {
                    Rb.drag = RigidbodyDrag;
                    return true;
                }
            }
        }

        return false;
    }

    void StoreCollisionData(Collision collision, bool isEntered = true) {
        List<Vector3> points = new();

        if (isEntered) {
            // Get contact data
            foreach (ContactPoint point in collision.contacts) {
                points.Add(point.point);
            }
        }

        // Check dictionary have KEY yet
        if (!collisionInfos.ContainsKey(collision.gameObject)) { collisionInfos.Add(collision.gameObject, new()); }

        // Update collision contact points
        collisionInfos[collision.gameObject] = isEntered ? new(points) : new();
    }

    #endregion

    #region Movement
    
    public void Move(Vector2 Input, CameraPerspective Perspective, float speed) {
        m_perspective = Perspective;
        m_moveSpeed = speed;

        if (!m_enableMovement) {
            MovementInput = Vector2.zero;
            return;
        }

        MovementInput = Input;

        if (MoveMethod != MovementMethod.AddForce) {
            m_targetVelocity = m_moveSpeed * DirectionWithCamera(new(MovementInput.x, 0, MovementInput.y));
        }
    }

    /// <summary>
    /// Rotate character as Movement Input (For third person only)
    /// </summary>
    void CharacterRotation() {
        if (m_perspective != CameraPerspective.ThirdPeron || !m_enableRotation) return;

        if (MovementInput.magnitude >= .1f) {
            Vector3 moveDir = MoveMethod switch {
                MovementMethod.MovePosition => m_smoothVelocity,
                MovementMethod.VelocityChange => m_smoothVelocity,
                MovementMethod.AddForce => DirectionWithCamera(new(MovementInput.x, 0, MovementInput.y)),
                _ => Vector3.zero
            };

            if (Vector3.Dot(moveDir, DirectionWithCamera(new(MovementInput.x, 0, MovementInput.y))) >= .9f) {
                float targetAngle = Mathf.Atan2(MovementInput.x, MovementInput.y) * Mathf.Rad2Deg + MainCamera.transform.eulerAngles.y;
                transform.eulerAngles = Vector3.up * (Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref ref_rotateVelocity, RotationSmoothness));
            }
        }
    }

    void MoveByVelocity() {
        if (Rb == null || MainCamera == null) {
            Debug.LogWarning("Missing components in MyCharacterController in " + name + " GameObject");
            return;
        }

        if (Rb.drag != 0) Rb.drag = 0;

        m_smoothVelocity = Vector3.SmoothDamp(m_smoothVelocity, m_targetVelocity, ref ref_moveVelocity, !EnabledGroundCheck ? SmoothGroundMovement : IsGrounded ? SmoothGroundMovement : SmoothAirMovement);
        Vector3 vel = Rb.velocity;

        vel.x = m_smoothVelocity.x;
        vel.z = m_smoothVelocity.z;

        Rb.velocity = vel;
    }

    void MoveByAddForce() {
        if (Rb == null || MainCamera == null) {
            Debug.LogWarning("Missing components in MyCharacterController in " + name + " GameObject");
            return;
        }

        Vector3 MoveDir = DirectionWithCamera(new(MovementInput.x, 0, MovementInput.y)) ;
        Rb.AddForce(MoveDir.normalized * m_moveSpeed * 10 * (!EnabledGroundCheck ? 1 : IsGrounded ? 1 : AirDecelerate), ForceMode.Force);
    }

    void MoveByPosition() {
        if (Rb == null) {
            Debug.LogWarning("Missing Rigidbody in MyCharacterController in " + name + " GameObject");
            return;
        }

        if (Rb.drag != 0) Rb.drag = 0;

        m_smoothVelocity = Vector3.SmoothDamp(m_smoothVelocity, m_targetVelocity, ref ref_moveVelocity, !EnabledGroundCheck ? SmoothGroundMovement : IsGrounded ? SmoothGroundMovement : SmoothAirMovement);
        Rb.MovePosition(Rb.position + m_smoothVelocity * Time.fixedDeltaTime);
    }

    // For MoveByAddForce to control the max speed
    void SpeedControl() {
        if (MoveMethod != MovementMethod.AddForce) return;

        if (Rb == null) {
            Debug.Log("Missing Rigidbody component");
            return;
        }

        //Vector3 flatVel = new Vector3(Rb.velocity.x, 0f, Rb.velocity.z);
        Vector3 flatVel = new(Rb.velocity.x, 0, Rb.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > m_moveSpeed && MovementInput.magnitude >= .1f) {
            Vector3 limitedVel = flatVel.normalized * m_moveSpeed;
            Rb.velocity = new Vector3(limitedVel.x, Rb.velocity.y, limitedVel.z);
        }
    }

    public void LookAt(Vector3 targetToLook, float smoothRotation) {
        Vector3 direction = targetToLook - transform.position;
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + MainCamera.transform.eulerAngles.y;

        transform.eulerAngles = Vector3.up * Mathf.LerpAngle(transform.eulerAngles.y, angle, smoothRotation * Time.deltaTime);
    }

    #endregion

    #region

    public void Jump(float strength, JumpMethod method = JumpMethod.ChangeVelocity) { 
        if (Rb == null) {
            Debug.LogWarning("Missing Rigidbody component in My Character Controller in " + name + " GameObject");
            return;
        }

        Rb.drag = 0;

        switch (method) {
            case JumpMethod.AddForce:
                Rb.AddForce(transform.up.normalized * strength, ForceMode.VelocityChange);
                break;

            case JumpMethod.ChangeVelocity:
                Vector3 vel = Rb.velocity;
                vel.y = strength;

                Rb.velocity = vel;

                break;
        }
    }

    #endregion

    #region DASH

    public void Dash(Vector3 direction = default(Vector3)) {
        if (Rb == null) {
            return;
        }

        if (m_canDash && !m_isDashing) {
            m_dashCount -= 1;

            if (cor_dash != null) {
                StopCoroutine(cor_dash);
                cor_dash = null;
            }

            cor_dash = StartCoroutine(Dashing(direction));
        }
    }

    void StartDashCoolDown() {
        if (m_dashCount < DashCount && m_dashCoolDownTimer > 0) {
            m_dashCoolDownTimer -= Time.deltaTime;

            if (m_dashCoolDownTimer <= 0) {
                m_dashCount += 1;
                m_canDash = true;

                if (m_dashCount < DashCount) m_dashCoolDownTimer = DashCoolDown;
            }
        }
    }

    #endregion

    Vector3 DirectionWithCamera(Vector3 Input) {
        float targetRotation = Mathf.Atan2(Input.x, Input.z) * Mathf.Rad2Deg + MainCamera.transform.eulerAngles.y;
        Vector3 result = Quaternion.Euler(0, targetRotation, 0) * Vector3.forward * (Input.magnitude >= .1f ? 1 : 0);

        return result;
    }

    // ---------------------------------------------------------------------------------------------------------------

    public float yGravity;

    IEnumerator Dashing(Vector3 direction) {
        m_canDash = false;
        m_isDashing = true;

        Vector3 v = Rb.velocity;
        v.x = v.z = 0;
        Rb.velocity = v;

        if (!DashCanceled) {
            DisableMovement();
        }

        float timer = 0;

        while (timer < DashDuration) {
            if (MovementInput.magnitude > .1f && DashCanceled) {
                break;
            }

            timer += Time.deltaTime;

            Vector3 dashDir = direction == Vector3.zero ? transform.forward.normalized : direction.normalized;

            Vector3 velocity = Rb.velocity;
            velocity.x = dashDir.x * DashStrength;
            velocity.z = dashDir.z * DashStrength;

            Rb.velocity = velocity;

            yGravity = Rb.velocity.y;

            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(.1f);
        EnableMovement();

        m_isDashing = false;

        if (m_dashCount > 0)
            m_canDash = true;

        if (m_dashCoolDownTimer <= 0) {
            m_dashCoolDownTimer = DashCoolDown;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------

    private void OnDrawGizmos() {
        if (m_Debug.GroundCheckDebug) {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position + CenterPointOffset, .2f);
        }

        if (m_Debug.MovementDebug && Application.isPlaying) {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward.normalized * 1);
        }
    }
}