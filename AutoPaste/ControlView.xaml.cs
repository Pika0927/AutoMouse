using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SWF = System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoPaste
{
    /// <summary>
    /// ControlView.xaml の相互作用ロジック
    /// </summary>
    public partial class ControlView : UserControl
    {
        #region Call DLL
        /// <summary>/// 擷取指定視窗的裝置情境/// </summary>
        /// /// <param name="hwnd">將擷取其裝置情境的視窗的控制代碼。若為0，則要擷取整個螢幕的DC</param>
        /// /// <returns>指定視窗的裝置情境控制代碼，出錯則為0</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        /// /// <summary>/// 釋放由調用GetDC函數擷取的指定裝置情境/// </summary>
        /// /// <param name="hwnd">要釋放的裝置情境相關的視窗控制代碼</param>
        /// /// <param name="hdc">要釋放的裝置情境控制代碼</param>
        /// /// <returns>執行成功為1，否則為0</returns>
        [DllImport("user32.dll")] public static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);
        /// /// <summary>
        /// /// 在指定的裝置情境中取得一個像素的RGB值
        /// /// </summary>
        /// /// <param name="hdc">一個裝置情境的控制代碼</param>
        /// /// <param name="nXPos">邏輯座標中要檢查的橫座標</param>
        /// /// <param name="nYPos">邏輯座標中要檢查的縱座標</param>
        /// /// <returns>指定點的顏色</returns>
        [DllImport("gdi32.dll")] public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
        #endregion

        KeyboardHook k_hook = new KeyboardHook();
        int PosX;
        int PosY;
        private bool _IsAutoPaste = false;
        public bool IsAutoPaste
        {
            get { return _IsAutoPaste; }
            set
            {
                if (value)
                {
                    //AutoPasteState = new SolidColorBrush(Colors.GreenYellow);
                    MouseHook.Start();
                }
                else
                {
                    //AutoPasteState = new SolidColorBrush(Colors.Gray);
                    MouseHook.Stop();
                }
                _IsAutoPaste = value;
                //OnPropertyChanged();
            }
        }
        bool IsClickWait = false;
        SolidColorBrush PosColor;
        public ControlView()
        {
            InitializeComponent();
            DispatcherTimer DTimer = new DispatcherTimer();
            DTimer.Interval = TimeSpan.FromMilliseconds(50);
            DTimer.Tick += DTimer_Tick;
            DTimer.Start();
            k_hook.KeyDownEvent += new SWF.KeyEventHandler(hook_KeyDown);
            k_hook.Start();

            MouseHook.MouseAction += new EventHandler(MouseClickEvent); 
        }
        private void DTimer_Tick(object sender, EventArgs e)
        {
            PosX = SWF.Cursor.Position.X;
            PosY = SWF.Cursor.Position.Y;
            PosColor = new SolidColorBrush(GetColor(PosX, PosY));
        }
        private System.Windows.Media.Color GetColor(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(255, (byte)(pixel & 0x000000FF), (byte)((pixel & 0x0000FF00) >> 8), (byte)((pixel & 0x00FF0000) >> 16));
            return color;
        }
        private void hook_KeyDown(object sender, SWF.KeyEventArgs e)
        {
            if (e.KeyValue == (int)SWF.Keys.F6)
            {
                IsAutoPaste = !IsAutoPaste;
            }
        }
        private void MouseClickEvent(object sender, EventArgs e)
        {
            // Console.WriteLine(PosColor.ToString());
            if (IsAutoPaste && PosColor.ToString().ToUpper() == "#FFFFFFFF")
            {
                Thread T1 = new Thread(ClickAfterFewTime);
                T1.Start();
            }
        }
        private void ClickAfterFewTime()
        {
            if (IsClickWait)
            {
                return;
            }
            IsClickWait = true;
            Thread.Sleep(150);
            SWF.SendKeys.SendWait("^{a}");
            Thread.Sleep(10);
            SWF.SendKeys.SendWait("^{v}");
            IsClickWait = false;
        }
    }
}
