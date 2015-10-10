using System;
using UnityEngine;

public class Firefly : MonoBehaviour
{
    /// <summary>
    /// Timer for button presses
    /// </summary>
    DateTime _mTimer = DateTime.MinValue;

    void OnEnable()
    {
        Corale.Colore.Core.Mousepad.Instance.Clear();
    }

    void OnDisable()
    {
        Corale.Colore.Core.Mousepad.Instance.Clear();
    }

    void Update()
    {
        if (null == Corale.Colore.Core.Mousepad.Instance)
        {
            Debug.LogError("MousePad instance is null!");
            return;
        }

        if (Input.GetMouseButton(0))
        {
            if (_mTimer == DateTime.MinValue)
            {
                Corale.Colore.Core.Mousepad.Instance.Clear();
                Corale.Colore.Core.Mousepad.Instance.SetStatic(Corale.Colore.Core.Color.Red);
            }
            _mTimer = DateTime.Now + TimeSpan.FromMilliseconds(500);
        }
        else if (_mTimer > DateTime.Now)
        {
            Corale.Colore.Core.Mousepad.Instance.SetStatic(Corale.Colore.Core.Color.Black);
        }
        else if (_mTimer != DateTime.MinValue)
        {
            _mTimer = DateTime.MinValue;
            Corale.Colore.Core.Mousepad.Instance.Clear();
        }
    }
}
