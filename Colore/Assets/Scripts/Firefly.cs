using UnityEngine;

public class Firefly : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        if (null == Corale.Colore.Core.Mousepad.Instance)
        {
            Debug.LogError("MousePad instance is null!");
            return;
        }

        Corale.Colore.Core.Mousepad.Instance.SetStatic(Corale.Colore.Core.Color.Blue);
    }
}
