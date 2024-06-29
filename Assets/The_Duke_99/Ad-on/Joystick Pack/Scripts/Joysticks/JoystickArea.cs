using MyBox;
using UnityEngine;

public class JoystickArea : MonoBehaviour {
    [Header("Area")]

    public RectTransform Rect;

    public bool FillHeight = false;
    public bool FillWidth = false;

    [Range(0, 100)]
    public int HeightPercent = 50;
    [Range(0, 100)]
    public int WidthPercent = 50;

    [Header("Joystick")]

    public RectTransform Background;
    public RectTransform Handle;

    [Range(10, 80)]
    public int BackgroundScale = 20;
    [Range(10, 80)]
    public int HandleScale = 50;

    [Header("Debug")]
    public bool EnabledDebug = false;
    [ReadOnly]
    public float ScreenWidth = 0;
    [ReadOnly]
    public float ScreenHeight = 0;

    // ---------------------------------------------------------------------------------------

    float m_pWidth, m_pHeight;
    float m_background, m_handle;
    Vector2 m_Screen;

    // ---------------------------------------------------------------------------------------

    private void Awake() {
        Rect = GetComponent<RectTransform>();

        m_background = m_handle = 0;
        m_pHeight = m_pWidth = 0;
        m_Screen = new();
    }

    private void Start() {
        FillToScreen();
        SetJoystickSize();
    }

    private void Update() {
        OnDebug();
        OnUpdateScreenResolution();
    }

    // ---------------------------------------------------------------------------------------

    void OnDebug() {
        if (EnabledDebug) {
            ScreenWidth = Screen.width;
            ScreenHeight = Screen.height;
        }
    }

    void FillToScreen() {
        if (Rect == null) return;

        Rect.anchoredPosition = new();

        float width = Screen.width;
        float height = Screen.height;

        Debug.Log("Height: " + height + "\nWidth: " + width);

        if (FillWidth)
            Rect.SetWidth(width);
        if (FillHeight)
            Rect.SetHeight(height);
    }

    void SetRectSize() {
        if (Rect == null) return;

        if (!FillWidth) {
            float width = Screen.width;

            Rect.SetWidth(width * (WidthPercent <= 0 ? 50 : WidthPercent) / 100);
        }

        if (!FillHeight) {
            float height = Screen.height;

            Rect.SetHeight(height * (HeightPercent <= 0 ? 50 : HeightPercent) / 100);
        }
    }

    void OnUpdateScreenResolution() {
        if (m_pWidth != WidthPercent || m_pHeight != HeightPercent || new Vector2(Screen.width, Screen.height) != m_Screen || m_background != BackgroundScale || m_handle != HandleScale) {
            m_pWidth = WidthPercent;
            m_pHeight = HeightPercent;
            m_Screen = new(Screen.width, Screen.height);
            m_background = BackgroundScale;
            m_handle = HandleScale;

            FillToScreen();
            SetRectSize();
            SetJoystickSize();
            Debug.Log("Update screen resolution");
        }
    }

    void SetJoystickSize() {
        if (Background == null || Handle == null) return;

        float height = Screen.height;
        float width = Screen.width;

        float target = height > width ? height : width;

        // Set background size 
        Background.SetWidth(target * BackgroundScale / 100);
        Background.SetHeight(target * BackgroundScale / 100);

        // Set handle size (scale base on background size)
        float b_size = Background.rect.width;

        Handle.SetWidth(b_size * HandleScale / 100);
        Handle.SetHeight(b_size * HandleScale / 100);
    }
}