using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace FP300Service.Views;

internal enum FiscalCmd
{
    LastZInfo,
    LastReceiptInfo,
    DrawerInfo
}

internal enum CollectType
{
    CashIn = 1,
    CashOut
}

public partial class FiscalInfoView : UserControl
{
    private readonly IBridge _bridge;
    private FiscalCmd _lastCmd;

    public FiscalInfoView(IBridge bridge)
    {
        InitializeComponent();
        _bridge = bridge;
        SetLanguageOption();
    }

    private void SetLanguageOption()
    {
        gbxStatusFuncs.Header = FormMessage.FISCAL_RECEIPT_INFO;
        btnDrawerInfo.Content = FormMessage.DRAWER_INFO;
        btnLastReceiptInfo.Content = FormMessage.LAST_RECEIPT_INFO;
        btnLastZInfo.Content = FormMessage.LAST_Z_REPORT_INFO;

        gbcEjLimit.Header = FormMessage.EJ_LIMIT;
        lblEjLimit.Text = FormMessage.LINE_COUNT + ":";
        btnSetEjLimit.Content = FormMessage.SET_EJ_LIMIT;

        groupBoxVersionInfo.Header = FormMessage.GROUP_VERSION_INFO;
        btnGetEcrVersion.Content = FormMessage.ECR_VERSION_INFO;
        btnLibraryVersion.Content = FormMessage.LIBRARY_VERSION_INFO;

        groupBoxOther.Header = FormMessage.GROUP_OTHER;
        btnDailySummary.Content = FormMessage.DAILY_SUMMARY;
    }

    private void BtnLastZInfo_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (File.Exists("periodicStatus.test"))
        {
            TestPeriodic();
        }

        _lastCmd = FiscalCmd.LastZInfo;

