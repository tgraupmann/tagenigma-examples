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

    void OnEnable()
    {
        // error checking
        if (null == Corale.Colore.Core.Mousepad.Instance)
        {
            Debug.LogError("MousePad instance is null!");
            return;
        }

        // clear the color on start
        Corale.Colore.Core.Mousepad.Instance.Clear();
    }

    void OnDisable()
    {
        // error checking
        if (null == Corale.Colore.Core.Mousepad.Instance)
        {
            Debug.LogError("MousePad instance is null!");
            return;
        }

        // clear the color on stop
        Corale.Colore.Core.Mousepad.Instance.Clear();
    }

    void Update()
    {
        // error checking
        if (null == Corale.Colore.Core.Mousepad.Instance)
        {
            Debug.LogError("MousePad instance is null!");
            return;
        }

        // When left mouse button is first pressed
        if (Input.GetMouseButtonDown(0))
        {
            //clear the color
            Corale.Colore.Core.Mousepad.Instance.Clear();
        }

        // When left mouse is pressed
        if (Input.GetMouseButton(0))
        {
            Corale.Colore.Core.Mousepad.Instance.SetStatic(_mTargetColor);

            // set camera to target color
            Camera.main.backgroundColor = Color.green;

            // do a slow fade when mouse is unpressed
            _mTimer = DateTime.Now + TimeSpan.FromMilliseconds(500);
        }

        // When left mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            // fade to black
            Corale.Colore.Core.Mousepad.Instance.SetStatic(Corale.Colore.Core.Color.Black);
        }

        // with time on the clock
        if (_mTimer > DateTime.Now)
        {
            //fade camera to black
            Camera.main.backgroundColor = Color.Lerp(Color.green, Color.black, 1f-(float)(_mTimer-DateTime.Now).TotalSeconds / 0.5f);
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
    }
}
