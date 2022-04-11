using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SWF = System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.ComponentModel.Composition;
using System.ComponentModel;
using AutoMouseMVVM.Functions;
using AutoMouseMVVM.Helper;
using System.Collections.ObjectModel;

namespace AutoMouseMVVM.ViewModels
{

    public class ControlViewModel : ViewModelBase
    {

        #region Global Parameter

        static bool ExeNow = false;
        static bool ExeBreak = false;
        KeyboardHook k_hook = new KeyboardHook();
        Record ListBoxRecord = new Record("ListBoxRecord");
        string FilePath;

        static List<string> PosAndTime;
        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        const int MOUSEEVENTF_MOVE = 0x0001;
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        #endregion

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

        #region Binding Data

        public ObservableCollection<string> _PathList = new ObservableCollection<string>();
        public ObservableCollection<string> PathList
        {
            get
            {
                if (_PathList.Count < 1)
                {
                    foreach (string item in ListBoxRecord.ReadRecordList())
                    {
                        _PathList.Add(item);
                    }
                }
                return _PathList;
            }

        }

        private string _SelectedItem;
        public string SelectedItem
        {
            get { return _SelectedItem; }
            set { _SelectedItem = value; }
        }

        private int _SelectedIndex;

        public int SelectedIndex
        {
            get { return _SelectedIndex; }
            set { _SelectedIndex = value; }
        }

        private int _PosX;
        public int PosX
        {
            get { return _PosX; }
            set
            {
                if (_PosX != value)
                    _PosX = value;
                OnPropertyChanged();
            }
        }
        private int _PosY;
        public int PosY
        {
            get { return _PosY; }
            set
            {
                if (_PosY != value)
                    _PosY = value;
                OnPropertyChanged();
            }
        }

        private SolidColorBrush _PosColor = new SolidColorBrush(Colors.White);
        public SolidColorBrush PosColor
        {
            get { return _PosColor; }
            set
            {
                _PosColor = value;
                OnPropertyChanged();
            }
        }

        private SolidColorBrush _AutoPasteState = new SolidColorBrush(Colors.Gray);
        public SolidColorBrush AutoPasteState
        {
            get { return _AutoPasteState; }
            set
            {
                _AutoPasteState = value;
                OnPropertyChanged();
            }
        }
        private bool _IsAutoPaste = false;
        public bool IsAutoPaste
        {
            get { return _IsAutoPaste; }
            set
            {
                if (value)
                {
                    AutoPasteState = new SolidColorBrush(Colors.GreenYellow);
                    MouseHook.Start();
                }
                else
                {
                    AutoPasteState = new SolidColorBrush(Colors.Gray);
                    MouseHook.Stop();
                }
                _IsAutoPaste = value;
                OnPropertyChanged();
            }
        }

        #endregion


        #region ICommand
        private bool ICommandReturnTrue(object param)
        {
            return true;
        }
        public ICommand AddPath { get; set; }

        private bool CanAddPath(object param)
        {
            return true;
        }

        public ICommand DeletePath { get; set; }

        private bool CanDeletePath(object param)
        {
            return true;
        }

