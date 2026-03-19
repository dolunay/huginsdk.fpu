using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FP300Service;

namespace FP300NetCoreService.Views;

public partial class UtililtyFuncsView : UserControl
{
    private readonly IBridge _bridge;

    public UtililtyFuncsView(IBridge bridge)
    {
        InitializeComponent();
        _bridge = bridge;
        SetLanguageOption();
    }

    private void SetLanguageOption()
    {
        lblStatusInfo.Text = FormMessage.STATUS_INFO;
        btnInterruptProcess.Content = FormMessage.INTERRUPT_PROCESS;
        btnLastResponse.Content = FormMessage.LAST_RESPONSE;
        btnQueryStatus.Content = FormMessage.CHECK_STATUS;
        lblFiscalOperations.Text = FormMessage.FISCAL_OPERATIONS;
        btnStartFM.Content = FormMessage.START_FM;
        lblCashierOptions.Text = FormMessage.CASHIER_LOGIN;
        lblPass.Text = FormMessage.PASSWORD;
        lblCashierNo.Text = FormMessage.CASHIER_ID;
        btnSignInCashier.Content = FormMessage.CASHIER_LOGIN;
        lblDrawerOptions.Text = FormMessage.CASH_IN_CASH_OUT;
        btnPayOut.Content = FormMessage.CASH_OUT;
        lblPayAmount.Text = FormMessage.AMOUNT;
        btnPayIn.Content = FormMessage.CASH_IN;
        lblNFReceipt.Text = FormMessage.SPECIAL_RECEIPT;
        btnSampleNF.Content = FormMessage.PRINT_SAMPLE_CONTEXT;
        btnCloseNF.Content = FormMessage.CLOSE_NF_RECEIPT;
        btnStartNFReceipt.Content = FormMessage.START_NF_RECEIPT;
        lblKeypadOptions.Text = FormMessage.KEYPAD_OPTIONS;
        btnLockKeys.Content = FormMessage.LOCK_KEYS;
        btnUnlockKeys.Content = FormMessage.UNLOCK_KEYS;
    }

    private void ParseResponse(CPResponse response)
    {
        try
        {
            if (response.ErrorCode == 0)
            {
                var retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(string.Format(FormMessage.DATE.PadRight(12, ' ') + ":{0}", retVal));
                }

                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(string.Format(FormMessage.TIME.PadRight(12, ' ') + ":{0}", retVal));
                }
                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(string.Format("NOTE".PadRight(12, ' ') + ":{0}", retVal));
                }
                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(string.Format(FormMessage.AMOUNT.PadRight(12, ' ') + ":{0}", retVal));
                }
                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    _bridge.Log(FormMessage.DOCUMENT_ID.PadRight(12, ' ') + ":" + retVal);
                }

                _ = response.GetNextParam();

                retVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(retVal))
                {
                    var authNote = string.Empty;
                    try
                    {
                        authNote = int.Parse(retVal) switch
                        {
                            0 => FormMessage.SALE,
                            1 => "PROGRAM",
                            2 => FormMessage.SALE + " & Z",
                            3 => FormMessage.ALL,
                            _ => string.Empty,
                        };

                        _bridge.Log(FormMessage.AUTHORIZATION.PadRight(12, ' ') + ":" + authNote);
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnStartFM_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var fiscalNumber = PromptFiscalId();
            if (fiscalNumber is null)
            {
                return;
            }

            if (fiscalNumber == 0 || fiscalNumber > 99999999)
            {
                _bridge.Log(FormMessage.INAPPROPRIATE_FISCAL_ID);
                return;
            }

            ParseResponse(new CPResponse(_bridge.Printer.StartFM(fiscalNumber.Value)));
        }
        catch (FormatException)
        {
            _bridge.Log(FormMessage.INAPPROPRIATE_FISCAL_ID);
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private int? PromptFiscalId()
    {
        var inputBox = new InputBox(FormMessage.PLS_ENTER_FISCAL_ID + "(1-99999999) :", 8)
        {
            Owner = Window.GetWindow(this)
        };

        return inputBox.ShowDialog() == true && int.TryParse(inputBox.input, out var value) ? value : null;
    }

    private void BtnQueryStatus_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.CheckPrinterStatus()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnLastResponse_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.GetLastResponse()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnInterruptProcess_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.InterruptReport()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnSignInCashier_OnClick(object sender, RoutedEventArgs e)
    {
        var id = (int)nmrCashierNo.Value;

        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.SignInCashier(id, txtPassword.Password)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnPayIn_OnClick(object sender, RoutedEventArgs e)
    {
        var amount = nmrPayAmount.Value;

        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.CashIn(amount)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnPayOut_OnClick(object sender, RoutedEventArgs e)
    {
        var amount = nmrPayAmount.Value;

        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.CashOut(amount)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnStartNFReceipt_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.StartNFReceipt()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnSampleNF_OnClick(object sender, RoutedEventArgs e)
    {
        var lines = new List<string>();
        for (var i = 0; i < 25; i++)
        {
            lines.Add(string.Format("{1} {0}", i, FormMessage.SAMPLE_LINE));
        }

        try
        {
            _ = new CPResponse(_bridge.Printer.WriteNFLine(lines.ToArray()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnCloseNF_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.CloseNFReceipt()));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnLockKeys_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.ChangeKeyLockStatus(true)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }

    private void BtnUnlockKeys_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ParseResponse(new CPResponse(_bridge.Printer.ChangeKeyLockStatus(false)));
        }
        catch (Exception ex)
        {
            _bridge.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
        }
    }
}
