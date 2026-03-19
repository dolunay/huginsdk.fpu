using System.Windows;

namespace FP300Service.Views;

public partial class InputBox : Window
{
    public string input = string.Empty;

    public InputBox(string caption)
    {
        InitializeComponent();
        lblText.Content = caption;
    }

    public InputBox(string caption, int maxLen)
        : this(caption)
    {
        txtInput.MaxLength = maxLen;
    }

    private void BtnOK_OnClick(object sender, RoutedEventArgs e)
    {
        input = txtInput.Text;
        DialogResult = true;
        Close();
    }
}
