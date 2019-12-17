using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WindowHelper
{
    /// <summary>
    /// SetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SetWindow : Window
    {
        Action<bool, IntPtr> action;

        //根据坐标获取窗口句柄
        [DllImport("user32")]
        private static extern IntPtr WindowFromPoint(POINTAPI Point);

        [StructLayout(LayoutKind.Sequential)]//定义与API相兼容结构体，实际上是一种内存转换
        public struct POINTAPI
        {
            public int X;
            public int Y;
        }

        public SetWindow(Action<bool, IntPtr> action)
        {
            InitializeComponent();
            this.action = action;

            Size size = new Size();
            //获取所有有效屏幕
            foreach (var item in Screen.AllScreens)
            {
                size.Width += item.Bounds.Width;

                if (item.Bounds.Height > size.Height)
                {
                    size.Height = item.Bounds.Height;
                }
            }

            Left = 0;
            Top = 0;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        /// 左键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(this);
            var p = PointToScreen(point);

            Hide();

            var pp = new POINTAPI();
            pp.X = (int)p.X;
            pp.Y = (int)p.Y;
            var w = WindowFromPoint(pp);
            if (w != null)
            {
                action?.Invoke(true, w);
            }
            else
            {
                action?.Invoke(true, IntPtr.Zero);
            }
            Close();
        }

        /// <summary>
        /// 右键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            action?.Invoke(false, IntPtr.Zero);
            Close();
        }
    }
}
