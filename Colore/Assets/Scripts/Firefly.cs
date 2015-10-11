using System;
using UnityEngine;

public class Firefly : MonoBehaviour
{
    /// <summary>
    /// Timer for button presses
    /// </summary>
    private DateTime _mTimer = DateTime.MinValue;

    /// <summary>
    /// The target color
    /// </summary>
    private Corale.Colore.Core.Color _mTargetColor = Corale.Colore.Core.Color.Green;

    /// <summary>
    /// Custom effects can set individual LEDs
    /// </summary>
    private Corale.Colore.Razer.Mousepad.Effects.Custom _mCustomEffect = Corale.Colore.Razer.Mousepad.Effects.Custom.Create();

    /// <summary>
    /// Set all the LEDs as the same color
    /// </summary>
    /// <param name="color"></param>
    void SetColor(Corale.Colore.Core.Color color)
    {
        for (int i = 0; i < Corale.Colore.Razer.Mousepad.Constants.MaxLeds; ++i)
        {
            _mCustomEffect[i] = color;
        }
        Corale.Colore.Core.Mousepad.Instance.SetCustom(_mCustomEffect);
    }

    /// <summary>
    /// Get the LED index based on the mouse position
    /// </summary>
    /// <returns></returns>
    int GetIndex()
    {
        float halfWidth = Screen.width * 0.5f;
        float halfHeight = Screen.height * 0.5f;
        if (Input.mousePosition.x < halfWidth)
        {
            return (int)Mathf.Lerp(8, 14, Mathf.InverseLerp(0, halfHeight, Input.mousePosition.y));
        }
        else
        {
            return (int)Mathf.Lerp(7, 1, Mathf.InverseLerp(0, halfHeight, Input.mousePosition.y));
        }
    }

    /// <summary>
    /// Highlight mouse position
    /// </summary>
    /// <param name="color"></param>
    void HighlightMousePosition(Corale.Colore.Core.Color color)
    {
        for (int i = 1; i < Corale.Colore.Razer.Mousepad.Constants.MaxLeds; ++i)
        {
            if (i >= (GetIndex() - 1) &&
                i <= (GetIndex() + 1))
            {
                _mCustomEffect[i] = color;
            }
            else
            {
                _mCustomEffect[i] = Corale.Colore.Core.Color.Black;
            }
        }
        Corale.Colore.Core.Mousepad.Instance.SetCustom(_mCustomEffect);
    }

    void Update()
    {
        // When left mouse is pressed
        if (Input.GetMouseButton(0))
        {
            // set the logo
            _mCustomEffect[0] = _mTargetColor;

            // highlight mouse position
            HighlightMousePosition(_mTargetColor);

            // set camera to target color
            Camera.main.backgroundColor = Color.green;

            // do a slow fade when mouse is unpressed
            _mTimer = DateTime.Now + TimeSpan.FromMilliseconds(500);
        }
        // fade to black when not pressed
        else if (_mTimer > DateTime.Now)
        {
            float t = (float)(_mTimer - DateTime.Now).TotalSeconds / 0.5f;
            Corale.Colore.Core.Color color = new Corale.Colore.Core.Color(0, (double)t, 0);
            SetColor(color);
            Camera.main.backgroundColor = Color.Lerp(Color.green, Color.black, 1f-t);
        }
        // times up
        else if (_mTimer != DateTime.MinValue)
        {
            // clear the color
            Corale.Colore.Core.Mousepad.Instance.Clear();

            // set camera black
            Camera.main.backgroundColor = Color.black;

            // unset the timer
            _mTimer = DateTime.MinValue;
        }
        // highlight the mouse position when button is not pressed
        else
        {
            // highlight mouse position
            HighlightMousePosition(Corale.Colore.Core.Color.Blue);
        }
    }
}
