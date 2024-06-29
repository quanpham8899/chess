using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


[System.Serializable]
public class CharacterGroundCheck {
    public static float TIME_TO_BEGIN_SAVE_LAST_POSITION = 10;
    public static float DISTANCE_TO_BEGIN_RESPAWN_PLAYER = 220;

    //--------------------------------------------------------------------------------

    public Vector3 CenterPointOffset = Vector3.up * .3f;
    public bool DisabledGroundCheckWarning = true;

    //--------------------------------------------------------------------------------

    public bool IsGrounded {
        get {
            m_timerToStoreLastPosition -= m_timerToStoreLastPosition > 0 ? Time.deltaTime : 0;

            if (!m_enabledGroundCheck && DisabledGroundCheckWarning) {
                // Just said it for some general usage
                Debug.LogWarning("To use Ground Check function. Please EnabledGroundCheck() in your scripts (It's me DUKE).\n" +
                    "For disabled warning tick Disabled Ground Check Warning");
                return false;
            }

            m_isGrounded = false;

            if (collisionInfos == null || collisionInfos.Count == 0 || m_player == null) {
                if (m_player == null) Debug.LogWarning("Missing player reference. Maybe forgot using Init()");
                return false;
            }
            float checkHeight = m_player.position.y + CenterPointOffset.y;

            foreach (var info in collisionInfos) {
                // Skip empty info(s)
                if (info.Value == null || info.Value.Count == 0) continue;

                foreach (Vector3 point in info.Value) {
                    if (point.y < checkHeight) {
                        if (m_timerToStoreLastPosition <= 0) {
                            LastPosition = point + Vector3.up * .5f;
                            m_timerToStoreLastPosition = TIME_TO_BEGIN_SAVE_LAST_POSITION;
                        }
                        return true;
                    }
                }
            }

            return m_isGrounded;
        }
    }

    public Vector3 LastPosition { get; private set; }   // Store player last grounded position

    //--------------------------------------------------------------------------------

    private Transform m_player;

    private bool m_isGrounded;
    private bool m_enabledGroundCheck = true;

    private float m_timerToStoreLastPosition = 0;

    private Dictionary<GameObject, List<Vector3>> collisionInfos = new();

    //--------------------------------------------------------------------------------

    public void DisabledGroundCheck() { m_enabledGroundCheck = false; }
    public void EnabledGroundCheck() { m_enabledGroundCheck = true; }
    public void Init(Transform player) { m_player = player; }

    public void PlayerClipThroughGround() {
        if (m_player == null) {
            Debug.LogWarning("Missing player reference. May forget to use Init()");
            Debug.LogWarning("Failed to perform respawn player when clip through ground");
            return;
        }

        if (m_player.transform.position.y < LastPosition.y && !m_isGrounded) {
            float distance = Vector3.Distance(m_player.position, LastPosition);

            if (distance >= DISTANCE_TO_BEGIN_RESPAWN_PLAYER) {
                Rigidbody p_rb = m_player.gameObject.GetComponent<Rigidbody>(); 

                m_player.position = LastPosition;
                if (p_rb != null) {
                    p_rb.velocity = new();
                }
            }
        }
    }

    public void StoreCollisionData(Collision collision, bool isEntered = true) {
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
}
