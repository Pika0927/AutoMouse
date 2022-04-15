using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

/// <summary>
/// 仿照Python的pyautogui库，写的C#版本，实现部分功能，其余功能慢慢添加
/// 本dll可以控制鼠标位置，键盘输入，通过屏幕截图，来寻找在屏幕中的坐标位置等功能
/// </summary>
namespace CsAutoGui
{
    /// <summary>
    /// 鼠标三个键的枚举
    /// </summary>
    public enum MouseButton
    {
        /// <summary>
        /// 左键
        /// </summary>
        left,
        /// <summary>
        /// 右键
        /// </summary>
        right,
        /// <summary>
        /// 中键
        /// </summary>
        middle
    }
    /// <summary>
    /// 坐标信息的结构体
    /// </summary>
    public struct Location
    {

        /// <summary>
        /// 在屏幕中的最小坐标X
        /// </summary>
        public int minX;
        /// <summary>
        /// 在屏幕中的最小坐标Y
        /// </summary>
        public int minY;
        /// <summary>
        /// 在屏幕中的最大坐标X
        /// </summary>
        public int maxX;
        /// <summary>
        /// 在屏幕中的最大坐标Y
        /// </summary>
        public int maxY;
        /// <summary>
        /// 中心坐标X
        /// </summary>
        public int centerX;
        /// <summary>
        /// 中心坐标Y
        /// </summary>
        public int centerY;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="minX">最小的x坐标位置</param>
        /// <param name="minY">最小的y坐标位置</param>
        /// <param name="maxX">最大的x坐标位置</param>
        /// <param name="maxY">最大的y坐标位置</param>
        /// <param name="centerX">中心的x坐标位置</param>
        /// <param name="centerY">中心的y坐标位置</param>
        public Location(int minX, int minY, int maxX, int maxY, int centerX, int centerY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
            this.centerX = centerX;
            this.centerY = centerY;
        }
        /// <summary>
        /// Location是否为Null
        /// </summary>
        /// <returns>Null返回True</returns>
        public bool IsNull()
        {
            if (this.minX == 0 && this.minY == 0 && this.maxX == 0 && this.maxY == 0 && this.centerX == 0 && this.centerY == 0)
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 异常提醒类
    /// </summary>
    class MyException : ApplicationException
    {
        private string error;
        private Exception innerException;
        //无参数构造函数
        public MyException()
        {

        }
        //带一个字符串参数的构造函数，作用：当程序员用Exception类获取异常信息而非 MyException时把自定义异常信息传递过去
        public MyException(string msg) : base(msg)
        {
            this.error = msg;
        }
        //带有一个字符串参数和一个内部异常信息参数的构造函数
        public MyException(string msg, Exception innerException) : base(msg)
        {
            this.innerException = innerException;
            this.error = msg;
        }
        public string GetError()
        {
            return error;
        }
    }

    /// <summary>
    /// 封装的控制鼠标和键盘输入的底层类
    /// </summary>
    class ControlMouseAndKeyBoard
    {
        #region MouseKeyBoardEvent
        [DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        private static extern void keybd_event(Keys bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
        [DllImport("kernel32.dll")]
        private static extern int GlobalAlloc(int wFlags, int dwBytes);
        [DllImport("kernel32.dll")]
        private static extern int GlobalLock(int hMem);
        [DllImport("kernel32.dll")]
        private static extern int RtlMoveMemory(int muBiaoAdd, string scoreData, int size);
        [DllImport("kernel32.dll")]
        private static extern int GlobalUnlock(int hMem);
        [DllImport("user32")]
        private static extern int EmptyClipboard();
        [DllImport("user32")]
        private static extern int OpenClipboard(int jianJiIntPrt);
        [DllImport("user32")]
        private static extern int SetClipboardData(int wFormat, int hMem);
        [DllImport("user32")]
        private static extern int CloseClipboard();
        //移动鼠标 
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下 
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起 
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下 
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起 
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下 
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起 
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标 
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        //模拟鼠标中间滚动
        const int MOUSEEVENTF_WHEEL = 0x0800;
        //模拟按键按下
        const int KEYEVENTF_KEYDOWN = 0;
        //模拟按键弹起
        const int KEYEVENTF_KEYUP = 2;
        #endregion

        /// <summary>
        /// 热键组合键
        /// </summary>
        /// <param name="keysOne">组合键1</param>
        /// <param name="keysTwo">组合键2</param>
        public static void HotKeyFun(Keys keysOne, Keys keysTwo)
        {
            keybd_event(keysOne, 0, 0, 0);
            keybd_event(keysTwo, 0, 0, 0);
            keybd_event(keysOne, 0, KEYEVENTF_KEYUP, 0);
        }
        /// <summary>
        /// 按下按键
        /// </summary>
        /// <param name="keys">按下的键盘的keys枚举</param>
        public static void PressDownFun(Keys keys)
        {
            keybd_event(keys, 0, KEYEVENTF_KEYDOWN, 0);
        }
        /// <summary>
        /// 弹起按键
        /// </summary>
        /// <param name="keys">弹起的键盘的keys枚举</param>
        public static void PressUpFun(Keys keys)
        {
            keybd_event(keys, 0, KEYEVENTF_KEYUP, 0);
        }
        /// <summary>
        /// 按下并弹起一个按键，完整的动作
        /// </summary>
        /// <param name="keys">按键的keys枚举</param>
        public static void PressFun(Keys keys)
        {
            keybd_event(keys, 0, KEYEVENTF_KEYDOWN, 0);
            keybd_event(keys, 0, KEYEVENTF_KEYUP, 0);
        }
        /// <summary>
        /// 控制鼠标滑轮滚动
        /// </summary>
        /// <param name="distance">滚动的值，负数代表向下，正数代表向上，如-100代表向下滚动100的y坐标</param>
        public static void ScrollFun(int distance)
        {
            //控制鼠标滑轮滚动，count代表滚动的值，负数代表向下，正数代表向上，如-100代表向下滚动100的y坐标
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, distance, 0);
        }
        /// <summary>
        /// 把鼠标移到当前坐标
        /// </summary>
        /// <param name="X">X坐标</param>
        /// <param name="Y">Y坐标</param>
        public static void SetCursorPosFun(int X, int Y)
        {
            SetCursorPos(X, Y);
        }
        /// <summary>
        /// 鼠标点击一下
        /// </summary>
        /// <param name="mouseButton">鼠标的那个按键点击</param>
        public static void MouseClickFun(MouseButton mouseButton)
        {
            switch (mouseButton)
            {
                case MouseButton.left:
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
                case MouseButton.right:
                    mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    break;
                case MouseButton.middle:
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 复制文本到系统剪切板
        /// </summary>
        /// <param name="test">需要复制的文本</param>
        public static void CopyFun(string test)
        {
            int Dwlength = test.Length + 300;
            int GHND = 2;
            int hGlobalMenmory = GlobalAlloc(GHND, Dwlength);
            int lpGlobalMenmory = GlobalLock(hGlobalMenmory);

            RtlMoveMemory(lpGlobalMenmory, test, Dwlength);
            GlobalUnlock(hGlobalMenmory);

            int hwnd = 0;
            OpenClipboard(hwnd);
            EmptyClipboard();

            const int CF_TEXT = 1;
            SetClipboardData(CF_TEXT, hGlobalMenmory);
            CloseClipboard();
        }
        /// <summary>
        /// 鼠标移动坐标，暂时不用
        /// </summary>
        /// <param name="_x">移动的X</param>
        /// <param name="_y">移动的Y</param>
        public static void MouseMoveFun(int _x, int _y)
        {
            mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, _x * 65535 / Screen.PrimaryScreen.Bounds.Width, _y * 65535 / Screen.PrimaryScreen.Bounds.Height, 0, 0);
            //mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, _x , _y, 0, 0);
            // mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
    }

    /// <summary>
    /// 控制类，操作鼠标和键盘需要先实例化此类
    /// </summary>
    public class AutoGui
    {
        /// <summary>
        /// 自动防故障，设置为true时，鼠标坐标为0，0的话，报异常，退出程序
        /// </summary>
        public bool FailSafe { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        public AutoGui()
        {
            FailSafe = false;
        }

        /// <summary>
        /// 由于win10有缩放功能，所以要转为正常的坐标
        /// </summary>
        /// <param name="pixel">需要转换的值</param>
        /// <returns>转换之后的值</returns>
        private double ConvertToNomalCooridate(double pixel)
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            float result = g.DpiX;
            if (result == 0)
            {
                return pixel;
            }
            return pixel * result / 96;
        }

        /// <summary>
        /// 获得主显示器的宽度和高度
        /// </summary>
        /// <returns>第一个为宽度，第二个为高度</returns>
        public int[] GetScreenSize()
        {
            return new int[] {
                Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height
                 };
        }

        /// <summary>
        /// 获得鼠标当前位置
        /// </summary>
        /// <returns>鼠标坐标位置</returns>
        public System.Drawing.Point Position()
        {
            return Control.MousePosition;
        }

        /// <summary>
        /// 鼠标移动到设置的坐标
        /// </summary>
        /// <param name="x">坐标x</param>
        /// <param name="y">坐标y</param>
        /// <param name="duration">移动的速度，默认0为瞬间移动过去</param>
        public void MoveTo(int x, int y, double duration = 0.0)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            //if (duration != 0)
            //{
            //    double moveTime = duration * 1000;
            //    System.Drawing.Point point = this.Position();
            //    int xDis = point.X > x ? point.X : x;
            //    int yDis = point.Y > y ? point.Y : y;
            //    for (int i = 1; i <= moveTime; i = i * (int)moveTime / 100)
            //    {

            //    }
            //}
            SetCursorPos(x, y);
        }

        /// <summary>
        /// 鼠标移动到设置的坐标
        /// </summary>
        /// <param name="xyPoint">System.Drawing.Point，里面包含X和Y的值</param>
        public void MoveTo(System.Drawing.Point xyPoint)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            SetCursorPos(xyPoint.X, xyPoint.Y);
        }

        /// <summary>
        /// 鼠标左键点击一下
        /// </summary>
        public void Click()
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.MouseClickFun(MouseButton.left);
        }

        /// <summary>
        /// 鼠标指针移动到指定坐标，并点击一下
        /// </summary>
        /// <param name="x">坐标x</param>
        /// <param name="y">坐标y</param>
        public void Click(int x, int y)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.SetCursorPosFun(x, y);
            ControlMouseAndKeyBoard.MouseClickFun(MouseButton.left);
        }

        /// <summary>
        /// 鼠标点击一下，需要指定坐标和鼠标的哪个键点击
        /// </summary>
        /// <param name="x">坐标x</param>
        /// <param name="y">坐标y</param>
        /// <param name="mouseButton">鼠标的按键</param>
        public void Click(int x, int y, MouseButton mouseButton)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.SetCursorPosFun(x, y);
            ControlMouseAndKeyBoard.MouseClickFun(mouseButton);
        }

