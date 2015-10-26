#define ENABLE_HEADSET
#define ENABLE_KEYBOARD
#define ENABLE_MOUSE
#define ENABLE_MOUSEPAD

using System;
using System.Drawing;
using System.Windows.Forms;

namespace FireflyReactive
{
    static class Firefly
    {
        /// <summary>
        /// Timer for button presses
        /// </summary>
        private static DateTime _mTimer = DateTime.MinValue;

        /// <summary>
        /// The target color
        /// </summary>
        private static Corale.Colore.Core.Color _mTargetColor = Corale.Colore.Core.Color.Green;

        /// <summary>
        /// Static effect for headset
        /// </summary>
        private static Corale.Colore.Razer.Headset.Effects.Static _mHeadsetStaticEffect =
            new Corale.Colore.Razer.Headset.Effects.Static();

        /// <summary>
        /// Custom effects can set individual LEDs on Mouse
        /// </summary>
        private static Corale.Colore.Razer.Mouse.Effects.Custom _mMouseCustomEffect =
            Corale.Colore.Razer.Mouse.Effects.Custom.Create();

        /// <summary>
        /// Custom effects can set individual LEDs on MousePad
        /// </summary>
        private static Corale.Colore.Razer.Mousepad.Effects.Custom _mMousepadCustomEffect =
            Corale.Colore.Razer.Mousepad.Effects.Custom.Create();

        /// <summary>
        /// A reference to the current screen
        /// </summary>
        private static Screen _mScreen = null;

        /// <summary>
        /// Set all the LEDs as the same color
        /// </summary>
        /// <param name="color"></param>
        static void SetColor(Corale.Colore.Core.Color color)
        {
#if ENABLE_MOUSE
            for (int i = 0; i < Corale.Colore.Razer.Mouse.Constants.MaxLeds; ++i)
            {
                _mMouseCustomEffect[i] = color;
            }
            Corale.Colore.Core.Mouse.Instance.SetCustom(_mMouseCustomEffect);
#endif

#if ENABLE_MOUSEPAD
            for (int i = 0; i < Corale.Colore.Razer.Mousepad.Constants.MaxLeds; ++i)
            {
                _mMousepadCustomEffect[i] = color;
            }
            Corale.Colore.Core.Mousepad.Instance.SetCustom(_mMousepadCustomEffect);
#endif
        }

        static float Lerp(float from, float to, float value)
        {
            if (value < 0.0f)
                return from;
            else if (value > 1.0f)
                return to;
            return (to - from) * value + from;
        }

        static float InverseLerp(float from, float to, float value)
        {
            if (from < to)
            {
                if (value < from)
                    return 0.0f;
                else if (value > to)
                    return 1.0f;
            }
            else
            {
                if (value < to)
                    return 1.0f;
                else if (value > from)
                    return 0.0f;
            }
            return (value - from) / (to - from);
        }

        /// <summary>
        /// Get the LED index based on the mouse position
        /// </summary>
        /// <returns></returns>
        static int GetIndex()
        {
            float halfWidth = _mScreen.Bounds.Width * 0.5f;
            int x = _mScreen.WorkingArea.Left;
            int y = _mScreen.WorkingArea.Top;
            if ((Cursor.Position.X - x) < halfWidth)
            {
                return (int)Lerp(8, 14, InverseLerp(_mScreen.Bounds.Height, 0, Cursor.Position.Y - y));
            }
            else
            {
                return (int)Lerp(7, 1, InverseLerp(_mScreen.Bounds.Height, 0, Cursor.Position.Y - y));
            }
        }

        /// <summary>
        /// Highlight mouse position
        /// </summary>
        /// <param name="color"></param>
        static void HighlightMousePosition(Corale.Colore.Core.Color color)
        {
#if ENABLE_MOUSE
            for (int i = 0; i < Corale.Colore.Razer.Mouse.Constants.MaxLeds; ++i)
            {
                _mMouseCustomEffect[i] = color;
            }
            Corale.Colore.Core.Mouse.Instance.SetCustom(_mMouseCustomEffect);
#endif

#if ENABLE_MOUSEPAD
            for (int i = 1; i < Corale.Colore.Razer.Mousepad.Constants.MaxLeds; ++i)
            {
                if (i >= (GetIndex() - 1) &&
                    i <= (GetIndex() + 1))
                {
                    _mMousepadCustomEffect[i] = color;
                }
                else
                {
                    _mMousepadCustomEffect[i] = Corale.Colore.Core.Color.Black;
                }
            }
            Corale.Colore.Core.Mousepad.Instance.SetCustom(_mMousepadCustomEffect);
#endif
        }

