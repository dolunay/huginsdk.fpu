using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FP300Service.Views;

public partial class ReportsView : UserControl
{
    private readonly IBridge _bridge;

    public ReportsView(IBridge bridge)
    {
        InitializeComponent();
        _bridge = bridge;

        cbxOtherDocType.ItemsSource = Common.OtherDocTypes;
        cbxOtherDocType.SelectedIndex = 0;

        dtZZFirstDate.SelectedDate = DateTime.Today;
        dtZZLastDate.SelectedDate = DateTime.Today;
        dtEJSingDocDate.SelectedDate = DateTime.Today;
        dtEJPerFirstDate.SelectedDate = DateTime.Today;
        dtEJPerLastDate.SelectedDate = DateTime.Today;
        dtDailyDate.SelectedDate = DateTime.Today;
        dtODPerFirstDate.SelectedDate = DateTime.Today;
        dtODPerLastDate.SelectedDate = DateTime.Today;

        SetLanguageOption();
    }

    private void SetLanguageOption()
    {
        cbxSoft.Content = FormMessage.CONTEXT;
        cbxHard.Content = FormMessage.PRINT;

        tabXReports.Header = FormMessage.X_REPORTS;
        rbtnXReport.Content = FormMessage.X_REPORT;
        lblCountReturnDocX.Content = FormMessage.RETURN_COUNT;
        lblAmountReturnDocX.Content = FormMessage.RETURN_AMOUNT;
        cbxAffectDrawerX.Content = FormMessage.RETURN_AFFECT_DRAWER;
        rbtPluRprt.Content = FormMessage.PLU_REPORT;
        lblFirstPlu.Content = FormMessage.FIRST_PLU;
        lblPLULast.Content = FormMessage.LAST_PLU;
        rbtnSystemInfoRprt.Content = FormMessage.SYSTEM_INFO_REPORT;
        rbtnReceiptTotalReport.Content = FormMessage.RECEIPT_TOTAL_REPORT;
        btnPrintNonFiscReport.Content = FormMessage.GET_REPORT;
        btnReportContentX.Content = FormMessage.REPORT_CONTENT;

        tabZReports.Header = FormMessage.Z_REPORTS;
        rbtnZReport.Content = FormMessage.Z_REPORT;
        lblCountReturnDocZ.Content = FormMessage.RETURN_COUNT;
        lblAmountReturnDocZ.Content = FormMessage.RETURN_AMOUNT;
        cbxAffectDrawerZ.Content = FormMessage.RETURN_AFFECT_DRAWER;
        rbtnEndDay.Content = FormMessage.END_DAY_REPORT;
        btnPrintZReport.Content = FormMessage.GET_REPORT;
        btnReportContentZ.Content = FormMessage.REPORT_CONTENT;

        tabFMReports.Header = "FM REPORTS";
        rbtnFiscZZReport.Content = FormMessage.FM_REPORT_ZZ;
        lblFFZFirst.Content = FormMessage.FIRST_Z_ID;
        lblFFZLast.Content = FormMessage.LAST_Z_ID;
        cbxFFZZDetailed.Content = FormMessage.Z_DETAILED;
        rbtnFiscDateReport.Content = FormMessage.FM_REPORT_DATE;
        lblZZFirstDate.Content = FormMessage.FIRST_Z_DATE;
        lblZZLastDate.Content = FormMessage.LAST_Z_DATE;
        cbxFFDateDetailed.Content = FormMessage.DATE_DETAILED;
        btnPrintFMReport.Content = FormMessage.GET_REPORT;
        btnReportContentFM.Content = FormMessage.REPORT_CONTENT;

        tabEJSingle.Header = FormMessage.EJ_SINGLE_REPORT;
        rbtnEJDetail.Content = FormMessage.EJ_DETAIL_REPORT;
        rbtnZCopy.Content = FormMessage.Z_COPY;
        lblZCopyZno.Content = FormMessage.Z_ID;
        rbtnEJDocCopyZandDocId.Content = FormMessage.EJ_SINGLE_REPORT_ZID_DOCID;
        lblEJSingDocZNo.Content = FormMessage.Z_ID;
        lblEJSingDocNo.Content = FormMessage.DOCUMENT_ID;
        rbtnEJDocCopyDate.Content = FormMessage.EJ_SINGLE_REPORT_DATE_TIME;
        lblEJSingDocDate.Content = FormMessage.DATE;
        lblEJSingDocTime.Content = FormMessage.TIME;
        btnPrintEJSingReport.Content = FormMessage.GET_REPORT;

        tabEJPeriyot.Header = FormMessage.EJ_PERIODIC;
        rbtnEJPerByNo.Content = FormMessage.EJ_PERIODIC_REPORT_ZID_DOCID;
        lblEJPerFirstZ.Content = FormMessage.FIRST_Z_ID;
        lblEJPerFirstDoc.Content = FormMessage.FIRST_DOC_ID;
        lblEJPerLastZ.Content = FormMessage.LAST_Z_ID;
        lblEJPerLastDoc.Content = FormMessage.LAST_DOC_ID;
        rbtnEJPerByDate.Content = FormMessage.EJ_PERIODIC_REPORT_DATE_TIME;
        lblEJPerFirstZDt.Content = FormMessage.FIRST_DATE;
        lblEJPerFistTime.Content = FormMessage.FIRST_HOUR;
        lblEJPerLastDt.Content = FormMessage.LAST_DATE;
        lblEJPerLastTime.Content = FormMessage.LAST_HOUR;
        rbtnEJPerByDaily.Content = FormMessage.EJ_PERIODIC_DAILY;
        lblEJPerDailyDate.Content = FormMessage.DATE;
        btnPrintEJPerReport.Content = FormMessage.GET_REPORT;

        tabOtherDoc.Header = FormMessage.OTHER_DOC;
        lblODocType.Content = FormMessage.OTHER_DOC_TYPE;
        rBtnODocDaily.Content = FormMessage.DAILY_OTHER_DOC_REPORT;
        rBtnODocPeriodic.Content = FormMessage.PERIODIC_OTHER_DOC_REPORT;
        lblODPerFirstDt.Content = FormMessage.FIRST_DATE;
        lblODPerLastDt.Content = FormMessage.LAST_DATE;
        btnPrintODReport.Content = FormMessage.GET_REPORT;
    }

