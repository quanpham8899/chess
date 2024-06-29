using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class VariableJoystick : Joystick
{
    public float MoveThreshold { get { return moveThreshold; } set { moveThreshold = Mathf.Abs(value); } }

    [SerializeField] private Vector2 portraitPosition;
    [SerializeField] private Vector2 landscapePosition;
    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;
    [SerializeField] private bool alwaysShowJoystick = false;

    private Vector2 fixedPosition = Vector2.zero;
    private Vector2 m_screen;

    private Coroutine cor_UpdateJoystickType;

    public void SetMode(JoystickType joystickType)
    {
        this.joystickType = joystickType;
        if(joystickType == JoystickType.Fixed)
        {
            background.anchoredPosition = fixedPosition;
            background.gameObject.SetActive(true);
        }
        else
            background.gameObject.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        m_screen = new();

        if (cor_UpdateJoystickType != null ) {
            StopCoroutine(cor_UpdateJoystickType);
            cor_UpdateJoystickType = null;
        }
        cor_UpdateJoystickType = StartCoroutine(UpdateJoystickType());

        fixedPosition = background.anchoredPosition;
        SetMode(joystickType);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if(joystickType != JoystickType.Fixed)
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            background.gameObject.SetActive(true);
        }
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData) {
        if (joystickType != JoystickType.Fixed && !alwaysShowJoystick)
            background.gameObject.SetActive(false);

        base.OnPointerUp(eventData);

        SetJoystickPosition();
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (joystickType == JoystickType.Dynamic && magnitude > moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius;
            background.anchoredPosition += difference;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }

    void SetJoystickPosition() {
        Vector2 screen = new(Screen.width, Screen.height);
        Vector2 position = new();

        if (screen.x < screen.y) {      // Is Portrait
            float _x = portraitPosition.x * screen.x / 100;
            float _y = portraitPosition.y * screen.y / 100;

            position = new(_x, _y);
        } else {                        // Is Landscape
            float _x = landscapePosition.x * screen.x / 100;
            float _y = landscapePosition.y * screen.y / 100;

            position = new(_x, _y);
        }

        background.anchoredPosition = position;
    }

    IEnumerator UpdateJoystickType(float interval = .25f) {
        while (true) {
            if (joystickType == JoystickType.Fixed || (joystickType != JoystickType.Fixed && alwaysShowJoystick)) {
                background.gameObject.SetActive(true);
            }

            Vector2 _screen = new(Screen.width, Screen.height);

            if (m_screen != _screen) {
                m_screen = _screen;
                SetJoystickPosition();  
            }

            yield return new WaitForSeconds(interval);
        }
    }
}

public enum JoystickType { Fixed, Floating, Dynamic }