using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LEAD_OLAP_DESINGER.ViewModels;
using System.Collections.Generic;

namespace LEAD_OLAP_DESINGER;

public partial class ChooseTableDialog : Window
{

    public string CurrentValue { get; set; }

    public ListBox tableList;

    public List<string> Tables { get; set; } // List of table names

    public ChooseTableDialog()
    {
        InitializeComponent();

        var acceptButton = this.FindControl<Button>("AcceptButton");
        var cancelButton = this.FindControl<Button>("CancelButton");
        tableList = this.FindControl<ListBox>("TableList");

        acceptButton.Click += AcceptButton_Click;
        cancelButton.Click += CancelButton_Click;
        tableList.SelectionChanged += TableList_SelectionChanged;
        MainWindowViewModel.IsAccepted = false;

    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AcceptButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (tableList.SelectedItem != null)
        {
            MainWindowViewModel.IsAccepted = true;
            CurrentValue = tableList.SelectedItem.ToString();
        }
        this.Close();
    }

    private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainWindowViewModel.IsAccepted = false;
        this.Close();
    }

    private void TableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var aliasTextBox = this.FindControl<TextBox>("AliasTextBox");
        if (sender is ListBox listBox && listBox.SelectedItem != null)
        {
            aliasTextBox.Text = listBox.SelectedItem.ToString();
        }
    }
}