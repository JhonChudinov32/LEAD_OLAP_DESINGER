
using Avalonia.Controls;
using Avalonia.Media.Imaging;

using LEAD_OLAP_DESINGER.ViewModels;


namespace LEAD_OLAP_DESINGER.Views
{
    public partial class ProcessesL : Window
    {
        private Image loadingImages;
        public ProcessesL()
        {
            InitializeComponent();
            loadingImages = this.FindControl<Image>("loadingImage");
            loadingImages.Source = new Bitmap("Assets/loading.gif");

        }

    }
}