    private void BtnXReport_OnClick(object sender, RoutedEventArgs e)
    {
        var copy = GetPrintTarget();

        if (rbtnXReport.IsChecked == true)
        {
            if (numerCountReturnDocX.Value == decimal.Zero)
            {
                RunReport(() => _bridge.Printer.PrintXReport(copy));
            }
            else
            {
                RunReport(() => _bridge.Printer.PrintXReport((int)numerCountReturnDocX.Value, numerAmountReturnDocX.Value, cbxAffectDrawerX.IsChecked == true));
            }

            return;
        }

        if (rbtPluRprt.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintXPluReport((int)nmrPLUFirst.Value, (int)nmrPLULast.Value, copy));
            return;
        }

        if (rbtnSystemInfoRprt.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintSystemInfoReport(copy));
            return;
        }

        if (rbtnReceiptTotalReport.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintReceiptTotalReport(copy));
        }
    }

    private void BtnZReport_OnClick(object sender, RoutedEventArgs e)
    {
        if (rbtnZReport.IsChecked == true)
        {
            if (numerCountReturnDocZ.Value == decimal.Zero)
            {
                RunReport(() => _bridge.Printer.PrintZReport(GetPrintTarget()));
            }
            else
            {
                RunReport(() => _bridge.Printer.PrintZReport((int)numerCountReturnDocZ.Value, numerAmountReturnDocZ.Value, cbxAffectDrawerZ.IsChecked == true));
            }

            return;
        }

        if (rbtnEndDay.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintEndDayReport());
        }
    }

    private void BtnFmReport_OnClick(object sender, RoutedEventArgs e)
    {
        var copy = GetPrintTarget();

        if (rbtnFiscZZReport.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintPeriodicZZReport((int)nmrFFZFirst.Value, (int)nmrFFZLast.Value, copy, cbxFFZZDetailed.IsChecked == true));
            return;
        }

        var firstDate = dtZZFirstDate.SelectedDate ?? DateTime.Today;
        var lastDate = dtZZLastDate.SelectedDate ?? DateTime.Today;
        RunReport(() => _bridge.Printer.PrintPeriodicDateReport(firstDate, lastDate, copy, cbxFFDateDetailed.IsChecked == true));
    }

    private void BtnEjReport_OnClick(object sender, RoutedEventArgs e)
    {
        var copy = GetPrintTarget();

        if (rbtnEJDetail.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintEJDetail(copy));
            return;
        }

        if (rbtnZCopy.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintEJPeriodic((int)nmrEJZCopyZNo.Value, 0, (int)nmrEJZCopyZNo.Value, 0, copy));
            return;
        }

        if (rbtnEJDocCopyZandDocId.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintEJPeriodic((int)nmrEJSingDocZNo.Value, (int)nmrEJSingDocNo.Value, (int)nmrEJSingDocZNo.Value, (int)nmrEJSingDocNo.Value, copy));
            return;
        }

        if (rbtnEJDocCopyDate.IsChecked == true)
        {
            if (!TryCombineDateAndTime(dtEJSingDocDate.SelectedDate, txtEJSingDocTime.Text, out var dateTime))
            {
                txtResult.Text = "Geçerli saat değeri giriniz. Örn: 14:30:00";
                return;
            }

            RunReport(() => _bridge.Printer.PrintEJPeriodic(dateTime, copy));
        }
    }

    private void BtnEjPeriodicReport_OnClick(object sender, RoutedEventArgs e)
    {
        var copy = GetPrintTarget();

        if (rbtnEJPerByNo.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintEJPeriodic((int)nmrEJPerFirstZ.Value, (int)nmrEJPerFirstDoc.Value, (int)nmrEJPerLastZ.Value, (int)nmrEJPerLastDoc.Value, copy));
            return;
        }

        if (rbtnEJPerByDate.IsChecked == true)
        {
            if (!TryCombineDateAndTime(dtEJPerFirstDate.SelectedDate, txtEJPerFirstTime.Text, out var firstDate))
            {
                txtResult.Text = "Geçerli ilk saat değeri giriniz. Örn: 08:30:00";
                return;
            }

            if (!TryCombineDateAndTime(dtEJPerLastDate.SelectedDate, txtEJPerLastTime.Text, out var lastDate))
            {
                txtResult.Text = "Geçerli son saat değeri giriniz. Örn: 18:30:00";
                return;
            }

            RunReport(() => _bridge.Printer.PrintEJPeriodic(firstDate, lastDate, copy));
            return;
        }

        var date = dtDailyDate.SelectedDate ?? DateTime.Today;
        var startDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
        var endDate = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
        RunReport(() => _bridge.Printer.PrintEJPeriodic(startDate, endDate, copy));
    }

    private void BtnOtherDocReport_OnClick(object sender, RoutedEventArgs e)
    {
        var otherDocType = cbxOtherDocType.SelectedIndex;
        if (otherDocType < 0)
        {
            txtResult.Text = "Diğer doküman tipi seçiniz.";
            return;
        }

        if (rBtnODocDaily.IsChecked == true)
        {
            RunReport(() => _bridge.Printer.PrintODocPeriodic(otherDocType));
            return;
        }

        var firstDate = dtODPerFirstDate.SelectedDate ?? DateTime.Today;
        var lastDate = dtODPerLastDate.SelectedDate ?? DateTime.Today;
        RunReport(() => _bridge.Printer.PrintODocPeriodic(firstDate, lastDate, otherDocType));
    }

    private void BtnReportContent_OnClick(object sender, RoutedEventArgs e)
    {
        RunReport(() => _bridge.Printer.GetReportContent());
    }

    private int GetPrintTarget()
    {
        var copy = 0;

        if (cbxSoft.IsChecked == true)
        {
            copy += 1;
        }

        if (cbxHard.IsChecked == true)
        {
            copy += 2;
        }

        if (copy == 0)
        {
            copy = 1;
        }

        return copy;
    }

    private void RunReport(Func<string> reportCommand)
    {
        txtResult.Text = string.Empty;

        try
        {
            var response = new CPResponse(reportCommand());
            var reportContent = response.GetParamByIndex(3);

            if (!string.IsNullOrWhiteSpace(reportContent))
            {
                txtResult.Text = reportContent;
            }
        }
        catch (Exception ex)
        {
            var message = FormMessage.OPERATION_FAILS + ": " + ex.Message;
            _bridge.Log(message);
            txtResult.Text = message;
        }
    }

    private static bool TryCombineDateAndTime(DateTime? date, string timeText, out DateTime result)
    {
        var selectedDate = date ?? DateTime.Today;

        if (TimeSpan.TryParseExact(timeText, new[] { "hh\\:mm", "hh\\:mm\\:ss", "h\\:mm", "h\\:mm\\:ss" }, CultureInfo.InvariantCulture, out var time) ||
            TimeSpan.TryParse(timeText, CultureInfo.CurrentCulture, out time))
        {
            result = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, time.Hours, time.Minutes, time.Seconds);
            return true;
        }

        result = selectedDate;
        return false;
    }
}
