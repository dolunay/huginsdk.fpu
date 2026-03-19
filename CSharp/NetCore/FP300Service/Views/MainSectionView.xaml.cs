using System.Windows.Controls;

namespace FP300NetCoreService.Views;

public partial class MainSectionView : UserControl
{
    public MainSectionView(string title, string description)
    {
        InitializeComponent();
        txtTitle.Text = title;
        txtDescription.Text = description;
    }
}
