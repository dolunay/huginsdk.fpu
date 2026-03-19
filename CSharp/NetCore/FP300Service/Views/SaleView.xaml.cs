using Hugin.Common;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;

namespace FP300Service.Views;

internal enum AdjustmentType : sbyte
{
    Fee,
    PercentFee,
    Discount,
    PercentDiscount
}

internal enum InfoReceiptPaymentType
{
    CASH = 1,
    CREDIT,
    CHECK,
    EFT_POS,
    FOREIGN_CURRENCY,
    FOODCARD
}

public partial class SaleView : UserControl
{
    private readonly IBridge _bridge;
    private Customer? _returnCustomer;
    private readonly DispatcherTimer _autoPrintTimer = new();
    private readonly DispatcherTimer _autoPrintCountdownTimer = new();
    private int _autoPrintDocCounter = 1;
    private int _autoPrintCountdownMs;
    private bool _autoPrintDeptMode;
    private string _cachedJsonDocument = string.Empty;

    public SaleView(IBridge bridge)
    {
        InitializeComponent();
        _bridge = bridge;

        cbxInvTypes.ItemsSource = Common.InvDocTypes;
        cbxInvTypes.SelectedIndex = 0;

        dpInvoiceIssueDate.SelectedDate = DateTime.Today;
        dpParkDate.SelectedDate = DateTime.Today;
        dpCollectionInvDate.SelectedDate = DateTime.Today;
        dpCrrAccDate.SelectedDate = DateTime.Today;
        dpRetDocDate.SelectedDate = DateTime.Today;

        txtParkTime.Text = DateTime.Now.ToString("HH:mm");
        comboBoxEDocumentDocTypes.ItemsSource = Common.EDocumentTypes;
        comboBoxEDocumentDocTypes.SelectedIndex = 0;

        InitializePaymentTypes();
        SetLanguageOption();

        _autoPrintTimer.Tick += AutoPrintTimer_OnTick;
        _autoPrintCountdownTimer.Tick += AutoPrintCountdownTimer_OnTick;

        panelVoidEft.IsEnabled = false;
        UpdatePriceInputState();
        UpdateCommissionState();
        UpdateVoidSaleMode();
        UpdateSlipCopyMode();
        UpdateStartDocPanelVisibility();
        UpdateSalePanelVisibility();
        UpdatePaymentPanelVisibility();
        UpdateFooterPanelVisibility();
    }

    private void SetLanguageOption()
    {
        tbStrtRcpt.Header = FormMessage.START_DOCUMENT;
        tbpSale.Header = FormMessage.SALE;
        tbpVoidSale.Header = FormMessage.VOID_SALE;
        tbpAdj.Header = FormMessage.ADJUSTMENT;

        tbpPay1.Header = FormMessage.PAYMENT;
        tbpPayEFT.Header = FormMessage.EFT_PAYMENT;
        tbpVoidPay.Header = FormMessage.VOID_PAYMENT;

        btnStartReceipt.Content = FormMessage.START_DOCUMENT;
        btnStartInvoice.Content = FormMessage.START_DOCUMENT;
        btnPaidDoc.Content = FormMessage.START_DOCUMENT;
        btnStartParkDoc.Content = FormMessage.START_DOCUMENT;
        buttonStartFoodDoc.Content = FormMessage.START_DOCUMENT;
        buttonStartCllctnDoc.Content = FormMessage.START_DOCUMENT;
        btnPrintCurrAccHeader.Content = FormMessage.START_DOCUMENT;
        buttonPrintEDocumentSample.Content = "PRINT E-DOCUMENT SAMPLE";
        buttonLoadInvoiceFile.Content = "LOAD FILE";
        buttonSendTestData.Content = "SEND TEST DATA";
        buttonStartReturnDoc.Content = FormMessage.START_DOCUMENT;
        buttonAddCustomerRetDoc.Content = FormMessage.ADD_CUSTOMER;

        btnCloseReceipt.Content = FormMessage.CLOSE_DOCUMENT;
        btnVoidReceipt.Content = FormMessage.VOID_DOCUMENT;
        btnSubtotal.Content = FormMessage.SUBTOTAL;
        btnOpenDrawer.Content = FormMessage.OPEN_DRAWER;
        btnCorrect.Content = FormMessage.CORRECT;
        btnReceiptInfo.Content = "RECEIPT INFO";
        btnPrintJsonDocument.Content = FormMessage.PRINT_JSON;
        btnPrintJsonDept.Content = "PRINT JSON DEPT";
        btnPrintSalesDocument.Content = "PRINT SALE DOCUMENT";
        btnAutoPrintJsonDept.Content = "AUTO PRINT    JSON DEPT";
        btnAutoPrintJsonPlu.Content = "AUTO PRINT    JSON PLU";

        btnSale.Content = FormMessage.SALE;
        btnVoidSale.Content = FormMessage.VOID_SALE;
        cbxSaveAndSale.Content = FormMessage.SAVE_AND_SALE;
        lblAmount.Text = FormMessage.PRICE;
        lblVoidPlu.Text = FormMessage.PLU;
        lblVoidDeptName.Text = "DEPT NAME";

        lblPayType.Text = FormMessage.PAYMENT_TYPE;
        lblPayIndx.Text = FormMessage.PAYMENT_INDEX;
        lblPayAmount.Text = FormMessage.PAYMENT_AMOUNT;
        btnPayment.Content = FormMessage.PAYMENT;

        lblEftAmount.Text = FormMessage.AMOUNT;
        lblInstallment.Text = FormMessage.INSTALLMENT;
        lblCardNum.Text = FormMessage.CARD_NUMBER;
        btnCardQuery.Content = FormMessage.CHECK_CARD;
        btnEFTAuthorization.Content = FormMessage.EFT_PAYMENT;

        rbtnFee.Content = FormMessage.FEE;
        rbtnDiscount.Content = FormMessage.DISCOUNT;
        cbxPerc.Content = FormMessage.PERCENTAGE;
        btnAdjust.Content = FormMessage.ADJUST;

        lblVoidPayIndex.Text = FormMessage.PAYMENT_INDEX;
        checkBoxVoidEft.Content = FormMessage.VOID_EFT;
        btnVoidPayment.Content = FormMessage.VOID_PAYMENT;
        lblAcquierId.Text = FormMessage.ACQUIER_ID;
        lblBatchNo.Text = "BATCH NO";
        lblStanNo.Text = "STAN NO";
    }

