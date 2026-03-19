using Hugin.Common;
using System.Windows;

namespace FP300Service.Views;

public partial class CustomerView : Window
{
    private static Customer? customer;

    public CustomerView()
    {
        InitializeComponent();
        SetLanguageOptions();
        customer = null;
    }

    public static Customer? CurrentCustomer => customer;

    private void SetLanguageOptions()
    {
        labelCustomerName.Text = FormMessage.CUSTOMER_NAME;
        labelCustomerTaxOffice.Text = FormMessage.TAX_OFFICE;
        labelCustomerAddress1.Text = FormMessage.ADDRESS_LINE + " 1";
        labelCustomerAddress2.Text = FormMessage.ADDRESS_LINE + " 2";
        labelCustomerAddress3.Text = FormMessage.ADDRESS_LINE + " 3";
        labelCustomerAddress4.Text = FormMessage.ADDRESS_LINE + " 4";
        labelCustomerAddress5.Text = FormMessage.ADDRESS_LINE + " 5";
        labelCustomerLabel.Text = FormMessage.LABEL;
        buttonAdd.Content = FormMessage.ADD;
        buttonClear.Content = FormMessage.CLEAR;
        Title = "CustomerForm";
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        customer = new Customer
        {
            TCKN_VKN = textBoxCustomerTCKNVKN.Text,
            Name = textBoxCustomerName.Text,
            Label = textBoxCustomerLabel.Text,
            TaxOffice = textBoxCustomerTaxOffice.Text,
            AddressList = new List<string>
            {
                textBoxCustomerAddress1.Text,
                textBoxCustomerAddress2.Text,
                textBoxCustomerAddress3.Text,
                textBoxCustomerAddress4.Text,
                textBoxCustomerAddress5.Text
            }
        };

        for (var i = customer.AddressList.Count; i > 0; i--)
        {
            var currentIndex = i - 1;
            if (customer.AddressList[currentIndex].Length == 0)
            {
                customer.AddressList.RemoveAt(currentIndex);
            }
            else
            {
                break;
            }
        }

        DialogResult = true;
        Close();
    }

    private void ButtonClear_OnClick(object sender, RoutedEventArgs e)
    {
        customer = null;
        textBoxCustomerAddress1.Clear();
        textBoxCustomerAddress2.Clear();
        textBoxCustomerAddress3.Clear();
        textBoxCustomerAddress4.Clear();
        textBoxCustomerAddress5.Clear();
        textBoxCustomerName.Clear();
        textBoxCustomerTaxOffice.Clear();
        textBoxCustomerTCKNVKN.Clear();
    }
}
