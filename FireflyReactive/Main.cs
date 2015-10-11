using Gma.System.MouseKeyHook;
using System;
using System.Threading;
using System.Windows.Forms;

namespace FireflyReactive
{
    public partial class Main : Form
    {
        /// <summary>
        /// Used 3rd party library to detect mouse clicks
        /// </summary>
        IKeyboardMouseEvents _mEvents = null;

        /// <summary>
        /// Indicates the left mouse button is pressed
        /// </summary>
        public static bool _mLeftMouseButton = false;

        /// <summary>
        /// Keep thread alive until exit
        /// </summary>
        private bool _mStayAwake = true;

        public Main()
        {
            InitializeComponent();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _mLeftMouseButton = true;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _mLeftMouseButton = false;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            _mEvents = Hook.GlobalEvents();

            _mEvents.MouseDown += OnMouseDown;
            _mEvents.MouseUp += OnMouseUp;

            this.WindowState = FormWindowState.Minimized;
            this.FormClosed += Main_FormClosed;

            ThreadStart ts = new ThreadStart(UpdateThread);
            Thread thread = new Thread(ts);
            thread.Start();
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            _mEvents.MouseDown -= OnMouseDown;
            _mEvents.MouseUp -= OnMouseUp;
            _mStayAwake = false;
        }

        private void UpdateThread()
        {
            while (_mStayAwake)
            {
                Firefly.Update();
                Thread.Sleep(0);
            }
            Firefly.Quit();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
