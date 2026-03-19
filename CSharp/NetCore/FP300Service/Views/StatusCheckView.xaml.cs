using System.Windows.Controls;

namespace FP300Service.Views;

public partial class StatusCheckView : UserControl
{
    public StatusCheckView(bool isConnected, string fiscalId, string connectionType)
    {
        InitializeComponent();

        txtConnectionState.Text = isConnected
            ? "Bağlantı durumu: Bağlı"
            : "Bağlantı durumu: Bağlı değil";

        txtConnectionType.Text = $"Bağlantı tipi: {connectionType}";
        txtFiscalInfo.Text = $"Mali ID: {fiscalId}";
    }
}
