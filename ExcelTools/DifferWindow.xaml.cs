using ExcelTools.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace ExcelTools
{
    /// <summary>
    /// DifferWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DifferWindow : Window
    {
        private ObservableCollection<DiffItem> _diffItems;

        public static readonly RoutedEvent ChooseEvent;

        public DifferWindow(String title, ObservableCollection<DiffItem> items)
        {
            Title += "请选择需要执行的修改__" + title;
            _diffItems = items;
            InitializeComponent();
            commitBtn.IsEnabled = false;
            diffListView.SelectionChanged += DiffItem_SelectedChanged;
        }

        private void DiffItem_SelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            if(diffListView.SelectedItems.Count > 0)
            {
                commitBtn.IsEnabled = true;
            }
            else
            {
                commitBtn.IsEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            diffListView.ItemsSource = _diffItems;
        }

        private void CommitBtn_Click(object sender, RoutedEventArgs e)
        {
            //TODO:
            //1、生成配置
            //2、将本地临时excel按所选行修改，并重命名
            //3、两个都commit
        }
    }
}
