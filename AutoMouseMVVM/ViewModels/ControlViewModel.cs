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
using System.Threading.Tasks;
using System.Diagnostics;
using CsAutoGui;

namespace AutoMouseMVVM.ViewModels
{

    public class ControlViewModel : ViewModelBase
    {

        #region Global Parameter

        static bool ExeNow = false;
        static bool ExeBreak = false;
        KeyboardHook k_hook = new KeyboardHook();
        Record ListBoxRecord = new Record(@"data\ListBoxRecord");
        string FilePath;
        string ConfigPath = @"data\Config.txt";
        System.Drawing.Point ReloadPos = new System.Drawing.Point(0, 0);
        System.Drawing.Point FAPos = new System.Drawing.Point(0, 0);
        AutoGui AG = new AutoGui();
        static List<string> PosAndTime;
        static List<string> Config;
        Dictionary<string, string> Cmds = new Dictionary<string, string>();
        Stopwatch CountDownSw = new Stopwatch();
        Random rnd = new Random(Guid.NewGuid().GetHashCode());

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

        private string _PosColorStr = "";
        public string PosColorStr
        {
            get { return _PosColorStr; }
            set
            {
                _PosColorStr = value;
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
            Record ConfigFile = new Record(ConfigPath);
            Config = ConfigFile.ReadRecordList();
            foreach (var line in Config)
            {
                List<string> items = line.Split(' ').ToList();
                switch (items[0])
                {
                    case "cmd":
                        Cmds[items[1]] = @"data\" + items[1] + ".txt";
                        break;
                    default:
                        break;
                }
            }
            k_hook.KeyDownEvent += new SWF.KeyEventHandler(hook_KeyDown);
            k_hook.Start();
            MouseHook.MouseAction += new EventHandler(MouseClickEvent);
        }
        private void MouseClickEvent(object sender, EventArgs e)
        {
            if (IsAutoPaste && PosColor.ToString().ToUpper() == "#FFFFFFFF")
            {
                Task.Factory.StartNew(() => ClickAfterFewTime());
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
            SpinWait.SpinUntil(() => false, 150);
            SWF.SendKeys.SendWait("^{a}");
            SpinWait.SpinUntil(() => false, 10);
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
            PosColorStr = PosColor.ToString();
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
            else if (e.KeyValue == (int)SWF.Keys.F2)
            {
                Location LocTmp = new Location();
                LocTmp = AG.LocateOnScreen(@"data\fa.png", 0.85);

                Console.WriteLine(Loc2PosString(LocTmp));
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


                        Task.Factory.StartNew(() => RunScript());
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
        public void RunScript()
        {
            // 1 group include : rectangle's x1, y1, x2, y2, Time, (option string => auto-reload)
            ExeNow = true;
            double Time = 0;
            double PreTime = 0;
            List<string> Pos = new List<string>();
            Location LocTmp;
            bool EndBreak = false;
            int CheckState = 0;// 0:Normal  1:Check=Get  2:Check=None 
            if (PosAndTime.Count < 1)
            {
                return;
            }

            while (true)
            {
                GC.Collect();
                //Read command，command list write on Readme.txt
                foreach (string item in PosAndTime)
                {
                    Pos = item.Split(' ').ToList();
                    string PosString = "";
                    bool IsFA = false;
                    if (Pos[0] != "END" && CheckState == 2)
                    {
                        continue;
                    }
                    if (EndBreak)
                    {
                        EndBreak = false;
                        break;
                    }
                    switch (Pos[0])
                    {
                        case "CHECK"://CHECK img_name
                            LocTmp = AG.LocateOnScreen(ImgName2Path(Pos[1]), 0.9);
                            if (LocTmp.IsNull())
                            {
                                CheckState = 2;
                            }
                            else
                            {
                                CheckState = 1;
                            }
                            continue;

                        case "END"://END + BREAK
                            if (CheckState == 1 && Pos.Count > 1 && Pos[1] == "BREAK")
                            {
                                EndBreak = true;
                            }
                            else if (CheckState == 1 && Pos.Count > 1 && Pos[1] == "STOP")
                            {
                                ExeBreak = false;
                                ExeNow = false;
                                return;
                            }
                            CheckState = 0;
                            continue;

                        case "P"://P x1 y1 x2 y2 Time + FA
                            Time = Convert.ToDouble(Pos[5]);
                            PosString = Cmd2PosString(item, 1);
                            break;
                        case "PIMG"://PIMG img_name PreTime Time + FA
                            Time = Convert.ToDouble(Pos[3]);
                            PreTime = Convert.ToDouble(Pos[2]);
                            while (true)
                            {
                                LocTmp = AG.LocateOnScreen(ImgName2Path(Pos[1]), 0.9);
                                if (!LocTmp.IsNull())
                                {
                                    PosString = Loc2PosString(LocTmp);
                                    break;
                                }
                                if (IsBreakCheck())
                                {
                                    return;
                                }
                                SpinWait.SpinUntil(() => false, 200);
                            }
                            break;
                        case "PIMG2"://PIMG2 img_name1 img_name2 Time
                            Time = Convert.ToDouble(Pos[3]);

                            while (true)
                            {
                                LocTmp = AG.LocateOnScreen(ImgName2Path(Pos[1]), 0.85);
                                if (!LocTmp.IsNull())
                                {
                                    break;
                                }
                                if (IsBreakCheck())
                                {
                                    return;
                                }
                                SpinWait.SpinUntil(() => false, 200);
                            }
                            Location LocTmp2 = AG.LocateOnScreen(ImgName2Path(Pos[2]), 0.9, LocTmp.Width, LocTmp.Height, LocTmp.minX, LocTmp.minY);
                            LocTmp2.maxX += LocTmp.minX;
                            LocTmp2.minX += LocTmp.minX;
                            LocTmp2.maxY += LocTmp.minY;
                            LocTmp2.minY += LocTmp.minY;
                            PosString = Loc2PosString(LocTmp2);
                            break;
                        case "PIMGP"://PIMGP img_name x1 y1 x2 y2 Time
                            Time = Convert.ToDouble(Pos[6]);
                            while (true)
                            {
                                LocTmp = AG.LocateOnScreen(ImgName2Path(Pos[1]), 0.85);
                                if (!LocTmp.IsNull())
                                {
                                    LocTmp.maxX = LocTmp.minX + Convert.ToInt32(Pos[4]);
                                    LocTmp.maxY = LocTmp.minY + Convert.ToInt32(Pos[5]);
                                    LocTmp.minX = LocTmp.minX + Convert.ToInt32(Pos[2]);
                                    LocTmp.minY = LocTmp.minY + Convert.ToInt32(Pos[3]);
                                    PosString = Loc2PosString(LocTmp);
                                    break;
                                }
                                if (IsBreakCheck())
                                {
                                    return;
                                }
                                SpinWait.SpinUntil(() => false, 200);
                            }
                            break;
                        case "KEY"://KEY paste Time
                            Time = Convert.ToDouble(Pos[2]);
                            PosString = "";
                            if (Pos[1] == "paste")
                            {
                                ClickAfterFewTime();
                            }
                            break;
                        default:
                            break;
                    }


                    if (PosString != "")
                    {
                        if (!Waitting(PreTime))
                        {
                            return;
                        }
                        RndPath(PosString);
                    }
                    IsFA = Pos[Pos.Count - 1] == "FA";
                    if (IsFA)
                    {
                        if (!AutoReload(Time))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (!Waitting(Time))
                        {
                            return;
                        }
                    }
                }
            }
        }
        public bool Waitting(double Time)
        {
            Time = rnd.Next((int)Math.Round(Time * 1000, 0), (int)Math.Round(Time * 1000, 0) + 500);
            CountDownSw.Restart();
            while (CountDownSw.ElapsedMilliseconds < Time)
            {
                if (IsBreakCheck())
                {
                    return false;
                }
                SpinWait.SpinUntil(() => false, 50);
            }
            return true;
        }
        public bool IsBreakCheck()
        {
            if (ExeBreak)
            {
                ExeBreak = false;
                ExeNow = false;
                return true;
            }
            return false;
        }
        public bool AutoReload(double Time)
        {
            CountDownSw.Restart();


            bool IsTarget = false;
            int ScanCount = 0;
            while (CountDownSw.ElapsedMilliseconds < Time)
            {

                if (IsBreakCheck())
                {
                    return false;
                }
                ScanCount++;
                SpinWait.SpinUntil(() => false, 50);
                if (ScanCount == 4)
                {
                    if (!IsTarget)
                    {
                        IsTarget = AG.LocateOnScreen(ImgName2Path("attack"), 0.9).IsNull();
                        if (!IsTarget && !AG.LocateOnScreen(ImgName2Path("ok"), 0.9).IsNull())
                        {
                            return true;
                        }
                    }
                    else
                    {
                        IsTarget = false;
                        if (Time - CountDownSw.ElapsedMilliseconds > 1000)
                        {
                            SpinWait.SpinUntil(() => false, 200);
                            Location TargetLoc = AG.LocateOnScreen(ImgName2Path("reload"), 0.9);
                            RndPath(Loc2PosString(TargetLoc));
                            ScanCount = 0;
                            while (ScanCount < 50 && CountDownSw.ElapsedMilliseconds < Time)
                            {
                                ScanCount++;
                                IsTarget = !AG.LocateOnScreen(ImgName2Path("attack"), 0.9).IsNull();
                                if (!IsTarget && !AG.LocateOnScreen(ImgName2Path("ok"), 0.9).IsNull())
                                {
                                    return true;
                                }
                                if (IsTarget)
                                {
                                    TargetLoc = AG.LocateOnScreen(ImgName2Path("fa"), 0.9);
                                    RndPath(Loc2PosString(TargetLoc));
                                    break;
                                }
                                if (IsBreakCheck())
                                {
                                    return false;
                                }

                                SpinWait.SpinUntil(() => false, 200);
                            }

                            IsTarget = false;
                        }
                    }
                    ScanCount = 0;
                }
            }
            return true;
        }
        public void RndPathWithImg(string path)
        {
            Location Target = AG.LocateOnScreen(path, 0.9);
        }
        public string Loc2PosString(Location Loc)
        {
            return Loc.minX.ToString() + " " + Loc.minY.ToString() + " " + Loc.maxX.ToString() + " " + Loc.maxY.ToString();
        }
        public string Cmd2PosString(string FullCmd, int StartIndex)
        {
            List<string> Cmd = FullCmd.Split(' ').ToList();
            return Cmd[0 + StartIndex] + " " + Cmd[1 + StartIndex] + " " + Cmd[2 + StartIndex] + " " + Cmd[3 + StartIndex];
        }
        public string ImgName2Path(string ImgName)
        {
            return $@"data\{ImgName}.png";
        }
        public void RndPath(string posline)
        {
            int nowx, nowy;
            int orix, oriy;
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            List<string> pos = posline.Split(' ').ToList();
            nowx = rnd.Next(Convert.ToInt32(pos[0]), Convert.ToInt32(pos[2]));
            nowy = rnd.Next(Convert.ToInt32(pos[1]), Convert.ToInt32(pos[3]));

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

        }
        #endregion

    }
}