    private void UpdatePriceInputState()
    {
        var enabled = checkBoxForPrice.IsChecked == true;
        txtPrice.IsEnabled = enabled;
        lblAmount.IsEnabled = enabled;
    }

    private void UpdateCommissionState()
    {
        txtComission.IsEnabled = checkBoxComission.IsChecked == true;
    }

    private void UpdateVoidSaleMode()
    {
        var isVoidDept = cbxVoidDept.IsChecked == true;
        panelVoidDeptName.Visibility = isVoidDept ? Visibility.Visible : Visibility.Collapsed;
        lblVoidPlu.Text = isVoidDept ? FormMessage.DEPT_ID : FormMessage.PLU;
    }

    private void UpdateSlipCopyMode()
    {
        var useZNoAndReceiptNo = checkBoxSlpCpyZnoRcptNo.IsChecked == true;

        lblSlipCopyZNo.IsEnabled = useZNoAndReceiptNo;
        lblSlipCopyReceiptNo.IsEnabled = useZNoAndReceiptNo;
        txtSlipCopyZNo.IsEnabled = useZNoAndReceiptNo;
        txtSlipCopyReceiptNo.IsEnabled = useZNoAndReceiptNo;

        lblSlipCopyBatchNo.IsEnabled = !useZNoAndReceiptNo;
        lblSlipCopyStanNo.IsEnabled = !useZNoAndReceiptNo;
        txtSlipCopyBatchNo.IsEnabled = !useZNoAndReceiptNo;
        txtSlipCopyStanNo.IsEnabled = !useZNoAndReceiptNo;
    }

    private void CheckBoxForPrice_OnChanged(object sender, RoutedEventArgs e) => UpdatePriceInputState();
    private void CheckBoxComission_OnChanged(object sender, RoutedEventArgs e) => UpdateCommissionState();
    private void CbxVoidDept_OnChanged(object sender, RoutedEventArgs e) => UpdateVoidSaleMode();
    private void CheckBoxSlpCpyZnoRcptNo_OnChanged(object sender, RoutedEventArgs e) => UpdateSlipCopyMode();

    private void BtnAutoPrintJsonDept_OnClick(object sender, RoutedEventArgs e) => ToggleAutoPrint(true);
    private void BtnAutoPrintJsonPlu_OnClick(object sender, RoutedEventArgs e) => ToggleAutoPrint(false);

    private void InitializePaymentTypes()
    {
        cbxPaymentType.ItemsSource = Common.Payments;
        cbxPaymentType.SelectedIndex = 0;
        cbxSubPayments.IsEnabled = false;
    }

    private void TbcStartDoc_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateStartDocPanelVisibility();
    private void TbcSale_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSalePanelVisibility();
    private void TbcPayment_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePaymentPanelVisibility();
    private void TbcFooter_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateFooterPanelVisibility();

