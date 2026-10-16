using UnityEngine;
using UnityEngine.InputSystem;

public class InputWatcher : MonoBehaviour
{
    public string device = "pc";
    public bool active = true;
    protected string currentInput = "";

    private PlayerInput playerInput;

    protected Vector2 swipeStartPos;
    protected float minSwipeDistance = 63.0f;
    private float holdTime = 0f;
    private const float requiredHoldTime = 0.72f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    // Update is called once per frame
    void Update()
    {
        watchInput();
    }

    protected virtual void watchInput()
    {
        if (!active) return;

        if(device == "smartphone")
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                currentInput = watchTouch();
            }
        }
        else if(device == "pc")
        {
            currentInput = watchKey();
        }
        
    }

    protected string watchKey()
    {
        if (Keyboard.current.upArrowKey.wasPressedThisFrame) return "run";
        if (Keyboard.current.downArrowKey.wasPressedThisFrame) return "slide";
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) return "right";
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) return "left";
        if (Keyboard.current.spaceKey.isPressed) return "stop";
        return "";
    }

    protected string watchTouch()
    {
        var touch = Touchscreen.current.primaryTouch;
        var phase = touch.phase.ReadValue();

        var tipo = "";

        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                swipeStartPos = touch.position.ReadValue();
                holdTime = 0f;  // Reset hold
                break;
            case UnityEngine.InputSystem.TouchPhase.Stationary:
                holdTime += Time.deltaTime;
                if (holdTime >= requiredHoldTime)
                {
                    holdTime = 0f;  // Reset para evitar repeats
                    return "stop";
                }
                break;
            case UnityEngine.InputSystem.TouchPhase.Ended:
                Vector2 swipeEndPos = touch.position.ReadValue();
                tipo = getSwipeType(swipeStartPos, swipeEndPos, minSwipeDistance);
                break;
        }

        return tipo;
    }

    protected string getSwipeType(Vector2 swipeStart, Vector2 swipeEnd, float minDistance)
    {
        Vector2 delta = swipeEnd - swipeStart;
        float swipeDistance = delta.magnitude;

        if (swipeDistance >= minDistance)
        {
            if (delta.y > 0 && validSwipe(swipeStart, swipeEnd, minDistance, true, "y"))
            {
                return "run";
            }
            else if (delta.y < 0 && validSwipe(swipeStart, swipeEnd, minDistance, false, "y"))
            {
                return "slide";
            }
            else if (delta.x > 0 && validSwipe(swipeStart, swipeEnd, minDistance, true, "x"))
            {
                return "right";
            }
            else if (delta.x < 0 && validSwipe(swipeStart, swipeEnd, minDistance, false, "x"))
            {
                return "left";
            }
        }

        return "";
    }

    protected bool validSwipe(Vector2 start, Vector2 end, float validDistance, bool superior, string coord)
    {
        float a = coord == "x" ? start.x : start.y;
        float b = coord == "x" ? end.x : end.y;
        float distance = superior ? (b - a) : (a - b);

        if (distance < validDistance)
        {
            return false;
        }

        if (coord == "y")
        {
            float right = end.x - start.x;
            float left = start.x - end.x;

            if (right > distance || left > distance)
            {
                return false;
            }
        }

        return true;
    }
}
