using Hugin.Common;
using System.Windows;

namespace FP300Service.Views;

public partial class ServiceSEIView : Window
{
    private static Service? service;

    public ServiceSEIView()
    {
        InitializeComponent();
        SetLanguageOptions();
        service = null;
    }

    public static Service? CurrentService => service;

    private void SetLanguageOptions()
    {
        labelServiceDefinition.Text = FormMessage.SERVICE_DEFINITION;
        labelServiceBrutAmount.Text = FormMessage.BRUT_AMOUNT;
        labelServiceVATRate.Text = FormMessage.VAT_RATE;
        labelWageRate.Text = FormMessage.WAGE_RATE;
        labelServiceStoppageRate.Text = FormMessage.STOPPAGE_RATE;
        buttonAdd.Content = FormMessage.ADD;
        buttonClear.Content = FormMessage.CLEAR;
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        service = new Service
        {
            Definition = textBoxServiceDefinition.Text,
            BrutAmount = nmrBrutAmount.Value,
            StoppageRate = (int)nmrStoppageRate.Value,
            VATRate = (int)nmrVATRate.Value,
            WageRate = (int)nmrWageRate.Value
        };

        DialogResult = true;
        Close();
    }

    private void ButtonClear_OnClick(object sender, RoutedEventArgs e)
    {
        service = null;
        textBoxServiceDefinition.Clear();
    }
}
