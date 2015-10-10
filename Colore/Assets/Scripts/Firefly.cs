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

        // When left mouse is pressed
        if (Input.GetMouseButton(0))
        {
            // on first press, immediately set the target color
            if (_mTimer == DateTime.MinValue)
            {
                //immediately set the color
                Corale.Colore.Core.Mousepad.Instance.Clear();
                Corale.Colore.Core.Mousepad.Instance.SetStatic(_mTargetColor);
            }
            // do a slow fade when mouse is unpressed
            _mTimer = DateTime.Now + TimeSpan.FromMilliseconds(500);
        }
        // with time on the clock
        else if (_mTimer > DateTime.Now)
        {
            // fade to black
            Corale.Colore.Core.Mousepad.Instance.SetStatic(Corale.Colore.Core.Color.Black);
        }
        // times up
        else if (_mTimer != DateTime.MinValue)
        {
            // clear the color
            Corale.Colore.Core.Mousepad.Instance.Clear();

            // unset the timer
            _mTimer = DateTime.MinValue;
        }
    }
}
