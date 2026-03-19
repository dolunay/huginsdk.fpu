using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using FP300Service;

namespace FP300NetCoreService.Views;

public partial class ServiceView : UserControl
{
    private readonly IBridge _bridge;

    public ServiceView(IBridge bridge)
    {
        InitializeComponent();
        _bridge = bridge;

        cbxSetDate.Checked += CbxSetDate_OnCheckedChanged;
        cbxSetDate.Unchecked += CbxSetDate_OnCheckedChanged;
        cbxTime.Checked += CbxTime_OnCheckedChanged;
        cbxTime.Unchecked += CbxTime_OnCheckedChanged;
        
        dtFPDate.SelectedDate = DateTime.Today;
        dtpLog.SelectedDate = DateTime.Today;
        txtFPTime.Text = DateTime.Now.ToString("HH:mm");

        cmbGmpCmds.ItemsSource = new[]
        {
            "Debug Modu",
            "First Init",
            "Parametre Yükleme",
            "Log Gönder",
            "Fiş Gönder",
            "İptal Fişi Gönder",
            "Z Gönder",
            "First Init Kontrol Etme",
            "-",
            "Printer Debug Açık",
            "Printer Debug Kapalı",
            "-",
            "-",
            "Logs to tsm"
        };
        cmbGmpCmds.SelectedIndex = 0;

        SetLanguageOption();
        UpdateDateTimeControls();

#if IN_TEST
        btnTestServer.Visibility = System.Windows.Visibility.Visible;
#else
        btnTestServer.Visibility = System.Windows.Visibility.Collapsed;
#endif
    }

    private void SetLanguageOption()
    {
        tabSRVOperation.Header = FormMessage.SERVICE_OPERATIONS;
        btnPrintLogs.Content = FormMessage.PRINT_LOGS;
        btnOrderNum.Content = FormMessage.ORDER_CODE;
        lblFileName.Text = FormMessage.FILE_NAME;
        btnxFer.Content = FormMessage.FILE_TRANSFER;
        cbxTime.Content = FormMessage.TIME;
        cbxSetDate.Content = FormMessage.DATE;
        btnSetDateTime.Content = FormMessage.SET_DATE_TIME;
        btnEnterService.Content = FormMessage.ENTER_SERVICE_MODE;
        btnExitService.Content = FormMessage.EXIT_SERVICE_MODE;
        lblPass.Text = FormMessage.PASSWORD;
        btnSetExternalSettings.Content = FormMessage.SET_EXT_DEVICE_SETTINGS;
        btnStartFMTest.Content = FormMessage.START_FM_TEST;
        btnCreateDB.Content = FormMessage.CREATE_SALE_DB;
        btnCloseFM.Content = FormMessage.CLOSE_FM;
        btnFatorySettings.Content = FormMessage.FACTORY_SETTINGS;
        btnFormatDailyMem.Content = FormMessage.FORMAT_DAILY_MEMORY;
        btnEJInit.Content = FormMessage.INITIALIZE_EJ;
        btnUpdateFirmware.Content = FormMessage.UPDATE_FIRMWARE;
        lblPrgrmPass.Text = FormMessage.PASSWORD;
        btnFiscalMode.Content = FormMessage.FISCAL_MODE_NOW;
        tabSRVTest.Header = FormMessage.TEST_COMMANDS;
    }