    private void UpdateStartDocPanelVisibility()
    {
        panelStartReceipt.Visibility = tbStrtRcpt.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartInvoice.Visibility = tbStrtInvoice.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartAdvance.Visibility = tabAdvance.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartParking.Visibility = tabParking.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartFood.Visibility = tabFood.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartCollection.Visibility = tabCollectionInv.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartCure.Visibility = tabPageCrrAccountCollctn.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartEDocument.Visibility = tabPageEDocument.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartDataTest.Visibility = tabPageDataTest.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelStartReturnDoc.Visibility = tabPageReturnDoc.IsSelected ? Visibility.Visible : Visibility.Collapsed;

        var showPlaceholder = !(tbStrtRcpt.IsSelected || tbStrtInvoice.IsSelected || tabAdvance.IsSelected || tabParking.IsSelected || tabFood.IsSelected || tabCollectionInv.IsSelected || tabPageCrrAccountCollctn.IsSelected || tabPageEDocument.IsSelected || tabPageDataTest.IsSelected || tabPageReturnDoc.IsSelected);
        panelStartPlaceholder.Visibility = showPlaceholder ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateSalePanelVisibility()
    {
        panelSaleMain.Visibility = tbpSale.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelVoidSale.Visibility = tbpVoidSale.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelAdjustment.Visibility = tbpAdj.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelSaleDept.Visibility = tpgSaleDept.IsSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdatePaymentPanelVisibility()
    {
        panelCashPayment.Visibility = tbpPay1.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelEftPayment.Visibility = tbpPayEFT.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelVoidPay.Visibility = tbpVoidPay.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelRefund.Visibility = tabRefund.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelBankList.Visibility = tabPageBankList.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelEftSlipCopy.Visibility = tabPageEftSlipCopy.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelExternalSlip.Visibility = tabSlipExternal.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelPaymentPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void UpdateFooterPanelVisibility()
    {
        panelFooterNotes.Visibility = tbpNotes.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelFooterExtra.Visibility = tbpExtra.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelFooterBarcode.Visibility = tabPageBarcode.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        panelFooterStoppage.Visibility = tabPageStoppage.IsSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnStartDocument_OnClick(object sender, RoutedEventArgs e) => Run(() => _bridge.Printer.PrintDocumentHeader(), FormMessage.DOCUMENT_ID);

    private void BtnStartInvoice_OnClick(object sender, RoutedEventArgs e)
    {
        var invType = (cbxInvTypes.SelectedIndex < 0 ? 0 : cbxInvTypes.SelectedIndex) + 1;
        var issueDate = dpInvoiceIssueDate.SelectedDate ?? DateTime.Today;
        Run(() => _bridge.Printer.PrintDocumentHeader(invType, txtTCKN_VKN.Text, txtInvoiceSerial.Text, issueDate), FormMessage.DOCUMENT_ID);
    }

    private void BtnPaidDoc_OnClick(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtAdvanceAmount.Text, out var amount))
        {
            txtStatus.Text = "Geçerli bir avans tutarı giriniz.";
            return;
        }

        Run(() => _bridge.Printer.PrintAdvanceDocumentHeader(txtAdvanceTckn.Text, txtAdvanceName.Text, amount), FormMessage.DOCUMENT_ID);
    }

    private void CheckBoxVoidEft_OnCheckedChanged(object sender, RoutedEventArgs e)
    {
        panelVoidEft.IsEnabled = checkBoxVoidEft.IsChecked == true;
    }

    private void BtnStartParkDoc_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TimeSpan.TryParse(txtParkTime.Text, out var time))
        {
            txtStatus.Text = "Geçerli park giriş saati giriniz.";
            return;
        }

        var date = dpParkDate.SelectedDate ?? DateTime.Today;
        var entry = new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, 0);
        Run(() => _bridge.Printer.PrintParkDocument(txtPlate.Text, entry), FormMessage.DOCUMENT_ID);
    }

    private void ButtonStartFoodDoc_OnClick(object sender, RoutedEventArgs e) => Run(() => _bridge.Printer.PrintFoodDocumentHeader(), FormMessage.DOCUMENT_ID);

    private void ButtonStartCllctnDoc_OnClick(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtCollectionAmount.Text, out var amount))
        {
            txtStatus.Text = "Geçerli tahsilat tutarı giriniz.";
            return;
        }

        var comission = 0m;
        if (checkBoxComission.IsChecked == true && !decimal.TryParse(txtComission.Text, out comission))
        {
            txtStatus.Text = "Geçerli komisyon tutarı giriniz.";
            return;
        }

        var invDate = dpCollectionInvDate.SelectedDate ?? DateTime.Today;
        Run(() => _bridge.Printer.PrintCollectionDocumentHeader(txtCollectionSerial.Text, invDate, amount, txtSubscriberNo.Text, txtInstitutionName.Text, comission), FormMessage.DOCUMENT_ID);
    }

    private void BtnPrintCurrAccHeader_OnClick(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtCrrAccAmount.Text, out var amount))
        {
            txtStatus.Text = "Geçerli cari hesap tutarı giriniz.";
            return;
        }

        var date = dpCrrAccDate.SelectedDate ?? DateTime.Today;
        Run(() => _bridge.Printer.PrintCurrentAccountCollectionDocumentHeader(txtCrrAccTcknVkn.Text, txtCrrAccCustName.Text, txtCrrAccDocSerial.Text, date, amount), FormMessage.DOCUMENT_ID);
    }

    private void ButtonPrintEDocumentSample_OnClick(object sender, RoutedEventArgs e)
    {
        var docType = GetSelectedEDocumentDocType();
        var lines = GetEDocumentSampleLines();
        Run(() => _bridge.Printer.PrintEDocumentCopy(docType, lines), FormMessage.OPERATION_SUCCESSFULL);
    }

    private void ButtonLoadInvoiceFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        labelInvoiceLinePath.Text = dialog.FileName;

        try
        {
            var lines = File.ReadAllLines(dialog.FileName);
            var docType = GetSelectedEDocumentDocType();
            var response = new CPResponse(_bridge.Printer.PrintEDocumentCopy(docType, lines));
            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void ButtonSendTestData_OnClick(object sender, RoutedEventArgs e)
    {
        const string rawData = "DFF0010103DFF003021060DFF004024850DFF01814495350414E414B204B472020202020202020202020DFF01F0101DF712E";

        try
        {
            var testData = Convert.FromHexString(rawData);
            _ = new CPResponse(_bridge.Printer.PrintDocumentHeader(1, "12345678901", "AA123456", DateTime.Now));
            var response = new CPResponse(_bridge.Printer.SendTestData(testData));
            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void ButtonAddCustomerRetDoc_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new CustomerView
            {
                Owner = Window.GetWindow(this),
            };

            if (dialog.ShowDialog() == true)
            {
                _returnCustomer = CustomerView.CurrentCustomer;
                buttonAddCustomerRetDoc.Content = FormMessage.UPDATE_CUSTOMER;
            }
            else
            {
                _returnCustomer = null;
                buttonAddCustomerRetDoc.Content = FormMessage.ADD_CUSTOMER;
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
            _returnCustomer = null;
            buttonAddCustomerRetDoc.Content = FormMessage.ADD_CUSTOMER;
        }
    }

    private void ButtonStartReturnDoc_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var date = dpRetDocDate.SelectedDate ?? DateTime.Today;
            var response = new CPResponse(_bridge.Printer.PrintReturnDocumentHeader(date, txtRetDocSerial.Text, txtRetDocOrderNo.Text, _returnCustomer));

            if (response.ErrorCode == 0)
            {
                txtStatus.Text = "BELGE NO: " + response.GetNextParam() + " | Z NO: " + response.GetNextParam();
            }
            else
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
        finally
        {
            _returnCustomer = null;
            buttonAddCustomerRetDoc.Content = FormMessage.ADD_CUSTOMER;
        }
    }

    private void BtnCloseDocument_OnClick(object sender, RoutedEventArgs e) => Run(() => _bridge.Printer.CloseReceipt(false), FormMessage.DOCUMENT_ID);
    private void BtnVoidDocument_OnClick(object sender, RoutedEventArgs e) => Run(() => _bridge.Printer.VoidReceipt(), FormMessage.VOIDED_DOC_ID);

    private void BtnSubtotal_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var response = new CPResponse(_bridge.Printer.PrintSubtotal(false));
            if (response.ErrorCode == 0)
            {
                txtStatus.Text = FormMessage.SUBTOTAL + ": " + response.GetNextParam() + " | " + FormMessage.PAID_TOTAL + ": " + response.GetNextParam();
            }
            else
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnRefund_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtAcquierIdRefund.Text, out var acq))
        {
            txtStatus.Text = "Geçerli acquirer id giriniz.";
            return;
        }

        try
        {
            CPResponse response;
            if (decimal.TryParse(txtRefundAmount.Text, out var amount) && amount > 0)
            {
                response = new CPResponse(_bridge.Printer.RefundEFTPayment(acq, amount));
            }
            else
            {
                response = new CPResponse(_bridge.Printer.RefundEFTPayment(acq));
            }

            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnBankList_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var response = new CPResponse(_bridge.Printer.GetBankListOnEFT());
            txtStatus.Text = response.ErrorCode == 0 ? "BANK LIST ALINDI" : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void ButtonGetEFTSlipCopy_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtSlipCopyAcquierId.Text, out var acq))
        {
            txtStatus.Text = "Geçerli acquirer id giriniz.";
            return;
        }

        var batch = 0;
        var stan = 0;
        var zNo = 0;
        var rNo = 0;

        if (checkBoxSlpCpyZnoRcptNo.IsChecked == true)
        {
            if (!int.TryParse(txtSlipCopyZNo.Text, out zNo) || !int.TryParse(txtSlipCopyReceiptNo.Text, out rNo))
            {
                txtStatus.Text = "Geçerli Z no / fiş no giriniz.";
                return;
            }
        }
        else if (!int.TryParse(txtSlipCopyBatchNo.Text, out batch) || !int.TryParse(txtSlipCopyStanNo.Text, out stan))
        {
            txtStatus.Text = "Geçerli batch / stan no giriniz.";
            return;
        }

        try
        {
            var response = new CPResponse(_bridge.Printer.GetEFTSlipCopy(acq, batch, stan, zNo, rNo));
            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSendSlip_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var slipType = 1;
            if (rbtnSlipTypeMerchant.IsChecked == true)
            {
                slipType = 2;
            }
            else if (rbtnSlipTypeError.IsChecked == true)
            {
                slipType = 3;
            }

            var lines = txtSlipLines.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            var response = new CPResponse(_bridge.Printer.PrintSlip(slipType, lines));
            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnRcptBarcode_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var typeIndex = comboBoxBarcodeTypes.SelectedIndex;
            var barcode = txtRcptBarcode.Text;
            var response = typeIndex > 0
                ? new CPResponse(_bridge.Printer.PrintReceiptBarcode(typeIndex, barcode))
                : new CPResponse(_bridge.Printer.PrintReceiptBarcode(barcode));

            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void ButtonStoppage_OnClick(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtStoppageAmount.Text, out var amount))
        {
            txtStatus.Text = "Geçerli stoppage tutarı giriniz.";
            return;
        }

        try
        {
            var response = new CPResponse(_bridge.Printer.PrintSubtotal(amount));
            txtStatus.Text = response.ErrorCode == 0
                ? FormMessage.SUBTOTAL + ": " + response.GetNextParam() + " | " + FormMessage.PAID_TOTAL + ": " + response.GetNextParam()
                : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }
    private void BtnOpenDrawer_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var response = new CPResponse(_bridge.Printer.OpenDrawer());
            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnReceiptInfo_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var response = new CPResponse(_bridge.Printer.GetSalesInfo());
            if (response.ErrorCode != 0)
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
                return;
            }

            var status = new StringBuilder();
            status.AppendLine($"DOC NO    : {response.GetNextParam()}");
            status.AppendLine($"Z NO      : {response.GetNextParam()}");
            status.AppendLine($"DOC TYPE  : {response.GetNextParam()}");
            status.AppendLine($"DATE      : {response.GetNextParam()}");
            status.AppendLine($"TIME      : {response.GetNextParam()}");
            status.AppendLine($"SUB TOTAL : {response.GetNextParam()}");

            try
            {
                var payCount = Convert.ToInt32(response.GetNextParam());
                status.AppendLine($"PAYMENT COUNT : {payCount}");
                for (var i = 0; i < payCount; i++)
                {
                    var payType = response.GetNextParam();
                    var payTypeName = int.TryParse(payType, out var payTypeValue)
                        ? Enum.GetName(typeof(InfoReceiptPaymentType), payTypeValue) ?? payType
                        : payType;

                    status.AppendLine($"PAY TYPE   : {payTypeName}");
                    status.AppendLine($"PAY INDEX  : {response.GetNextParam()}");
                    status.AppendLine($"PAY AMOUNT : {response.GetNextParam()}");
                    status.AppendLine($"PAY DETAIL : {response.GetNextParam()}");
                }

                var vatCount = Convert.ToInt32(response.GetNextParam());
                decimal vatTotal = 0;
                decimal total = 0;
                for (var i = 0; i < vatCount; i++)
                {
                    status.AppendLine($"KDV RATE         : {response.GetNextParam()}");
                    var totalAmountText = response.GetNextParam();
                    _ = decimal.TryParse(totalAmountText, out var totalAmount);
                    total += totalAmount;
                    status.AppendLine($"KDV TOTAL AMOUNT : {totalAmountText}");

                    var vatAmountText = response.GetNextParam();
                    _ = decimal.TryParse(vatAmountText, out var vatAmount);
                    vatTotal += vatAmount;
                    status.AppendLine($"KDV AMOUNT       : {vatAmountText}");
                }

                status.AppendLine($"TOTAL  : {total}");
                status.AppendLine($"KDV : {vatTotal}");
            }
            catch
            {
            }

            txtStatus.Text = status.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnPrintJsonDocument_OnClick(object sender, RoutedEventArgs e) => ExecuteJsonCommand(json => _bridge.Printer.PrintJSONDocument(json));
    private void BtnPrintJsonDept_OnClick(object sender, RoutedEventArgs e) => ExecuteJsonCommand(json => _bridge.Printer.PrintJSONDocumentDeptOnly(json));
    private void BtnPrintSalesDocument_OnClick(object sender, RoutedEventArgs e) => ExecuteJsonCommand(json => _bridge.Printer.PrintSalesDocument(json));

    private void ToggleAutoPrint(bool isDeptSale)
    {
        if (_autoPrintTimer.IsEnabled)
        {
            StopAutoPrint();
            return;
        }

        var jsonDocument = LoadJsonDocument();
        if (string.IsNullOrWhiteSpace(jsonDocument))
        {
            return;
        }

        _cachedJsonDocument = jsonDocument;
        _autoPrintDeptMode = isDeptSale;
        _autoPrintDocCounter = 1;

        var intervalMs = GetLegacyAppSettingInt("AutoPrintTimerInterval", 10000);
        _autoPrintTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);
        _autoPrintCountdownMs = Math.Max(0, intervalMs - 1000);
        _autoPrintCountdownTimer.Interval = TimeSpan.FromSeconds(1);

        btnAutoPrintJsonDept.Content = isDeptSale ? "STOP PRINT" : "AUTO PRINT    JSON DEPT";
        btnAutoPrintJsonPlu.Content = isDeptSale ? "AUTO PRINT    JSON PLU" : "STOP PRINT";
        btnAutoPrintJsonDept.IsEnabled = isDeptSale;
        btnAutoPrintJsonPlu.IsEnabled = !isDeptSale;

        _autoPrintTimer.Start();
        if (_autoPrintCountdownMs > 0)
        {
            _autoPrintCountdownTimer.Start();
        }
    }

    private void StopAutoPrint()
    {
        _autoPrintTimer.Stop();
        _autoPrintCountdownTimer.Stop();
        btnAutoPrintJsonDept.Content = "AUTO PRINT    JSON DEPT";
        btnAutoPrintJsonPlu.Content = "AUTO PRINT    JSON PLU";
        btnAutoPrintJsonDept.IsEnabled = true;
        btnAutoPrintJsonPlu.IsEnabled = true;
    }

    private void AutoPrintTimer_OnTick(object? sender, EventArgs e)
    {
        try
        {
            txtStatus.Text = $"AUTO PRINT DOC COUNTER: {_autoPrintDocCounter}";

            var response = new CPResponse(_bridge.Printer.PrintDocumentHeader());
            if (response.ErrorCode == 0)
            {
                response = _autoPrintDeptMode
                    ? new CPResponse(_bridge.Printer.PrintJSONDocumentDeptOnly(_cachedJsonDocument))
                    : new CPResponse(_bridge.Printer.PrintJSONDocument(_cachedJsonDocument));
            }

            if (response.ErrorCode != 0)
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
                StopAutoPrint();
                return;
            }

            if (_autoPrintDocCounter % GetLegacyAppSettingInt("GetZReportAt", 500) == 0)
            {
                var zResponse = new CPResponse(_bridge.Printer.PrintZReport());
                if (zResponse.ErrorCode != 0)
                {
                    txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + zResponse.ErrorMessage;
                    StopAutoPrint();
                    return;
                }
            }

            _autoPrintDocCounter++;
        }
        catch (Exception ex)
        {
            SetError(ex);
            StopAutoPrint();
        }
    }

    private void AutoPrintCountdownTimer_OnTick(object? sender, EventArgs e)
    {
        txtStatus.Text = "Auto Print will start in " + (_autoPrintCountdownMs / 1000);
        _autoPrintCountdownMs -= (int)_autoPrintCountdownTimer.Interval.TotalMilliseconds;

        if (_autoPrintCountdownMs <= 0)
        {
            _autoPrintCountdownTimer.Stop();
        }
    }

    private void BtnAddItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtPluId.Text, out var pluNo) || !decimal.TryParse(txtQuantity.Text, out var quantity))
        {
            txtStatus.Text = "Geçerli satış parametreleri giriniz.";
            return;
        }

        var price = decimal.MinusOne;
        if (checkBoxForPrice.IsChecked == true && !decimal.TryParse(txtPrice.Text, out price))
        {
            txtStatus.Text = "Geçerli fiyat giriniz.";
            return;
        }

        Run(() => _bridge.Printer.PrintItem(pluNo, quantity, price, null, null, -1, -1), FormMessage.SUBTOTAL);
    }

    private void BtnVoidSale_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtVoidPluId.Text, out var pluNo) || !decimal.TryParse(txtVoidQuantity.Text, out var quantity))
        {
            txtStatus.Text = "Geçerli void satış parametreleri giriniz.";
            return;
        }

        Run(() => _bridge.Printer.Void(pluNo, quantity), FormMessage.SUBTOTAL);
    }

    private void BtnSaleDept_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtDeptSaleId.Text, out var deptId)
            || !decimal.TryParse(txtDeptSaleQuantity.Text, out var quantity)
            || !decimal.TryParse(txtDeptSalePrice.Text, out var price))
        {
            txtStatus.Text = "Geçerli departman satış parametreleri giriniz.";
            return;
        }

        var weighable = cbxDeptSaleWeighable.IsChecked == true ? 1 : 0;
        Run(() => _bridge.Printer.PrintDepartment(deptId, quantity, price, txtDeptSaleName.Text, weighable), FormMessage.SUBTOTAL);
    }

    private void CbxPaymentType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        cbxSubPayments.ItemsSource = null;
        cbxSubPayments.IsEnabled = false;

        if (cbxPaymentType.SelectedItem is not string paymentType)
        {
            return;
        }

        if (paymentType == FormMessage.CURRENCY)
        {
            cbxSubPayments.ItemsSource = MainWindow.Currencies.Where(x => x != null).Select(x => x!.Name).ToList();
            cbxSubPayments.IsEnabled = true;
            cbxSubPayments.SelectedIndex = 0;
            return;
        }

        if (paymentType == FormMessage.CREDIT)
        {
            cbxSubPayments.ItemsSource = MainWindow.Credits.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            cbxSubPayments.IsEnabled = true;
            cbxSubPayments.SelectedIndex = 0;
            return;
        }

        if (paymentType == FormMessage.FOODCARD)
        {
            cbxSubPayments.ItemsSource = MainWindow.FoodCards.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            cbxSubPayments.IsEnabled = true;
            cbxSubPayments.SelectedIndex = 0;
        }
    }

    private void BtnPayment_OnClick(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtPaymentAmount.Text, out var amount))
        {
            txtStatus.Text = "Geçerli ödeme tutarı giriniz.";
            return;
        }

        var paymentType = cbxPaymentType.SelectedIndex;
        if (paymentType > 3)
        {
            paymentType++;
        }

        var index = -1;
        if (cbxPaymentType.SelectedItem is string selectedPayment && (selectedPayment == FormMessage.CURRENCY || selectedPayment == FormMessage.CREDIT || selectedPayment == FormMessage.FOODCARD))
        {
            index = cbxSubPayments.SelectedIndex;
        }

        try
        {
            var response = new CPResponse(_bridge.Printer.PrintPayment(paymentType, index, amount));
            if (response.ErrorCode == 0)
            {
                txtStatus.Text = FormMessage.SUBTOTAL + ": " + response.GetNextParam() + " | " + FormMessage.PAID_TOTAL + ": " + response.GetNextParam();
            }
            else
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnCardQuery_OnClick(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtEftAmount.Text, out var amount))
        {
            txtStatus.Text = "Geçerli EFT tutarı giriniz.";
            return;
        }

        try
        {
            var response = new CPResponse(_bridge.Printer.GetEFTCardInfo(amount));
            if (response.ErrorCode == 0)
            {
                _ = response.GetNextParam();
                _ = response.GetNextParam();
                _ = response.GetNextParam();
                var cardNumber = response.GetNextParam();
                if (!string.IsNullOrWhiteSpace(cardNumber))
                {
                    txtCardNumber.Text = cardNumber.Length >= 6 ? cardNumber[..6] : cardNumber;
                }
                txtStatus.Text = FormMessage.OPERATION_SUCCESSFULL;
            }
            else
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnEftAuthorization_OnClick(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtEftAmount.Text, out var amount) || !int.TryParse(txtInstallment.Text, out var installment))
        {
            txtStatus.Text = "Geçerli EFT parametreleri giriniz.";
            return;
        }

        try
        {
            var response = new CPResponse(_bridge.Printer.GetEFTAuthorisation(amount, installment, txtCardNumber.Text));
            if (response.ErrorCode == 0)
            {
                var totalAmount = response.GetNextParam();
                var provisionCode = response.GetNextParam();
                var paidAmount = response.GetNextParam();
                txtStatus.Text = "EFT başarılı | Tutar: " + paidAmount + " | Provizyon: " + provisionCode + " | Belge: " + totalAmount;
            }
            else
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnAdjust_OnClick(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtAdjAmount.Text, out var rawAmount))
        {
            txtStatus.Text = "Geçerli bir tutar giriniz.";
            return;
        }

        var isFee = rbtnFee.IsChecked == true;
        var isPercentage = cbxPerc.IsChecked == true;

        var amount = 0m;
        var percentage = 0;
        var type = AdjustmentType.Fee;

        if (isPercentage)
        {
            percentage = (int)rawAmount;
            type = isFee ? AdjustmentType.PercentFee : AdjustmentType.PercentDiscount;
        }
        else
        {
            amount = rawAmount;
            type = isFee ? AdjustmentType.Fee : AdjustmentType.Discount;
        }

        Run(() => _bridge.Printer.PrintAdjustment((int)type, amount, percentage), FormMessage.SUBTOTAL);
    }

    private void BtnVoidPayment_OnClick(object sender, RoutedEventArgs e)
    {
        if (checkBoxVoidEft.IsChecked == true)
        {
            if (!int.TryParse(txtAcquierId.Text, out var acq) || !int.TryParse(txtBatchNo.Text, out var batch) || !int.TryParse(txtStanNo.Text, out var stan))
            {
                txtStatus.Text = "Geçerli EFT iptal parametreleri giriniz.";
                return;
            }

            try
            {
                var eftResponse = new CPResponse(_bridge.Printer.VoidEFTPayment(acq, batch, stan));
                txtStatus.Text = eftResponse.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + eftResponse.ErrorMessage;
            }
            catch (Exception ex)
            {
                SetError(ex);
            }

            return;
        }

        if (!int.TryParse(txtVoidPayIndex.Text, out var paymentIndex) || paymentIndex <= 0)
        {
            txtStatus.Text = "Geçerli bir ödeme indeksi giriniz.";
            return;
        }

        try
        {
            var response = new CPResponse(_bridge.Printer.VoidPayment(paymentIndex - 1));
            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnRemark_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var lines = txtRemarkLine.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.PadRight(ProgramConfig.LOGO_LINE_LENGTH - 3, ' '))
                .ToArray();

            if (lines.Length == 0)
            {
                txtStatus.Text = "Not alanı boş.";
                return;
            }

            var response = new CPResponse(_bridge.Printer.PrintRemarkLine(lines));
            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnCorrect_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var response = new CPResponse(_bridge.Printer.Correct());
            if (response.ErrorCode == 0)
            {
                txtStatus.Text = FormMessage.SUBTOTAL + ": " + response.GetNextParam();
            }
            else
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void ExecuteJsonCommand(Func<string, string> command)
    {
        var json = LoadJsonDocument();
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var response = new CPResponse(command(json));
            txtStatus.Text = response.ErrorCode == 0 ? FormMessage.OPERATION_SUCCESSFULL : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private string? LoadJsonDocument()
    {
        var jsonPath = ResolveJsonDocumentPath();
        if (!File.Exists(jsonPath))
        {
            txtStatus.Text = FormMessage.JSON_FILE_NOT_EXISTS;
            return null;
        }

        try
        {
            return File.ReadAllText(jsonPath);
        }
        catch (Exception ex)
        {
            SetError(ex);
            return null;
        }
    }

    private string ResolveJsonDocumentPath()
    {
        var configuredPath = GetLegacyAppSetting("JSONDocName");
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(AppContext.BaseDirectory, "JSONSaleDoc.txt");
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return FindRelativePathInParents(configuredPath)
            ?? Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    private static int GetLegacyAppSettingInt(string key, int defaultValue)
    {
        var value = GetLegacyAppSetting(key);
        return int.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
    }

    private static string? GetLegacyAppSetting(string key)
    {
        var configPath = FindRelativePathInParents(Path.Combine("FP300Service", "Properties", "App.config"));
        if (configPath is null)
        {
            return null;
        }

        try
        {
            var document = XDocument.Load(configPath);
            return document.Root?
                .Element("appSettings")?
                .Elements("add")
                .FirstOrDefault(element => string.Equals((string?)element.Attribute("key"), key, StringComparison.OrdinalIgnoreCase))?
                .Attribute("value")?
                .Value;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindRelativePathInParents(string relativePath)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            var candidate = Path.Combine(currentDirectory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }

    private int GetSelectedEDocumentDocType()
    {
        return comboBoxEDocumentDocTypes.SelectedIndex switch
        {
            0 => 1,
            1 => 3,
            2 => 2,
            3 => 9,
            4 => 10,
            5 => 1,
            _ => 3
        };
    }

    private string[] GetEDocumentSampleLines()
    {
        return
        [
            "[BELGE]",
            "SAMPLE E-DOCUMENT",
            "SATIR 1",
            "SATIR 2",
            "SATIR 3"
        ];
    }

    private void Run(Func<string> command, string successLabel)
    {
        try
        {
            var response = new CPResponse(command());
            if (response.ErrorCode == 0)
            {
                txtStatus.Text = successLabel + ": " + response.GetNextParam();
            }
            else
            {
                txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void SetError(Exception ex)
    {
        txtStatus.Text = FormMessage.OPERATION_FAILS + ": " + ex.Message;
    }

    private void IntegerInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void DecimalInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            e.Handled = true;
            return;
        }

        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var candidate = textBox.Text.Insert(textBox.CaretIndex, e.Text);
        var pattern = $"^[0-9]*({Regex.Escape(separator)}[0-9]*)?$";
        e.Handled = !Regex.IsMatch(candidate, pattern);
    }
}
