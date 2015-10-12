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
        /// Custom effects can set individual LEDs
        /// </summary>
        private static Corale.Colore.Razer.Mousepad.Effects.Custom _mCustomEffect =
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
            for (int i = 0; i < Corale.Colore.Razer.Mousepad.Constants.MaxLeds; ++i)
            {
                _mCustomEffect[i] = color;
            }
            Corale.Colore.Core.Mousepad.Instance.SetCustom(_mCustomEffect);
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

        public static void Update()
        {
            // get the current screen
            _mScreen = Screen.FromRectangle(new Rectangle(Cursor.Position.X, Cursor.Position.Y, 1, 1));

            // When left mouse is pressed
            if (Main._mLeftMouseButton)
            {
                // set the logo
                _mCustomEffect[0] = _mTargetColor;

                // highlight mouse position
                HighlightMousePosition(_mTargetColor);

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
                // clear the color
                Corale.Colore.Core.Mousepad.Instance.Clear();

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

        public static void Quit()
        {
            // clear the color
            Corale.Colore.Core.Mousepad.Instance.Clear();
        }
    }
}
