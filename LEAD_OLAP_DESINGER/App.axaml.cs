using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using LEAD_OLAP_DESINGER.ViewModels;
using LEAD_OLAP_DESINGER.Views;
using System;
using System.Linq;

namespace LEAD_OLAP_DESINGER
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(RequestedThemeVariant),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }


        /// <summary>
        /// Метод для замены или добавления нового стиля в коллекции.
        /// </summary>
        private void SetPaletteStyle(IStyle? oldStyle, IStyle newStyle)
        {
            if (oldStyle != null && newStyle != null && !Styles.Contains(newStyle))
            {
                if (Styles.Contains(oldStyle))
                {
                    Styles[Styles.IndexOf(oldStyle)] = newStyle; // Заменяем стиль
                }
                else
                {
                    Styles.Add(newStyle); // Добавляем новый стиль
                }
            }
        }
    }
}