using UnityEngine;
using UnityEngine.Rendering;

public class CameraFollowPlayer : MonoBehaviour {
    private Transform m_target;

    //-------------------------------

    public Transform SetTargetToFollow { set => m_target = value; }

    //-------------------------------

    private void Update() {
        FollowTarget();
    }

    //-------------------------------

    void FollowTarget() {
        if (m_target != null) {
            transform.position = m_target.position;
        }
    }
}