        try
        {
            SendCommand(new CPResponse(_bridge.Printer.GetLastDocumentInfo(true)));
        }
        catch
        {
            _bridge.Log(FormMessage.OPERATION_FAILS);
        }
    }

    private void BtnLastReceiptInfo_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        _lastCmd = FiscalCmd.LastReceiptInfo;

        try
        {
            SendCommand(new CPResponse(_bridge.Printer.GetLastDocumentInfo(false)));
        }
        catch
        {
            _bridge.Log(FormMessage.OPERATION_FAILS);
        }
    }

    private void BtnDrawerInfo_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        _lastCmd = FiscalCmd.DrawerInfo;

        try
        {
            SendCommand(new CPResponse(_bridge.Printer.GetDrawerInfo()));
        }
        catch
        {
            _bridge.Log(FormMessage.OPERATION_FAILS);
        }
    }

    private void BtnSetEjLimit_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        SendCommand(new CPResponse(_bridge.Printer.SetEJLimit((int)nmrEjLine.Value)));
    }

    private void BtnGetEcrVersion_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var version = _bridge.Printer.GetECRVersion();
            _bridge.Log("***************************************************" + Environment.NewLine);
            _bridge.Log(string.Format(FormMessage.ECR_VERSION_INFO + ": {0}", version));
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnLibraryVersion_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var version = _bridge.Printer.LibraryVersion;
            _bridge.Log("***************************************************" + Environment.NewLine);
            _bridge.Log(string.Format(FormMessage.ECR_VERSION_INFO + ": {0}", version));
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnDailySummary_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var dailySummary = _bridge.Printer.GetDailySummary();
            _bridge.Log("***************************************************" + Environment.NewLine);
            _bridge.Log(FormMessage.DAILY_SUMMARY + ":" + Environment.NewLine);
            _bridge.Log(dailySummary + Environment.NewLine);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void SendCommand(CPResponse response)
    {
        try
        {
            if (response.ErrorCode == 0 && response.ParamList != null)
            {
                string? paramVal;

                if (_lastCmd != FiscalCmd.DrawerInfo)
                {
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        _bridge.Log(string.Format(FormMessage.DOCUMENT_ID.PadLeft(12, ' ') + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        _bridge.Log(string.Format(FormMessage.Z_ID.PadLeft(12, ' ') + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        _bridge.Log(string.Format(FormMessage.EJ_ID.PadLeft(12, ' ') + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        _bridge.Log(string.Format(FormMessage.DOCUMENT_TYPE.PadLeft(12, ' ') + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        _bridge.Log(string.Format(FormMessage.DATE.PadLeft(12, ' ') + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        _bridge.Log(string.Format(FormMessage.TIME.PadLeft(12, ' ') + ": {0}", paramVal));
                    }
                }

                paramVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(paramVal))
                {
                    _bridge.Log("--- " + FormMessage.TOTAL_INFO + " ---");
                    _bridge.Log(string.Format(FormMessage.TOTAL_RECEIPT_COUNT + ": {0}", paramVal));
                }
                paramVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(paramVal))
                {
                    _bridge.Log(string.Format(FormMessage.TOTAL_AMOUNT + ": {0}", paramVal));
                }

                paramVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(paramVal))
                {
                    _bridge.Log("--- " + FormMessage.SALE_INFO + " ---");
                    _bridge.Log(string.Format(FormMessage.TOTAL_SALE_RECEIPT_COUNT + ": {0}", paramVal));
                }
                paramVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(paramVal))
                {
                    _bridge.Log(string.Format(FormMessage.TOTAL_SALE_AMOUNT + ": {0}", paramVal));
                }

                paramVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(paramVal))
                {
                    _bridge.Log("--- " + FormMessage.VOID_INFO + " ---");
                    _bridge.Log(string.Format(FormMessage.TOTAL_VOID_RECEIPT_COUNT + ": {0}", paramVal));
                }
                paramVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(paramVal))
                {
                    _bridge.Log(string.Format(FormMessage.TOTAL_VOID_AMOUNT + ": {0}", paramVal));
                }

                paramVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(paramVal))
                {
                    _bridge.Log("--- " + FormMessage.ADJUSTMENT_INFO + " ---");
                    _bridge.Log(string.Format(FormMessage.TOTAL_ADJUSTED_AMOUNT + ": {0}", paramVal));
                }

                _bridge.Log("--- " + FormMessage.CASH_IN + " ---");
                LogCollectInfo(response);

                _bridge.Log("--- " + FormMessage.CASH_OUT + " ---");
                LogCollectInfo(response);

                _bridge.Log("--- " + FormMessage.PAYMENT_INFO + " ---");
                var i = 0;
                while (response.CurrentParamIndex < response.ParamList.Count)
                {
                    i++;
                    _bridge.Log("** " + FormMessage.PAYMENT + " " + i + " **");
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        var paymentType = int.Parse(paramVal);
                        _bridge.Log(string.Format(FormMessage.PAYMENT_TYPE.PadLeft(15, ' ') + ": {0}", Common.Payments[paymentType]));
                    }
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        _bridge.Log(string.Format(FormMessage.PAYMENT_INDEX.PadLeft(15, ' ') + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(paramVal))
                    {
                        _bridge.Log(string.Format(FormMessage.PAYMENT_AMOUNT.PadLeft(15, ' ') + ": {0}", paramVal));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void LogCollectInfo(CPResponse response)
    {
        var paramVal = response.GetNextParam();
        if (!string.IsNullOrEmpty(paramVal))
        {
            var collectType = int.Parse(paramVal);
            _bridge.Log(string.Format(FormMessage.COLLECT_TYPE.PadLeft(15, ' ') + ": {0}", Common.CollectTypes[collectType - 1]));
        }

        paramVal = response.GetNextParam();
        if (!string.IsNullOrEmpty(paramVal))
        {
            _bridge.Log(string.Format(FormMessage.COLLECT_QUANTITY.PadLeft(15, ' ') + ": {0}", paramVal));
        }

        paramVal = response.GetNextParam();
        if (!string.IsNullOrEmpty(paramVal))
        {
            _bridge.Log(string.Format(FormMessage.COLLECT_AMOUNT.PadLeft(15, ' ') + ": {0}", paramVal));
        }
    }

    private void TestPeriodic()
    {
        var dateStart = DateTime.Now;
        var dateLastReceipt = DateTime.Now;
        var counter = 0;

        while (true)
        {
            try
            {
                var dateNow = DateTime.Now;
                if ((dateNow - dateStart).Seconds > 5 * 60)
                {
                    break;
                }

                if ((dateNow - dateLastReceipt).Seconds > 50)
                {
                    _ = new CPResponse(_bridge.Printer.PrintDocumentHeader());
                    _ = new CPResponse(_bridge.Printer.PrintDepartment(1, decimal.Parse("2,831", new CultureInfo("tr-TR")), decimal.Parse("1,28", new CultureInfo("tr-TR")), "DEPARTSMPLE", 1));
                    _ = new CPResponse(_bridge.Printer.PrintPayment(1, 0, 5));
                    _ = new CPResponse(_bridge.Printer.CloseReceipt(false));
                    dateLastReceipt = dateNow;
                }
                else
                {
                    _ = new CPResponse(_bridge.Printer.CheckPrinterStatus());
                    counter++;
                    if (counter % 20 == 19)
                    {
                        _ = new CPResponse(_bridge.Printer.GetLastDocumentInfo(true));
                    }
                }

                if (!File.Exists("periodicStatus.test"))
                {
                    break;
                }

                Thread.Sleep(1000);
            }
            catch
            {
            }
        }
    }

    private void SetError(Exception ex)
    {
        var message = FormMessage.OPERATION_FAILS + ": " + ex.Message;
        _bridge.Log(message);
    }
}