        /// <summary>
        /// 鼠标左键双击
        /// </summary>
        public void DoubleClick()
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.MouseClickFun(MouseButton.left);
            ControlMouseAndKeyBoard.MouseClickFun(MouseButton.left);
        }

        /// <summary>
        /// 鼠标左键在指定位置双击
        /// </summary>
        /// <param name="x">坐标x</param>
        /// <param name="y">坐标y</param>
        public void DoubleClick(int x, int y)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.SetCursorPosFun(x, y);
            ControlMouseAndKeyBoard.MouseClickFun(MouseButton.left);
            ControlMouseAndKeyBoard.MouseClickFun(MouseButton.left);
        }

        /// <summary>
        /// 鼠标双击，需要指定坐标和鼠标的哪个键点击
        /// </summary>
        /// <param name="x">坐标x</param>
        /// <param name="y">坐标y</param>
        /// <param name="mouseButton">鼠标的按键</param>
        public void DoubleClick(int x, int y, MouseButton mouseButton)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.SetCursorPosFun(x, y);
            ControlMouseAndKeyBoard.MouseClickFun(mouseButton);
            ControlMouseAndKeyBoard.MouseClickFun(mouseButton);
        }

        /// <summary>
        /// 鼠标滑轮移动
        /// </summary>
        /// <param name="distance">滑轮移动的距离</param>
        public void Scroll(int distance)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.ScrollFun(distance);
        }


        private void SetCursorPos(int x, int y)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.SetCursorPosFun(x, y);
        }

        /// <summary>
        /// 模拟键盘按键
        /// </summary>
        /// <param name="keys">Keys的枚举</param>
        public void Press(params Keys[] keys)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            for (int i = 0; i < keys.Length; i++)
            {
                ControlMouseAndKeyBoard.PressFun(keys[i]);
            }
        }

        /// <summary>
        /// 模拟键盘按键，按下和抬起一个完整的过程
        /// </summary>
        /// <param name="keys">按键的英文名字</param>
        public void Press(params string[] keys)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            for (int i = 0; i < keys.Length; i++)
            {
                ControlMouseAndKeyBoard.PressFun(StrToKeys(keys[i]));
            }
        }


        /// <summary>
        /// 按键按下，不弹起
        /// </summary>
        /// <param name="keys"></param>
        public void KeyDown(params Keys[] keys)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            for (int i = 0; i < keys.Length; i++)
            {
                ControlMouseAndKeyBoard.PressDownFun(keys[i]);
            }
        }

        /// <summary>
        /// 按键按下，不弹起
        /// </summary>
        /// <param name="keys"></param>
        public void KeyDown(params string[] keys)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            for (int i = 0; i < keys.Length; i++)
            {
                ControlMouseAndKeyBoard.PressDownFun(StrToKeys(keys[i]));
            }
        }


        /// <summary>
        /// 按键弹起
        /// </summary>
        /// <param name="keys"></param>
        public void KeyUp(params Keys[] keys)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            for (int i = 0; i < keys.Length; i++)
            {
                ControlMouseAndKeyBoard.PressUpFun(keys[i]);
            }
        }

        /// <summary>
        /// 按键弹起
        /// </summary>
        /// <param name="keys"></param>
        public void KeyUp(params string[] keys)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            for (int i = 0; i < keys.Length; i++)
            {
                ControlMouseAndKeyBoard.PressUpFun(StrToKeys(keys[i]));
            }
        }

        /// <summary>
        /// 快捷键（Keys的枚举），依次传入需要按下的快捷键，例如Ctrl+c
        /// </summary>
        /// <param name="keys">Keys的枚举</param>
        public void HotKey(params Keys[] keys)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            for (int i = 0; i < keys.Length; i++)
            {
                ControlMouseAndKeyBoard.PressDownFun(keys[i]);
            }
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                ControlMouseAndKeyBoard.PressUpFun(keys[i]);
            }
        }


        /// <summary>
        /// 快捷键（字符串），依次传入需要按下的快捷键，例如Ctrl+c
        /// </summary>
        /// <param name="keys">依次按下的按键，需要保证顺序</param>
        public void HotKey(params string[] keys)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            for (int i = 0; i < keys.Length; i++)
            {
                ControlMouseAndKeyBoard.PressDownFun(StrToKeys(keys[i]));
            }
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                ControlMouseAndKeyBoard.PressUpFun(StrToKeys(keys[i]));
            }
        }


        /// <summary>
        /// 将本文复制到系统的剪切板中
        /// </summary>
        /// <param name="test">需要复制的信息</param>
        public void Copy(string test)
        {
            ControlMouseAndKeyBoard.CopyFun(test);
        }

        /// <summary>
        /// 在光标位置发送信息，类似窗口的需要先点击一下，再使用此函数
        /// </summary>
        /// <param name="test">需要发送的文本</param>
        public void SendTest(string test)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                throw new MyException("异常:防故障处理");
            }
            ControlMouseAndKeyBoard.CopyFun(test);
            this.HotKey(Keys.ControlKey, Keys.V);
        }

        /// <summary>
        /// 获得Location的中心坐标值，返回一个Point
        /// </summary>
        /// <param name="location">需要提取中心的Location</param>
        /// <returns></returns>
        public System.Drawing.Point Center(Location location)
        {
            return new System.Drawing.Point(location.centerX, location.centerY);
        }
        public static System.Windows.Media.Imaging.BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();

            System.Windows.Media.Imaging.BitmapSource bitmapsource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap,
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            return bitmapsource;
        }
        /// <summary>
        /// 传入截图，获得图片在屏幕中的位置信息
        /// </summary>
        /// <param name="imgPath">截图的地址</param>
        /// <param name="threshold">相似度，默认为1，建议为0.9</param>
        /// <returns>坐标信息的结构体</returns>
        public Location LocateOnScreen(string imgPath, double threshold = 1)
        {
            //防故障检测
            if (this.FailSafe && this.Position().X == 0 && this.Position().Y == 0)
            {
                Console.WriteLine("Fail");
                throw new MyException("异常:防故障处理");
            }


            //创建图象，保存将来截取的图象
            Bitmap imgSrc = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics imgGraphics = Graphics.FromImage(imgSrc);
            //设置截屏区域
            imgGraphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size((int)ConvertToNomalCooridate(Screen.PrimaryScreen.Bounds.Width), (int)ConvertToNomalCooridate(Screen.PrimaryScreen.Bounds.Height)));
            //寻找位置的图片
            Bitmap imgSub = new Bitmap(imgPath);
            OpenCvSharp.Mat srcMat = null;
            OpenCvSharp.Mat dstMat = null;
            OpenCvSharp.OutputArray outArray = null;
            Console.WriteLine(Screen.PrimaryScreen.Bounds.Width + "*" + Screen.PrimaryScreen.Bounds.Height);
            try
            {
                srcMat = BitmapSourceConverter.ToMat(Convert(imgSrc));
                dstMat = Cv2.ImRead(imgPath);
                Cv2.CvtColor(dstMat, dstMat, ColorConversionCodes.RGB2GRAY);
                Cv2.CvtColor(srcMat, srcMat, ColorConversionCodes.RGB2GRAY);

                outArray = OpenCvSharp.OutputArray.Create(srcMat);
                //开始匹配
                OpenCvSharp.Cv2.MatchTemplate(srcMat, dstMat, outArray, TemplateMatchModes.CCoeffNormed);

                double minValue, maxValue;
                OpenCvSharp.Point location, point;
                OpenCvSharp.Cv2.MinMaxLoc(OpenCvSharp.InputArray.Create(outArray.GetMat()), out minValue, out maxValue, out location, out point);
                if (maxValue >= threshold)
                {
                    return new Location(point.X, point.Y, point.X + imgSub.Width, point.Y + imgSub.Height, point.X + imgSub.Width / 2, point.Y + imgSub.Height / 2);
                }
                return new Location();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.Message);
                return new Location();
            }
            finally
            {
                if (imgSrc != null)
                    imgSrc.Dispose();
                if (imgGraphics != null)
                    imgGraphics.Dispose();
                if (imgSub != null)
                    imgSub.Dispose();
                if (srcMat != null)
                    srcMat.Dispose();
                if (dstMat != null)
                    dstMat.Dispose();
                if (outArray != null)
                    outArray.Dispose();
            }
        }

        private Keys StrToKeys(string key)
        {
            key = key.ToUpper();
            switch (key)
            {
                case "A":
                    return Keys.A;
                case "B":
                    return Keys.B;
                case "C":
                    return Keys.C;
                case "D":
                    return Keys.D;
                case "E":
                    return Keys.E;
                case "F":
                    return Keys.F;
                case "G":
                    return Keys.G;
                case "H":
                    return Keys.J;
                case "I":
                    return Keys.I;
                case "J":
                    return Keys.J;
                case "K":
                    return Keys.K;
                case "L":
                    return Keys.L;
                case "M":
                    return Keys.M;
                case "N":
                    return Keys.N;
                case "O":
                    return Keys.O;
                case "P":
                    return Keys.P;
                case "Q":
                    return Keys.Q;
                case "R":
                    return Keys.R;
                case "S":
                    return Keys.S;
                case "T":
                    return Keys.T;
                case "U":
                    return Keys.U;
                case "V":
                    return Keys.V;
                case "W":
                    return Keys.W;
                case "X":
                    return Keys.X;
                case "Y":
                    return Keys.Y;
                case "Z":
                    return Keys.Z;
                case "+":
                    return Keys.Add;
                case "*":
                    return Keys.Multiply;
                case "-":
                    return Keys.Subtract;
                case "/":
                    return Keys.Divide;
                case "F1":
                    return Keys.F1;
                case "F2":
                    return Keys.F2;
                case "F3":
                    return Keys.F3;
                case "F4":
                    return Keys.F4;
                case "F5":
                    return Keys.F5;
                case "F6":
                    return Keys.F6;
                case "F7":
                    return Keys.F7;
                case "F8":
                    return Keys.F8;
                case "F9":
                    return Keys.F9;
                case "F10":
                    return Keys.F10;
                case "F11":
                    return Keys.F11;
                case "F12":
                    return Keys.F12;
                case "CTRL":
                    return Keys.Control;
                case "ALT":
                    return Keys.Alt;
                case "SHIFT":
                    return Keys.Shift;
                case "LSHIFT":
                    return Keys.LShiftKey;
                case "RSHIFT":
                    return Keys.RShiftKey;
                case "CTRLKEY":
                    return Keys.ControlKey;
                case "SHIFTKEY":
                    return Keys.ShiftKey;
                case "TAB":
                    return Keys.Tab;
                case "CAPSLOCK":
                    return Keys.CapsLock;
                case "UP":
                    return Keys.Up;
                case "DOWN":
                    return Keys.Down;
                case "LEFT":
                    return Keys.Left;
                case "RIGHT":
                    return Keys.Right;
                case "BAKE":
                    return Keys.Back;
                case "BAKESPACE":
                    return Keys.Back;
                case "ESC":
                    return Keys.Escape;
                case "ENTER":
                    return Keys.Enter;
                case "LCTRLKEY":
                    return Keys.LControlKey;
                case "RCTRLKEY":
                    return Keys.RControlKey;
                case "PAGEDOWN":
                    return Keys.PageDown;
                case "PAGEUP":
                    return Keys.PageUp;
                case "DELETE":
                    return Keys.Delete;
                case "HOME":
                    return Keys.Home;
                case "END":
                    return Keys.End;
                case "INSERT":
                    return Keys.Insert;
                case "PRINTSCREEN":
                    return Keys.PrintScreen;
                case "JIETU":
                    return Keys.PrintScreen;
                case "SCROLLLOCK":
                    return Keys.Scroll;
                case "PAUSE":
                    return Keys.Pause;
                case "NUMLOCK":
                    return Keys.NumLock;
                case "0":
                    return Keys.D0;
                case "1":
                    return Keys.D1;
                case "2":
                    return Keys.D2;
                case "3":
                    return Keys.D3;
                case "4":
                    return Keys.D4;
                case "5":
                    return Keys.D5;
                case "6":
                    return Keys.D6;
                case "7":
                    return Keys.D7;
                case "8":
                    return Keys.D8;
                case "9":
                    return Keys.D9;
                case "N0":
                    return Keys.NumPad0;
                case "N1":
                    return Keys.NumPad1;
                case "N2":
                    return Keys.NumPad2;
                case "N3":
                    return Keys.NumPad3;
                case "N4":
                    return Keys.NumPad4;
                case "N5":
                    return Keys.NumPad5;
                case "N6":
                    return Keys.NumPad6;
                case "N7":
                    return Keys.NumPad7;
                case "N8":
                    return Keys.NumPad8;
                case "N9":
                    return Keys.NumPad9;
                case ".":
                    return Keys.Decimal;
                case ";":
                    return Keys.OemSemicolon;
                case "WIN":
                    return Keys.LWin;
                case "LWIN":
                    return Keys.LWin;
                case "RWIN":
                    return Keys.RWin;
                case "~":
                    return Keys.Oemtilde;
                case "(":
                    return Keys.OemOpenBrackets;
                case ")":
                    return Keys.OemCloseBrackets;
                case "'":
                    return Keys.OemQuotes;
                case "\"":
                    return Keys.OemQuotes;
                case "\\":
                    return Keys.OemBackslash;
                default:
                    return Keys.None;
            }
        }

    }
}