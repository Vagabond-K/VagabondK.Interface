using VagabondK.Windows;

namespace WpfBindingSample
{
    public partial class MainWindow : ThemeWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