        public static void Update()
        {
            // get the current screen
            _mScreen = Screen.FromRectangle(new Rectangle(Cursor.Position.X, Cursor.Position.Y, 1, 1));

            // When left mouse is pressed
            if (Main._mLeftMouseButton)
            {
                // set the logo
                _mMousepadCustomEffect[0] = _mTargetColor;

                // highlight mouse position
                HighlightMousePosition(_mTargetColor);

#if ENABLE_HEADSET
                if (_mHeadsetStaticEffect.Color != _mTargetColor)
                {
                    _mHeadsetStaticEffect.Color = _mTargetColor;
                    Corale.Colore.Core.Headset.Instance.SetStatic(_mHeadsetStaticEffect);
                }
#endif

                // do a slow fade when mouse is unpressed
                _mTimer = DateTime.Now + TimeSpan.FromMilliseconds(500);
            }
            // fade to black when not pressed
            else if (_mTimer > DateTime.Now)
            {
                float t = (float)(_mTimer - DateTime.Now).TotalSeconds / 0.5f;
                Corale.Colore.Core.Color color = new Corale.Colore.Core.Color(0, (double)t, 0);
                SetColor(color);
            }
            // times up
            else if (_mTimer != DateTime.MinValue)
            {
#if ENABLE_HEADSET
                _mHeadsetStaticEffect.Color = Corale.Colore.Core.Color.Blue;
                Corale.Colore.Core.Headset.Instance.SetStatic(_mHeadsetStaticEffect);
#endif

#if ENABLE_MOUSE
                // clear the color
                Corale.Colore.Core.Mouse.Instance.Clear();
#endif

#if ENABLE_MOUSEPAD
                // clear the color
                Corale.Colore.Core.Mousepad.Instance.Clear();
#endif

                // unset the timer
                _mTimer = DateTime.MinValue;
            }
            // highlight the mouse position when button is not pressed
            else
            {
                // highlight mouse position
                HighlightMousePosition(Corale.Colore.Core.Color.Blue);

#if ENABLE_HEADSET
                if (_mHeadsetStaticEffect.Color != Corale.Colore.Core.Color.Blue)
                {
                    _mHeadsetStaticEffect.Color = Corale.Colore.Core.Color.Blue;
                    Corale.Colore.Core.Headset.Instance.SetStatic(_mHeadsetStaticEffect);
                }
#endif
            }
        }

        public static void Start()
        {
#if ENABLE_KEYBOARD
            Corale.Colore.Core.Keyboard.Instance.Clear();

            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.W, Corale.Colore.Core.Color.Red);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.A, Corale.Colore.Core.Color.Red);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.S, Corale.Colore.Core.Color.Red);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.D, Corale.Colore.Core.Color.Red);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F, Corale.Colore.Core.Color.Red);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.G, Corale.Colore.Core.Color.Red);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Oem4, Corale.Colore.Core.Color.Red);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Oem5, Corale.Colore.Core.Color.Red);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Insert, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Delete, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Home, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.End, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.PageDown, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.PageUp, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Tab, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.LeftControl, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.LeftAlt, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.LeftShift, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Space, Corale.Colore.Core.Color.Green);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.One, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Two, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Three, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Four, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Five, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Six, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Seven, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Eight, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Nine, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.Zero, Corale.Colore.Core.Color.Blue);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F1, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F2, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F3, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F4, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F5, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F6, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F7, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F8, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F9, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F10, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F11, Corale.Colore.Core.Color.Orange);
            Corale.Colore.Core.Keyboard.Instance.SetKey(Corale.Colore.Razer.Keyboard.Key.F12, Corale.Colore.Core.Color.Orange);
#endif
        }

        public static void Quit()
        {
#if ENABLE_HEADSET
            Corale.Colore.Core.Headset.Instance.Clear();
#endif

#if ENABLE_KEYBOARD
            Corale.Colore.Core.Keyboard.Instance.Clear();
#endif

#if ENABLE_MOUSE
            Corale.Colore.Core.Mouse.Instance.Clear();
#endif

#if ENABLE_MOUSEPAD
            // clear the color
            Corale.Colore.Core.Mousepad.Instance.Clear();
#endif
        }
    }
}
