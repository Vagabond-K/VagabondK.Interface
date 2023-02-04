using VagabondK.Windows;

namespace WpfModbusSlaveSample
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
