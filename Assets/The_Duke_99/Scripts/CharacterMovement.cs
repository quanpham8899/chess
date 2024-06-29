using UnityEngine;
using MyBox;
using UnityEngine.Windows;
using Unity.VisualScripting;
using UnityEditor;

[System.Serializable]
public class CharacterMovement {
    public enum MovementMethod { MovePosition, ChangeVelocity, AddForce };
    public enum JumpMethod { ChangeVelocity, AddForce };    

    //-----------------------------------------------------------------------------------

    [Space]
    public MovementMethod Method;
    [ConditionalField(nameof(Method), false, MovementMethod.AddForce)]
    public float RigidbodyDrag = 7;
    [ConditionalField(nameof(Method), true, MovementMethod.AddForce)]
    public float MovementSmoothness = .1f;
    [ConditionalField(nameof(Method), true, MovementMethod.AddForce)]
    public float AirSmoothness = 0;

    [Space]
    [Tooltip("Apply for third person Movement")]
    public float RotationSmoothness;

    [Header("Jump")]
    public JumpMethod J_Method;

    [Space]
    [SerializeField]
    private bool movementDebug = false;
    [SerializeField]
    private Vector2 currentInput = Vector2.zero;
    [SerializeField][ConditionalField(nameof(movementDebug))]
    private float desireSpeed = 0;
    [SerializeField][ConditionalField(nameof(movementDebug))]
    private float rigidbodySpeed = 0;
    [SerializeField][ConditionalField(nameof(movementDebug))]
    private Vector3 desireSpeedVector3 = Vector3.zero;
    [SerializeField][ConditionalField(nameof(movementDebug))]
    private Vector3 rigidbodySpeedVector3 = Vector3.zero;

    //-----------------------------------------------------------------------------------

    public Vector2 MovementInput { get; set; }

    public Vector3 TargetVelocity { get; private set; }
    public Vector3 SmoothVelocity { get; private set; }
    public Vector3 MoveDirection { get => SmoothVelocity; }

    //-----------------------------------------------------------------------------------

    private float r_currentTurnVelocity;

    private Vector3 r_currentMoveVelocity;
    private Vector3 r_currentMoveVelocity_air;

    //-----------------------------------------------------------------------------------


    public void MoveCharacter(Rigidbody rb, Camera mainCamera, float moveSpeed, bool isGrounded) {
        if (rb == null || mainCamera == null) {
            Debug.LogError("Missing Rigidbody component");
            return;
        }

        Vector3 Direction = DirectionToMove(mainCamera);

        if (Method == MovementMethod.AddForce) {
            rb.AddForce(Direction.normalized * moveSpeed * 10 * (isGrounded ? 1 : .35f), ForceMode.Force);

        } else {
            TargetVelocity = Direction.normalized * moveSpeed;
            SmoothVelocity = Vector3.SmoothDamp(SmoothVelocity, TargetVelocity, ref r_currentMoveVelocity, isGrounded ? MovementSmoothness : AirSmoothness);

            switch (Method) {
                case MovementMethod.MovePosition:
                    rb.MovePosition(rb.position + SmoothVelocity * Time.fixedDeltaTime);
                    break;

                case MovementMethod.ChangeVelocity:
                    Vector3 vel = rb.velocity;
                    vel.x = SmoothVelocity.x;
                    vel.z = SmoothVelocity.z;

                    rb.velocity = vel;

                    break;
            }
        }
    }

    // For using AddForce MovementMethod only
    public void SpeedControl(Rigidbody rb, float targetMoveSpeed, bool isGrounded) {
        if (Method != MovementMethod.AddForce) return;

        if (rb == null) {
            Debug.Log("Missing Rigidbody component");
            return;
        }

        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.drag = 0;
        if (flatVel.magnitude >= 1 && isGrounded) {
            rb.drag = RigidbodyDrag;
        } 

        // limit velocity if needed
        if (flatVel.magnitude > targetMoveSpeed) {
            Vector3 limitedVel = flatVel.normalized * targetMoveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    // If main camera is NULL then rotate by Input only
    public void RotateByInput(Transform player, Camera mainCamera) {
        float targetRotation = Mathf.Atan2(MovementInput.x, MovementInput.y) * Mathf.Rad2Deg + (mainCamera != null ? mainCamera.transform.eulerAngles.y : 0);

        if (MovementInput.magnitude >= .1f) {
            player.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(player.eulerAngles.y, targetRotation, ref r_currentTurnVelocity, RotationSmoothness);
        }
    }

    public void CharacterJump(Rigidbody rb, bool isGrounded, float strength, bool skipGrounded = false) {
        if (isGrounded || skipGrounded) { 
            switch (J_Method) {
                case JumpMethod.ChangeVelocity:
                    Vector3 vel = rb.velocity;
                    vel.y = strength;
                    rb.velocity = vel;
                    
                    break;

                case JumpMethod.AddForce:
                    rb.AddForce(rb.transform.up * strength, ForceMode.VelocityChange);
                    break;
            }
        }
    }

    public void UpdateDebug(Rigidbody rb) {
        if (rb == null) {
            Debug.LogError("Missing rigidbody to perform debug");
            return;
        }

        if (movementDebug) {
            currentInput = MovementInput;
        }
    }

    //-----------------------------------------------------------------------------------

    Vector3 DirectionToMove(Camera mainCamera) {
        float targetRotation = Mathf.Atan2(MovementInput.x, MovementInput.y) * Mathf.Rad2Deg + (mainCamera != null ? mainCamera.transform.eulerAngles.y : 0);
        Vector3 moveDirection = Quaternion.Euler(0, targetRotation, 0) * Vector3.forward * (MovementInput.magnitude >= .1f ? 1 : 0);

        return moveDirection;
    }
}