        #endregion
        public ControlViewModel()
        {
            DispatcherTimer DTimer = new DispatcherTimer();
            DTimer.Interval = TimeSpan.FromMilliseconds(50);
            DTimer.Tick += DTimer_Tick;
            DTimer.Start();
            AddPath = new DelegateCommand(_AddPath, ICommandReturnTrue);
            DeletePath = new DelegateCommand(_DeletePath, ICommandReturnTrue);
            k_hook.KeyDownEvent += new SWF.KeyEventHandler(hook_KeyDown);
            k_hook.Start();
            
            MouseHook.MouseAction += new EventHandler(MouseClickEvent);
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
        private bool IsClickWait = false;
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
        public void _AddPath(object param)
        {
            SWF.OpenFileDialog path = new SWF.OpenFileDialog();
            path.Filter =
            "Path (*.TXT)" +
            "All files (*.*)|*.*";
            path.Multiselect = true;
            path.Title = "My Mouse Path Browser";
            if (path.ShowDialog() == SWF.DialogResult.Cancel)
            {
                return;
            }
            else if (!string.IsNullOrEmpty(path.SafeFileName))
            {
                foreach (var item in path.FileNames)
                {
                    PathList.Add(item);
                }
            }

            ListBoxRecord.WriteRecord(PathList.ToList());
        }
        private void _DeletePath(object param)
        {
            if (SelectedIndex > -1)
            {
                PathList.Remove(SelectedItem);
            }
            ListBoxRecord.WriteRecord(PathList.ToList());

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
        string SendText = "";
        int Count = 0;
        private void hook_KeyDown(object sender, SWF.KeyEventArgs e)
        {

            if (e.KeyValue == (int)SWF.Keys.F9)//Stop
            {
                ExeBreak = true;
            }
            else if (e.KeyValue == (int)SWF.Keys.F8)
            {
                Count++;
                SendText = PosX + " " + PosY + " ";
                SendStringToOtherWindow(SendText);
            }
            else if (e.KeyValue == (int)SWF.Keys.F7)
            {
                SendText = PosColor.ToString();
                SendStringToOtherWindow(SendText);
            }
            else if (e.KeyValue == (int)SWF.Keys.F6)
            {
                IsAutoPaste = !IsAutoPaste;
            }
            else if (SWF.Control.ModifierKeys == SWF.Keys.Alt)//Start
            {
                int index = e.KeyValue - 48;
                if (index > -1 && index < PathList.Count)
                {
                    if (!ExeNow)
                    {
                        FilePath = PathList[index].ToString();
                        Record PosFile = new Record(FilePath);
                        PosAndTime = PosFile.ReadRecordList();

                        Thread thread = new Thread(run_mouse);
                        thread.Start();
                    }
                }
            }
        }
        bool SendLock = false;
        private void SendTextFunction()
        {
            if (SendLock)
            {
                return;
            }
            SendLock = true;
            SWF.SendKeys.SendWait(SendText);
            SendLock = false;
        }
        #region KeyBoardSimulation
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        private void SendStringToOtherWindow(string SendData)
        {
            foreach (char item in SendData)
            {
                if (item == '-')
                {
                    keybd_event(189, 0, 0, UIntPtr.Zero);
                    keybd_event(189, 0, 2, UIntPtr.Zero);
                }
                else if (item == '.')
                {
                    keybd_event(190, 0, 0, UIntPtr.Zero);
                    keybd_event(190, 0, 2, UIntPtr.Zero);
                }
                else
                {
                    keybd_event((byte)item, 0, 0, UIntPtr.Zero);
                    keybd_event((byte)item, 0, 2, UIntPtr.Zero);
                }
            }

        }
        #endregion

        #region RunMouse
        public void run_mouse()
        {
            // 1 group include : rectangle's x1, y1, x2, y2, time
            ExeNow = true;
            int nowx, nowy;
            int time;
            string[] pos = new string[5];
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            if (PosAndTime.Count < 1)
            {
                return;
            }

            while (true)
            {
                int orix;
                int oriy;

                foreach (string item in PosAndTime)
                {
                    pos = item.Split(' ');
                    nowx = rnd.Next(Convert.ToInt32(pos[0]), Convert.ToInt32(pos[2]));
                    nowy = rnd.Next(Convert.ToInt32(pos[1]), Convert.ToInt32(pos[3]));

                   
                    time = rnd.Next((int)Math.Round(Convert.ToDouble(pos[4]) * 1000,0), (int)Math.Round(Convert.ToDouble(pos[4]) * 1000, 0) + 500);

                    orix = SWF.Cursor.Position.X;
                    oriy = SWF.Cursor.Position.Y;
                    int recoupx = -1, recoupy = -1;
                    if (orix - nowx < 0)
                    {
                        recoupx = 1;
                    }
                    if (oriy - nowy < 0)
                    {
                        recoupy = 1;
                    }

                    while (true)
                    {
                        for (int k = 0; k < 200000; k++) ;
                        if (nowx != orix)
                        {
                            orix += rnd.Next(2) * recoupx;
                        }
                        if (nowy != oriy)
                        {
                            oriy += rnd.Next(2) * recoupy;
                        }

                        SWF.Cursor.Position = new System.Drawing.Point(orix, oriy);
                        if (orix == nowx && oriy == nowy)
                        {
                            break;
                        }
                    }
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                    for (int j = 0; j < time / 50; j++)
                    {
                        if (ExeBreak)
                        {
                            ExeBreak = false;
                            ExeNow = false;
                            return;
                        }
                        Thread.Sleep(50);
                    }
                }
                time = 500;
                for (int j = 0; j < time / 50; j++)
                {
                    if (ExeBreak)
                    {
                        ExeBreak = false;
                        ExeNow = false;
                        return;
                    }
                    Thread.Sleep(50);
                }
            }
        }
        #endregion

    }
}
