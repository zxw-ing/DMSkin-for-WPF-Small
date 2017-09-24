using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DMSkin.WPF
{
    public partial class DMSkinWindow : Window, INotifyPropertyChanged
    {
        #region UI更新接口
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region 初始化


        public DMSkinWindow()
        {
            DataContext = this;
            InitializeWindowStyle();

            //绑定窗体操作函数
            SourceInitialized += MainWindow_SourceInitialized;
            StateChanged += MainWindow_StateChanged;
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        }
        #endregion

        #region 窗口模式

        /// <summary>
        /// 慢慢显示的动画
        /// </summary>
        ///Storyboard StoryboardSlowShow;
        /// <summary>
        /// 慢慢隐藏的动画
        /// </summary>
        //Storyboard StoryboardSlowHide;

        /// <summary>
        /// 加载双层窗口的样式
        /// </summary>
        private void InitializeWindowStyle()
        {
            ResourceDictionary dic = new ResourceDictionary { Source = new Uri(@"/DMSkin.WPF;component/Themes/DMSkin.xaml", UriKind.Relative) };
            Resources.MergedDictionaries.Add(dic);
            Style = (Style)dic["MainWindow"];
            //string packUriAnimation = @"/DMSkin.WPF;component/Themes/Animation.xaml";
            //ResourceDictionary dicAnimation = new ResourceDictionary { Source = new Uri(packUriAnimation, UriKind.Relative) };
            //Resources.MergedDictionaries.Add(dicAnimation);

            //StoryboardSlowShow = (Storyboard)FindResource("SlowShow");
            //StoryboardSlowHide = (Storyboard)FindResource("SlowHide");
            //绑定按钮
            BindingButton();
        }
        #endregion

        #region 绑定系统按钮事件
        Button btnClose;
        Button btnMax;
        Button btnRestore;
        Button btnMin;
        public void BindingButton()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(300);
                    btnClose = (Button)Template.FindName("PART_Close", this);
                    btnMax = (Button)Template.FindName("PART_Max", this);
                    btnRestore = (Button)Template.FindName("PART_Restore", this);
                    btnMin = (Button)Template.FindName("PART_Min", this);
                    if (btnClose != null && btnMax != null && btnRestore != null && btnMin != null)
                    {
                        btnClose.Click += delegate
                        {
                            Close();
                        };
                        btnMax.Click += delegate
                        {
                            WindowState = WindowState.Maximized;
                        };
                        btnRestore.Click += delegate
                        {
                            WindowState = WindowState.Normal;
                        };
                        btnMin.Click += delegate
                        {
                            WindowState = WindowState.Minimized;
                        };
                        break;
                    }
                }
            });
        }
        #endregion

        #region XAML动画
        /// <summary>
        /// 执行最小化窗体
        /// </summary>
        private void StoryboardHide()
        {
            //启动最小化动画
            //StoryboardSlowHide.Begin(this);
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(300);
                Dispatcher.Invoke(new Action(() =>
                {
                    WindowState = WindowState.Minimized;
                }));
            });
        }

        /// <summary>
        /// 恢复窗体
        /// </summary>
        private void WindowRestore()
        {
            //启动最小化动画
            //StoryboardSlowShow.Begin(this);
        }
        #endregion

        #region 系统函数

        IntPtr Handle = IntPtr.Zero;
        HwndSource source;
        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            Handle = new WindowInteropHelper(this).Handle;
            source = HwndSource.FromHwnd(Handle);
            if (source == null)
            { throw new Exception("Cannot get HwndSource instance."); }
            source.AddHook(new HwndSourceHook(this.WndProc));
        }
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                //获取窗口的最大化最小化信息
                case Win32.WM_GETMINMAXINFO:
                        WmGetMinMaxInfo(hwnd, lParam);
                        handled = true;
                    break;
                case Win32.WM_NCHITTEST:
                    return WmNCHitTest(lParam, ref handled);
                //case Win32.WM_SYSCOMMAND:
                //        if (wParam.ToInt32() == Win32.SC_MINIMIZE)//最小化消息
                //        {
                //            StoryboardHide();//执行最小化动画
                //            handled = true;
                //        }
                //        if (wParam.ToInt32() == Win32.SC_RESTORE)//恢复消息
                //        {
                //            WindowRestore();//执行恢复动画
                //            //handled = true;
                //        }
                //    break;
                //case Win32.WM_NCPAINT:
                //    break;
                //case Win32.WM_NCCALCSIZE:
                //    handled = true;
                //    break;
                //case Win32.WM_NCUAHDRAWCAPTION:
                //case Win32.WM_NCUAHDRAWFRAME:
                //    handled = true;
                //    break;
                //case Win32.WM_NCACTIVATE:
                //    if (wParam == (IntPtr)Win32.WM_TRUE)
                //    {
                //        handled = true;
                //    }
                //    break;
            }
            return IntPtr.Zero;
        }

        /// <summary>  
        /// 圆角拖动大小 
        /// </summary>  
        private readonly int cornerWidth = 8;

        /// <summary>  
        /// 拉伸鼠标坐标 
        /// </summary>  
        private Point mousePoint = new Point();
        IntPtr HTBOTTOMRIGHT = new IntPtr((int)Win32.HitTest.HTBOTTOMRIGHT);
        IntPtr HTRIGHT = new IntPtr((int)Win32.HitTest.HTRIGHT);
        IntPtr HTBOTTOM = new IntPtr((int)Win32.HitTest.HTBOTTOM);
        private IntPtr WmNCHitTest(IntPtr lParam, ref bool handled)
        {
            this.mousePoint.X = (int)(short)(lParam.ToInt32() & 0xFFFF);
            this.mousePoint.Y = (int)(short)(lParam.ToInt32() >> 16);
            if (ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip)
            {
                handled = true;
                //if (Math.Abs(this.mousePoint.Y - this.Top) <= this.cornerWidth
                //    && Math.Abs(this.mousePoint.X - this.Left) <= this.cornerWidth)
                //{ // 左上 
                //    return new IntPtr((int)Win32.HitTest.HTTOPLEFT);
                //}
                //else if (Math.Abs(this.ActualHeight + this.Top - this.mousePoint.Y) <= this.cornerWidth
                //    && Math.Abs(this.mousePoint.X - this.Left) <= this.cornerWidth)
                //{ // 左下  
                //    return new IntPtr((int)Win32.HitTest.HTBOTTOMLEFT);
                //}
                //else if (Math.Abs(this.mousePoint.Y - this.Top) <= this.cornerWidth
                //    && Math.Abs(this.ActualWidth + this.Left - this.mousePoint.X) <= this.cornerWidth)
                //{ //右上
                //    return new IntPtr((int)Win32.HitTest.HTTOPRIGHT);
                //}
                //else if (Math.Abs(this.mousePoint.X - this.Left) <= 30)
                //{ // 左侧边框
                //    return new IntPtr((int)Win32.HitTest.HTLEFT);
                //}
                //else if (Math.Abs(this.mousePoint.Y - this.Top) <= 30)
                //{ // 顶部  
                //    return new IntPtr((int)Win32.HitTest.HTTOP);
                //}
                if (Math.Abs(this.ActualWidth + this.Left - this.mousePoint.X) <= this.cornerWidth
                    && Math.Abs(this.ActualHeight + this.Top - this.mousePoint.Y) <= this.cornerWidth)
                { // 右下 
                    return HTBOTTOMRIGHT;
                }
                else if (Math.Abs(this.ActualWidth + this.Left - this.mousePoint.X) <= 4 && Math.Abs(this.mousePoint.Y - this.Top) > DMSystemButtonSize)
                { // 右  
                    return HTRIGHT;
                }
                else if (Math.Abs(this.ActualHeight + this.Top - this.mousePoint.Y) <= 4)
                { // 底部  
                    return HTBOTTOM;
                }
            }
            handled = false;
            return IntPtr.Zero;
        }

        //最大最小化信息
        void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            // MINMAXINFO structure  
            Win32.MINMAXINFO mmi = (Win32.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(Win32.MINMAXINFO));

            // Get handle for nearest monitor to this window  
            IntPtr hMonitor = Win32.MonitorFromWindow(Handle, Win32.MONITOR_DEFAULTTONEAREST);

            // Get monitor info   显示屏
            Win32.MONITORINFOEX monitorInfo = new Win32.MONITORINFOEX();

            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            Win32.GetMonitorInfo(new HandleRef(this, hMonitor), monitorInfo);

            // Convert working area  
            Win32.RECT workingArea = monitorInfo.rcWork;

            // Set the maximized size of the window  
            //ptMaxSize：  设置窗口最大化时的宽度、高度
            //mmi.ptMaxSize.x = (int)dpiIndependentSize.X;
            //mmi.ptMaxSize.y = (int)dpiIndependentSize.Y;

            // Set the position of the maximized window  
            mmi.ptMaxPosition.x = workingArea.Left;
            mmi.ptMaxPosition.y = workingArea.Top;

            // Get HwndSource  
            if (source == null)
                // Should never be null  
                throw new Exception("Cannot get HwndSource instance.");
            if (source.CompositionTarget == null)
                // Should never be null  
                throw new Exception("Cannot get HwndTarget instance.");

            Matrix matrix = source.CompositionTarget.TransformToDevice;

            Point dpiIndenpendentTrackingSize = matrix.Transform(new Point(
               this.MinWidth,
               this.MinHeight
               ));

            if (DMFullScreen)
            {
                Point dpiSize = matrix.Transform(new Point(
              SystemParameters.PrimaryScreenWidth,
              SystemParameters.PrimaryScreenHeight
              ));

                mmi.ptMaxSize.x = (int)dpiSize.X;
                mmi.ptMaxSize.y = (int)dpiSize.Y;
            }
            else
            {
                mmi.ptMaxSize.x = workingArea.Right;
                mmi.ptMaxSize.y = workingArea.Bottom;
            }

            // Set the minimum tracking size ptMinTrackSize： 设置窗口最小宽度、高度 
            mmi.ptMinTrackSize.x = (int)dpiIndenpendentTrackingSize.X;
            mmi.ptMinTrackSize.y = (int)dpiIndenpendentTrackingSize.Y;

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        Thickness MaxThickness = new Thickness(0);
        Thickness NormalThickness = new Thickness(20);
        
        //窗体最大化 隐藏阴影
        void MainWindow_StateChanged(object sender, EventArgs e)
        {
            //最大化
            if (WindowState == WindowState.Maximized)
            {
                if (DMShowMax)
                {
                    BtnMaxVisibility = Visibility.Collapsed;
                    BtnRestoreVisibility = Visibility.Visible;
                }
                BorderThickness = MaxThickness;
            }
            //默认大小
            if (WindowState == WindowState.Normal)
            {
                if (DMShowMax)
                {
                    BtnMaxVisibility = Visibility.Visible;
                    BtnRestoreVisibility = Visibility.Collapsed;
                }
                BorderThickness = NormalThickness;
            }
            //最小化-隐藏阴影
            if (WindowState == WindowState.Minimized)
            {
               
            }
        }
        //窗体移动
        void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Grid || e.OriginalSource is Window || e.OriginalSource is Border)
            {
                Win32.SendMessage(Handle, Win32.WM_NCLBUTTONDOWN, (int)Win32.HitTest.HTCAPTION, 0);
                return;
            }
        }

        #endregion

        #region 窗体属性


        private bool _DMFullscreen = false;
        [Description("全屏是否保留任务栏显示"), Category("DMSkin")]
        public bool DMFullScreen
        {
            get
            {
                return _DMFullscreen;
            }

            set
            {
                _DMFullscreen = value;
                OnPropertyChanged("DMFull");
            }
        }


        #region 系统按钮
        private int _DMSystemButtonSize = 30;

        [Description("窗体系统按钮大小"), Category("DMSkin")]
        public int DMSystemButtonSize
        {
            get
            {
                return _DMSystemButtonSize;
            }

            set
            {
                _DMSystemButtonSize = value;
                OnPropertyChanged("DMSystemButtonSize");
            }
        }

        private Brush _DMSystemButtonHoverColor = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));

        [Description("窗体系统按钮鼠标悬浮背景颜色"), Category("DMSkin")]
        public Brush DMSystemButtonHoverColor
        {
            get
            {
                return _DMSystemButtonHoverColor;
            }

            set
            {
                _DMSystemButtonHoverColor = value;
                OnPropertyChanged("DMSystemButtonHoverColor");
            }
        }

        private Brush _DMSystemButtonCloseHoverColor = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));

        [Description("窗体系统关闭按钮鼠标悬浮背景颜色"), Category("DMSkin")]
        public Brush DMSystemButtonCloseHoverColor
        {
            get
            {
                return _DMSystemButtonCloseHoverColor;
            }

            set
            {
                _DMSystemButtonCloseHoverColor = value;
                OnPropertyChanged("DMSystemButtonCloseHoverColor");
            }
        }


        private double _DMSystemButtonShadowEffect = 1.0;
        [Description("窗体控制按钮阴影大小"), Category("DMSkin")]
        public double DMSystemButtonShadowEffect
        {
            get
            {
                return _DMSystemButtonShadowEffect;
            }

            set
            {
                _DMSystemButtonShadowEffect = value;
                OnPropertyChanged("DMSystemButtonShadowEffect");
            }
        }

        private Brush _DMSystemButtonForeground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        [Description("窗体系统按钮颜色"), Category("DMSkin")]
        public Brush DMSystemButtonForeground
        {
            get
            {
                return _DMSystemButtonForeground;
            }

            set
            {
                _DMSystemButtonForeground = value;
                OnPropertyChanged("DMSystemButtonForeground");
            }
        }

        private Brush _DMSystemButtonHoverForeground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        [Description("窗体系统按钮鼠标悬按钮颜色"), Category("DMSkin")]
        public Brush DMSystemButtonHoverForeground
        {
            get
            {
                return _DMSystemButtonHoverForeground;
            }

            set
            {
                _DMSystemButtonHoverForeground = value;
                OnPropertyChanged("DMSystemButtonHoverForeground");
            }
        }


        private bool dmShowMax = true;
        [Description("显示最大化按钮"), Category("DMSkin")]
        public bool DMShowMax
        {
            get
            {
                return dmShowMax;
            }

            set
            {
                dmShowMax = value;
                if (dmShowMax)
                {
                    ResizeMode = ResizeMode.CanResize;
                    BtnMaxVisibility = Visibility.Visible;
                    BtnRestoreVisibility = Visibility.Collapsed;
                }
                else
                {
                    ResizeMode = ResizeMode.CanMinimize;
                    BtnMaxVisibility = Visibility.Collapsed;
                    BtnRestoreVisibility = Visibility.Collapsed;
                }

                OnPropertyChanged("DMShowMax");
            }
        }

        private bool dmShowMin = true;
        [Description("显示最小化按钮"), Category("DMSkin")]
        public bool DMShowMin
        {
            get
            {
                return dmShowMin;
            }

            set
            {
                dmShowMin = value;
                if (dmShowMin)
                {
                    ResizeMode = ResizeMode.CanResize;
                    BtnMinVisibility = Visibility.Visible;
                }
                else
                {
                    ResizeMode = ResizeMode.CanMinimize;
                    BtnMinVisibility = Visibility.Collapsed;
                }
                OnPropertyChanged("DMShowMin");
            }
        }


        private bool dmShowClose = true;
        [Description("显示关闭按钮"), Category("DMSkin")]
        public bool DMShowClose
        {
            get
            {
                return dmShowClose;
            }

            set
            {
                dmShowClose = value;
                if (dmShowClose)
                {
                    BtnCloseVisibility = Visibility.Visible;
                }
                else
                {
                    BtnCloseVisibility = Visibility.Collapsed;
                }
                OnPropertyChanged("DMShowClose");
            }
        }
        #endregion

        private Visibility btnMinVisibility = Visibility.Visible;
        //最小化按钮显示
        public Visibility BtnMinVisibility
        {
            get
            {
                return btnMinVisibility;
            }

            set
            {
                btnMinVisibility = value;
                OnPropertyChanged("BtnMinVisibility");
            }
        }

        private Visibility btnCloseVisibility = Visibility.Visible;
        //关闭按钮显示
        public Visibility BtnCloseVisibility
        {
            get
            {
                return btnCloseVisibility;
            }

            set
            {
                btnCloseVisibility = value;
                OnPropertyChanged("BtnCloseVisibility");
            }
        }

        private Visibility btnMaxVisibility = Visibility.Visible;
        //最大化按钮显示
        public Visibility BtnMaxVisibility
        {
            get
            {
                return btnMaxVisibility;
            }

            set
            {
                btnMaxVisibility = value;
                OnPropertyChanged("BtnMaxVisibility");
            }
        }

        private Visibility btnRestoreVisibility = Visibility.Collapsed;
        //最大化按钮显示
        public Visibility BtnRestoreVisibility
        {
            get
            {
                return btnRestoreVisibility;
            }

            set
            {
                btnRestoreVisibility = value;
                OnPropertyChanged("BtnRestoreVisibility");
            }
        }

        private int _DMWindowShadowSize = 10;
        [Description("窗体阴影大小"), Category("DMSkin")]
        public int DMWindowShadowSize
        {
            get
            {
                    return _DMWindowShadowSize;
            }

            set
            {
                    _DMWindowShadowSize = value;
                    OnPropertyChanged("DMWindowShadowSize");
            }
        }

        private Color _DMWindowShadowColor = Color.FromArgb(255, 200, 200, 200);
        [Description("窗体阴影颜色"), Category("DMSkin")]
        public Color DMWindowShadowColor
        {
            get
            {
                
                    return _DMWindowShadowColor;
            }

            set
            {
                    _DMWindowShadowColor = value;
                   OnPropertyChanged("DMWindowShadowColor");
            }
        }
        #endregion
    }
}

