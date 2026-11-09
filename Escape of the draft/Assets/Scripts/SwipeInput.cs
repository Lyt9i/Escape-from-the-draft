using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeInput : MonoBehaviour
{
    [SerializeField] private ShiftPuzzleGame game;
    [SerializeField] private float minimumSwipePixels = 30f;

    private Vector2 pointerStart;

    private void Awake()
    {
        if (game == null)
            game = GetComponent<ShiftPuzzleGame>();
    }

    private void Update()
    {
        if (game == null || !game.CanAcceptInput)
            return;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
                pointerStart = touch.position.ReadValue();

            if (touch.press.wasReleasedThisFrame)
                TrySendSwipe(touch.position.ReadValue());

            if (touch.press.isPressed)
                return;
        }

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            pointerStart = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            TrySendSwipe(Mouse.current.position.ReadValue());
    }

    private void TrySendSwipe(Vector2 pointerEnd)
    {
        if ((pointerEnd - pointerStart).sqrMagnitude <
            minimumSwipePixels * minimumSwipePixels)
        {
            return;
        }

        game.TryShiftFromSwipe(pointerStart, pointerEnd);
    }
}