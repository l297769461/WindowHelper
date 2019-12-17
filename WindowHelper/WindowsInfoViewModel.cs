using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowHelper
{
    public class WindowsInfoViewModel : INotifyPropertyChanged
    {
        private int _State = 0;
        private int _keyCode = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 键盘的keycode
        /// </summary>
        public int KeyCode
        {
            get => _keyCode;
            set
            {
                _keyCode = value;
                PropertyChanged?.Notify(() => KeyStr);
            }
        }

        /// <summary>
        /// 键盘的key字符串
        /// </summary>
        public string KeyStr
        {
            get
            {
                if (KeyCode == -1)
                    return string.Empty;
                return ((Keys)KeyCode).ToString();
            }
        }

        /// <summary>
        /// 状态；0：默认；1：正常；2：禁用
        /// </summary>
        public int State
        {
            get => _State;
            set
            {
                _State = value;
                PropertyChanged?.Notify(() => State);
            }
        }

        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr WindowPtr { get; set; }
    }
}
