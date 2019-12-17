using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowHelper
{
    public class WindowHelperViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<WindowsInfoViewModel> _datas;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 窗口集合
        /// </summary>
        public ObservableCollection<WindowsInfoViewModel> Datas
        {
            get => _datas;
            set
            {
                _datas = value;
                PropertyChanged?.Notify(() => Datas);
            }
        }
    }
}
