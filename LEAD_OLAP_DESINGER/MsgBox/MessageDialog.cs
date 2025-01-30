using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia;
using System.Linq;
using Avalonia.LogicalTree;

namespace LEAD_OLAP_DESINGER.MsgBox
{
    public static class MessageDialog
    {
        public static void Show(string title, string message)
        {
            Window S = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                MinWidth = 150,
                MinHeight = 150,
                FontSize = 13,
                Content = new StackPanel
                {
                    MaxHeight = 400,
                    MaxWidth = 400,
                    Children = {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    MinHeight = 30,
                    Margin = new Thickness(30),
                },
                new Button
                {
                    Content = "Продолжить",
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Width = 150,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Thickness(5),
                }
            }
                },
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var button = (Button)((StackPanel)S.Content).Children.Last();
            button.Click += Next_Click;

            var lifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (lifetime?.MainWindow != null)
            {
                S.ShowDialog(lifetime.MainWindow);
            }
            else
            {
                S.Show(); // Если MainWindow не установлено
            }
        }

        private static void Next_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var window = button.GetLogicalAncestors()
                    .OfType<Window>()
                    .FirstOrDefault();

                if (window != null)
                {
                    window.Close();
                }
            }
        }
    }
}
