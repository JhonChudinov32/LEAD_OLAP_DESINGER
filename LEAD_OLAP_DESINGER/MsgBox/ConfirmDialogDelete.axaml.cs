using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace LEAD_OLAP_DESINGER;

public partial class ConfirmDialogDelete : Window
{
    public ConfirmDialogDelete()
    {
        this.InitializeComponent();
        this.FindControl<Button>("ConfirmButton").Click += (sender, e) => this.Close(true);
        this.FindControl<Button>("CancelButton").Click += (sender, e) => this.Close(false);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    private void CloseButtonPointerEnter(object sender, PointerEventArgs e)
    {
        Border border = (Border)sender;
        border.Background = new SolidColorBrush(Color.FromRgb(196, 43, 28));
        var textBlock = border.Child as TextBlock;
        textBlock.Foreground = new SolidColorBrush(Colors.White);
    }
    private void CloseButtonPointerLeave(object sender, PointerEventArgs e)
    {
        Border border = (Border)sender;
        border.Background = new SolidColorBrush(Colors.Transparent);
        var textBlock = border.Child as TextBlock;
        textBlock.Foreground = new SolidColorBrush(Colors.Black);
    }
    private void CloseButtonPointerPressed(object sender, PointerPressedEventArgs e)
    {
        Border border = (Border)sender;
        border.Background = new SolidColorBrush(Color.FromRgb(196, 43, 28));
        var textBlock = border.Child as TextBlock;
        textBlock.Foreground = new SolidColorBrush(Colors.White);
    }
    private void CloseButtonPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        this.Close();
    }

}