using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WindowHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        #region 变量
        private const int SW_HIDE = 0;

        private const int SW_NORMAL = 1;

        /// <summary>
        /// 最大化
        /// </summary>
        private const int SW_MAXIMIZE = 3;

        private const int SW_SHOWNOACTIVATE = 4;

        private const int SW_SHOW = 5;

        /// <summary>
        /// 最小化
        /// </summary>
        private const int SW_MINIMIZE = 6;

        /// <summary>
        /// 还原
        /// </summary>
        private const int SW_RESTORE = 9;

        private const int SW_SHOWDEFAULT = 10;

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        /// <summary>
        /// 钩子对象
        /// </summary>
        UserActivityHook hook = new UserActivityHook();

        /// <summary>
        /// 右键菜单绑定
        /// </summary>
        ContextMenu menu = new ContextMenu();
        #endregion

        #region 构造函数

        public MainWindow()
        {
            InitializeComponent();

            hook.KeyDown += Hook_KeyDown;
            hook.Start(false, true);

            Closing += MainWindow_Closing;

            Left = 10;
            Top = 50;

            DataContext = new WindowHelperViewModel();

            vm.Datas = new System.Collections.ObjectModel.ObservableCollection<WindowsInfoViewModel>();
            
            var str = "窗口快捷切换助手\r\n抱歉，兼容性问题，\r\n请保持窗口打开或最大化，\r\n别最小化！！！谢谢~";
            var notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.BalloonTipText = str;
            notifyIcon.Text = str;
            notifyIcon.Icon = Properties.Resources.切换;
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(3000);

            MenuItem item = new MenuItem();
            item.Header = "禁用/启用";
            item.Click += Item_Click;
            menu.Items.Add(item);
        }

        ~MainWindow()
        {
            Dispose();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 当前上下文
        /// </summary>
        public WindowHelperViewModel vm => DataContext as WindowHelperViewModel;

        #endregion

        #region 自定义事件
        #endregion

        #region 私有方法
        #endregion

        #region 公共/保护方法
        public void Dispose()
        {
            hook.Stop(true, true);
        }

        #endregion

        #region 事件方法

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Addbtn_Click(object sender, RoutedEventArgs e)
        {
            var code = vm.Datas.Count + 49;// 1-9
            if (code > 57)
            {
                //A-Z   65-90
                code = code - 57 - 1 + 65;

                if (code > 90)
                {
                    MessageBox.Show("抱歉，暂时只支持1-9 A-Z个窗口切换！");
                    return;
                }
            }

            var info = new WindowsInfoViewModel();
            info.KeyCode = code;

            vm.Datas.Insert(0, info);
        }

        /// <summary>
        /// 清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clearbtn_Click(object sender, RoutedEventArgs e)
        {
            vm.Datas.Clear();
        }

        /// <summary>
        /// 设置窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is FrameworkElement ele && ele.DataContext is WindowsInfoViewModel vmItem)
            {
                Hide();
                new SetWindow((pd, ptr) =>
                {
                    if (pd)
                    {
                        if (ptr.Equals(IntPtr.Zero))
                        {
                            vmItem.State = 0;
                            MessageBox.Show("窗口句柄设置异常，请重试！");
                        }
                        else
                        {
                            vmItem.WindowPtr = ptr;
                            vmItem.State = 1;
                        }
                    }
                }).ShowDialog();
                Show();
            }
        }

        /// <summary>
        /// 按键钩子
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Debug.WriteLine($"{e.KeyCode} - {e.KeyValue} - {e.KeyCode.ToString()}");

            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                var vmItem = vm.Datas.Where(c => c.KeyCode == e.KeyValue).FirstOrDefault();
                if (vmItem != null && vmItem.State == 1 && !vmItem.WindowPtr.Equals(IntPtr.Zero))
                {
                    e.Handled = true;

                    var ptr = vmItem.WindowPtr;
                    Task.Run(() =>
                    {
                        //System.Threading.Thread.Sleep(100);
                        //ShowWindowAsync(ptr, 4);

                        System.Threading.Thread.Sleep(100);
                        SwitchToThisWindow(ptr, true);
                    });
                }
            }
        }

        /// <summary>
        /// 窗口关闭时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Dispose();
        }

        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minbtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 鼠标右键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bd_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is WindowsInfoViewModel vmItem)
            {
                if (vmItem.State == 0) return;

                foreach (MenuItem item in menu.Items)
                {
                    item.Tag = vmItem;
                }
            }

            menu.IsOpen = true;
        }

        /// <summary>
        /// 禁用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is WindowsInfoViewModel vmItem)
            {
                vmItem.State = vmItem.State == 1 ? 2 : 1;
            }
        }
        #endregion

    }
}