    private void ParseServiceResponse(CPResponse response)
    {
        try
        {
            if (response.ErrorCode == 0)
            {
                var retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(string.Format("{1} : {0}", retVal, FormMessage.DATE));
                }

                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(string.Format("{1} : {0}", retVal, FormMessage.TIME));
                }

                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(string.Format("NOTE : {0}", retVal));
                }

                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(string.Format("{1} : {0}", retVal, FormMessage.QUANTITY));
                }

                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(FormMessage.DOCUMENT_ID + " : " + retVal);
                }

                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log("Kod: " + retVal);
                    retVal = response.GetNextParam();
                    if (!string.IsNullOrEmpty(retVal))
                    {
                        lblOrderNum.Text = retVal;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnEnterService_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            ParseServiceResponse(new CPResponse(_bridge.Printer.EnterServiceMode(txtPassword.Password)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnExitService_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            ParseServiceResponse(new CPResponse(_bridge.Printer.ExitServiceMode(txtPassword.Password)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void CbxSetDate_OnCheckedChanged(object sender, System.Windows.RoutedEventArgs e) => UpdateDateTimeControls();
    private void CbxTime_OnCheckedChanged(object sender, System.Windows.RoutedEventArgs e) => UpdateDateTimeControls();

    private void UpdateDateTimeControls()
    {
        dtFPDate?.IsEnabled = cbxSetDate.IsChecked == true;
        txtFPTime?.IsEnabled = cbxTime.IsChecked == true;
    }

    private void BtnFormatDailyMem_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            ParseServiceResponse(new CPResponse(_bridge.Printer.ClearDailyMemory()));
        }
        catch
        {
        }
    }

    private void BtnSetDateTime_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var date = DateTime.MinValue;
        var time = DateTime.MinValue;

        if (cbxSetDate.IsChecked == true && dtFPDate.SelectedDate.HasValue)
        {
            date = dtFPDate.SelectedDate.Value;
        }

        if (cbxTime.IsChecked == true && TimeSpan.TryParse(txtFPTime.Text, out var timeSpan))
        {
            time = DateTime.Today.Add(timeSpan);
        }

        try
        {
            ParseServiceResponse(new CPResponse(_bridge.Printer.SetDateTime(date, time)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnFatorySettings_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            ParseServiceResponse(new CPResponse(_bridge.Printer.FactorySettings()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnCloseFM_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            ParseServiceResponse(new CPResponse(_bridge.Printer.CloseFM()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnSetExternalSettings_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var port = -1;
            if (!string.IsNullOrEmpty(tbxPort.Text))
            {
                port = Convert.ToInt32(tbxPort.Text);
            }

            ParseServiceResponse(new CPResponse(_bridge.Printer.SetExternalDevAddress(txtIP.Text, port)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnUpdateFirmware_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var port = -1;
            if (!string.IsNullOrEmpty(txtServerPort.Text))
            {
                port = Convert.ToInt32(txtServerPort.Text);
            }

            ParseServiceResponse(new CPResponse(_bridge.Printer.UpdateFirmware(txtServerIp.Text, port)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnTestServer_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var server = new TestServer
            {
                Owner = System.Windows.Window.GetWindow(this),
                IpAddress = txtServerIp.Text,
                Port = int.TryParse(txtServerPort.Text, out var port) ? port : 0
            };

            server.Show();
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnOrderNum_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            ParseServiceResponse(new CPResponse(_bridge.Printer.GetServiceCode()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnFiscalMode_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        ParseServiceResponse(new CPResponse(_bridge.Printer.Fiscalize(txtPrgmPass.Password)));
    }

    private void BtnEJInit_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        ParseServiceResponse(new CPResponse(_bridge.Printer.StartEJ()));
    }

    private void BtnPrintLogs_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        ParseServiceResponse(new CPResponse(_bridge.Printer.PrintLogs(dtpLog.SelectedDate ?? DateTime.Today)));
    }

    private void BtnCreateDB_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        ParseServiceResponse(new CPResponse(_bridge.Printer.CreateDB()));
    }

    private void BtnStartFMTest_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        ParseServiceResponse(new CPResponse(_bridge.Printer.StartFMTest()));
    }

    private void BtnXFer_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var response = new CPResponse(_bridge.Printer.TransferFile(txtFileName.Text));
            if (response.ErrorCode == 0)
            {
                ParseServiceResponse(response);
            }
        }
        catch
        {
        }
    }

    private void BtnTestGmp_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var index = cmbGmpCmds.SelectedIndex;
        ParseServiceResponse(new CPResponse(_bridge.Printer.SetEJLimit(index)));
    }

    private void BtnGMPPort_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var segments = txtTSMIp.Text.Split(new[] { '.', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var ipVal = string.Empty;
        try
        {
            foreach (var segment in segments)
            {
                ipVal += int.Parse(segment).ToString().PadLeft(3, '0');
            }
        }
        catch
        {
        }

        var port = Convert.ToInt32(txtGMPPort.Text);
        ParseServiceResponse(new CPResponse(_bridge.Printer.SaveGMPConnectionInfo(ipVal, port)));
    }

    private void IpInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9.]+$");
    }

    private void PortInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void TimeInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9:]+$");
    }
}
