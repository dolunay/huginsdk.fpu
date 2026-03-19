using FP300Service;
using Hugin.POS.CompactPrinter.FP300;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FP300NetCoreService.Views;

public partial class ProgramView : UserControl
{
    private enum ProgramOptionSource
    {
        CUTTER,
        PAY_WITH_EFT,
        RECEIPT_LIMIT,
        GRAPHIC_LOGO,
        RECEIPT_BARCODE,
        EFT_MANAGEMENT_ON_POS
    }

    private readonly IBridge _bridge;
    private readonly ObservableCollection<DepartmentRow> _departments = [];
    private readonly ObservableCollection<VatRow> _vatRates = [];
    private readonly ObservableCollection<PluRow> _plus = [];
    private readonly ObservableCollection<CreditRow> _credits = [];
    private readonly ObservableCollection<CurrencyRow> _currencies = [];
    private readonly ObservableCollection<MainCategoryRow> _mainCategories = [];
    private readonly ObservableCollection<CashierRow> _cashiers = [];
    private readonly ObservableCollection<ProgramOptionRow> _programOptions = [];
    private string _lastLogoPath = string.Empty;
    private System.Drawing.Image? _selectedLogoImage;
    private List<string> _productLineList = [];
    private bool _secondStage;
    private bool _stopSendProductsTimerRequested;
    private int _sendProductsElapsedSeconds;
    private readonly DispatcherTimer _sendProductsTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public ProgramView(IBridge bridge)
    {
        InitializeComponent();

        UpdateNetworkTextBoxStates();
        
        _bridge = bridge;
        InitializeDepartmentRows();
        InitializeVatRows();
        InitializePluRows();
        InitializeCreditRows();
        InitializeCurrencyRows();
        InitializeMainCategoryRows();
        InitializeCashierRows();
        InitializeProgramOptionRows();
        SetLanguageOption();
        SetGridColumnHeaders();

        if (_bridge.Printer is not null)
        {
            _bridge.Printer.OnFileSendingProgress += Printer_OnFileSendingProgress;
        }

        _sendProductsTimer.Tick += SendProductsTimer_OnTick;
    }

    private void SetGridColumnHeaders()
    {
        dgvDepDefine.Columns[0].Header = FormMessage.DEPARTMENT_ID;
        dgvDepDefine.Columns[1].Header = FormMessage.DEPARTMENT_NAME;
        dgvDepDefine.Columns[2].Header = FormMessage.VAT_GROUP_ID;
        dgvDepDefine.Columns[3].Header = FormMessage.PRICE;
        dgvDepDefine.Columns[4].Header = "LIMIT";
        dgvDepDefine.Columns[5].Header = FormMessage.WEIGHABLE;
        dgvDepDefine.Columns[6].Header = FormMessage.COMMIT;

        dgvPLUDefine.Columns[0].Header = FormMessage.PLU_ID;
        dgvPLUDefine.Columns[1].Header = FormMessage.PLU_NAME;
        dgvPLUDefine.Columns[2].Header = FormMessage.DEPARTMENT_ID;
        dgvPLUDefine.Columns[3].Header = FormMessage.PRICE;
        dgvPLUDefine.Columns[4].Header = FormMessage.WEIGHABLE;
        dgvPLUDefine.Columns[5].Header = FormMessage.BARCODE;
        dgvPLUDefine.Columns[6].Header = FormMessage.SUB_CATEGORY;
        dgvPLUDefine.Columns[7].Header = FormMessage.COMMIT;

        dgvFCurrency.Columns[0].Header = FormMessage.F_CURRENCY_ID;
        dgvFCurrency.Columns[1].Header = FormMessage.F_CURRENCY_CODE_NAME;
        dgvFCurrency.Columns[2].Header = FormMessage.EXCHANGE_RATE;
        dgvFCurrency.Columns[3].Header = FormMessage.COMMIT;

        dgvCredits.Columns[0].Header = FormMessage.CREDIT_INDEX;
        dgvCredits.Columns[1].Header = FormMessage.CREDIT_NAME;
        dgvCredits.Columns[2].Header = FormMessage.COMMIT;

        dgvMainCategory.Columns[0].Header = FormMessage.MAIN_CAT_ID;
        dgvMainCategory.Columns[1].Header = FormMessage.MAIN_CAT_NAME;
        dgvMainCategory.Columns[2].Header = FormMessage.COMMIT;

        dgvCashier.Columns[0].Header = FormMessage.CASHIER_ID;
        dgvCashier.Columns[1].Header = FormMessage.CASHIER_NAME;
        dgvCashier.Columns[2].Header = FormMessage.CASHIER_PWD;
        dgvCashier.Columns[3].Header = FormMessage.COMMIT;

        dgvPrmOption.Columns[0].Header = FormMessage.PROG_OPT_ID;
        dgvPrmOption.Columns[1].Header = FormMessage.PROG_OPT_NAME;
        dgvPrmOption.Columns[2].Header = FormMessage.PROG_OPT_VALUE;
        dgvPrmOption.Columns[3].Header = FormMessage.COMMIT;

        dgvVatDefine.Columns[0].Header = FormMessage.VAT_ID;
        dgvVatDefine.Columns[1].Header = FormMessage.VAT_RATE;
        dgvVatDefine.Columns[2].Header = FormMessage.COMMIT;
    }

    private void InitializeDepartmentRows()
    {
        _departments.Clear();
        for (var i = 1; i <= ProgramConfig.MAX_DEPARTMENT_COUNT; i++)
        {
            _departments.Add(new DepartmentRow
            {
                DepartmentId = i,
                DepartmentName = string.Empty,
                VatGroupId = 1,
                Price = 0,
                Limit = 0,
                Weighable = false,
                Commit = false
            });
        }

        dgvDepDefine.ItemsSource = _departments;
    }

    private void InitializeVatRows()
    {
        _vatRates.Clear();
        for (var i = 1; i <= ProgramConfig.MAX_VAT_RATE_COUNT; i++)
        {
            _vatRates.Add(new VatRow
            {
                VatId = i,
                VatRate = 0,
                Commit = false
            });
        }

        dgvVatDefine.ItemsSource = _vatRates;
    }

    private void InitializePluRows()
    {
        _plus.Clear();
        dgvPLUDefine.ItemsSource = _plus;
    }

    private void InitializeCreditRows()
    {
        _credits.Clear();
        for (var i = 0; i < ProgramConfig.MAX_CREDIT_COUNT; i++)
        {
            _credits.Add(new CreditRow { Id = i + 1, Name = string.Empty, Commit = false });
        }

        dgvCredits.ItemsSource = _credits;
    }

    private void InitializeCurrencyRows()
    {
        _currencies.Clear();
        dgvFCurrency.ItemsSource = _currencies;
    }

    private void InitializeMainCategoryRows()
    {
        _mainCategories.Clear();
        dgvMainCategory.ItemsSource = _mainCategories;
    }

    private void InitializeCashierRows()
    {
        _cashiers.Clear();
        dgvCashier.ItemsSource = _cashiers;
    }

    private void InitializeProgramOptionRows()
    {
        _programOptions.Clear();
        dgvPrmOption.ItemsSource = _programOptions;
    }

    private static string[] GetProgramOptionNames()
    {
        return System.Enum.GetNames(typeof(ProgramOptionSource));
    }

    private void SetStatusMessage(string message, bool logMessage = false)
    {
        txtStatus.Text = message;
        if (logMessage)
        {
            _bridge.Log(message);
        }
    }

    private void UpdateElapsedTimeLabel()
    {
        labelElapsedTime.Text = $"Elapsed Time : {_sendProductsElapsedSeconds} second(s)";
    }

    private void ProcessPendingUi()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitPendingUiFrame), frame);
        Dispatcher.PushFrame(frame);
    }

    private static object ExitPendingUiFrame(object frame)
    {
        ((DispatcherFrame)frame).Continue = false;
        return null!;
    }

    private static BitmapImage CreatePreviewBitmapSource(System.Drawing.Image image)
    {
        using var memoryStream = new MemoryStream();
        image.Save(memoryStream, image.RawFormat);
        memoryStream.Position = 0;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = memoryStream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void SetLanguageOption()
    {
        tbpDepTax.Header = FormMessage.DEPARTMENT;
        lblDepartment.Text = FormMessage.DEPARTMENT_DEFINITION;
        btnSaveDepartment.Content = FormMessage.SAVE_DEPARTMENT;
        btnGetDepartment.Content = FormMessage.GET_DEPARTMENT;

        tbpPLU.Header = FormMessage.PLU;
        lblPLUDefine.Text = FormMessage.PLU_DEFINITION;
        lblPLUStart.Text = FormMessage.PLU_ID;
        tbpCredit.Header = FormMessage.CREDIT;
        tbpCategory.Header = FormMessage.CATEGORY;
        tbpCashier.Header = FormMessage.CASHIER;
        tbpProgramOption.Header = FormMessage.PROGRAM_OPTIONS;
        tbpBitmapLogo.Header = FormMessage.GRAPHIC_LOGO;
        tbpNetwork.Header = FormMessage.NETWORK_SETTINGS;
        tabSRVLogo.Header = FormMessage.LOGO;
        tabSRVVAT.Header = FormMessage.VAT;
        tabPageEndOfReceiptNote.Header = FormMessage.END_OF_RECEIPT_NOTE;
        tabPageSendProduct.Header = "SEND PRODUCT";

        btnGetPLU.Content = FormMessage.GET_PLU;
        btnSavePLU.Content = FormMessage.SAVE_PLU;
        lblProductFile.Text = "PRODUCT FILE";
        btnBrowseProductFile.Content = "BROWSE";
        labelTotalLine.Text = "TOTAL LINE :";
        btnSendProducts.Content = "SEND";
        lblFCurrency.Text = FormMessage.CURRENCY_DEFINITION;
        btnGetFCurrency.Content = FormMessage.GET_F_CURRENCY;
        btnSaveFcurrency.Content = FormMessage.SAVE_F_CURRENCY;
        lblCredit.Text = FormMessage.CREDIT_DEFINITION;
        btnGetCredits.Content = FormMessage.GET_CREDIT;
        btnSaveCredits.Content = FormMessage.SAVE_CREDIT;
        lblMainGrup.Text = FormMessage.MAIN_GROUP;
        btnGetMainCategory.Content = FormMessage.GET_MAIN_CATEGORY;
        btnSaveMainCategory.Content = FormMessage.SAVE_MAIN_CATEGORY;
        lblCashier.Text = FormMessage.CASHIER_LIST;
        btnGetCashier.Content = FormMessage.GET_CASHIER;
        btnSaveCashier.Content = FormMessage.SAVE_CASHIER;
        lblProgramOption.Text = FormMessage.PROGRAM_OPTIONS;
        btnGetPrmOption.Content = FormMessage.GET_PROG_OPTIONS;
        btnSavePrmOption.Content = FormMessage.SAVE_PROG_OPTIONS;
        lblNetworkInfo.Text = FormMessage.AUTOMATIC_IP_MESSSAGE;
        lblGateway.Text = "Gateway :";
        lblSubnet.Text = "Subnet :";
        lblTsmIp.Text = "IP :";
        btnSaveNetwork.Content = FormMessage.SAVE;
        btnGetLogo.Content = FormMessage.GET_LOGO;
        btnSaveLogo.Content = FormMessage.SAVE_LOGO;
        btnBrowseBmp.Content = FormMessage.BROWSE_BITMAP;
        btnSendBitmap.Content = FormMessage.SAVE_LOGO;
        labelEORLine1.Text = FormMessage.LINE + " 1:";
        labelEORLine2.Text = FormMessage.LINE + " 2:";
        labelEORLine3.Text = FormMessage.LINE + " 3:";
        labelEORLine4.Text = FormMessage.LINE + " 4:";
        labelEORLine5.Text = FormMessage.LINE + " 5:";
        labelEORLine6.Text = FormMessage.LINE + " 6:";
        checkBoxEORLine1.Content = FormMessage.SAVE;
        checkBoxEORLine2.Content = FormMessage.SAVE;
        checkBoxEORLine3.Content = FormMessage.SAVE;
        checkBoxEORLine4.Content = FormMessage.SAVE;
        checkBoxEORLine5.Content = FormMessage.SAVE;
        checkBoxEORLine6.Content = FormMessage.SAVE;
        lblLogoHeight.Text = "Yükseklik 120px";
        lblLogoWidth.Text = "<-- Genişlik 570px -->";
        lblPreviewLogo.Text = FormMessage.PREVIEW;
        buttonEORGet.Content = FormMessage.GET;
        buttonEORSet.Content = FormMessage.SET;

        lblVAT.Text = FormMessage.DEFINE_VAT;
        btnSaveVat.Content = FormMessage.SAVE_VAT;
        btnGetVat.Content = FormMessage.GET_VAT;

        UpdateNetworkTextBoxStates();
    }

    private void BtnBrowseProductFile_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var fileDialog = new OpenFileDialog
        {
            Filter = "All Files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false
        };

        if (fileDialog.ShowDialog() == true)
        {
            textBoxProductFilePath.Text = fileDialog.FileName;
            var lines = File.ReadAllLines(fileDialog.FileName);
            labelTotalLineValue.Text = lines.Length.ToString();
            _productLineList = new List<string>(lines);
        }
    }

    private void NetworkCheckBox_OnCheckedChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        UpdateNetworkTextBoxStates();
    }

    private void UpdateNetworkTextBoxStates()
    {
        txtIp.IsEnabled = chkIp.IsChecked == true;
        txtSubnet?.IsEnabled = chkSubnet.IsChecked == true;
        txtGateway?.IsEnabled = chkGateway.IsChecked == true;
    }

    private void BtnSendProducts_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (_productLineList.Count == 0)
            {
                SetStatusMessage("Önce bir ürün dosyası seçiniz.");
                return;
            }

            _secondStage = false;
            progressBarSendProd.Value = 0;
            labelUnderProgressBar.Text = string.Empty;
            _sendProductsElapsedSeconds = 0;
            _stopSendProductsTimerRequested = false;
            labelElapsedTime.Text = string.Empty;
            _sendProductsTimer.Start();

            var worker = new BackgroundWorker();
            worker.DoWork += (_, _) =>
            {
                _ = new CPResponse(_bridge.Printer.SendMultipleProduct(_productLineList.ToArray()));
            };
            worker.RunWorkerCompleted += (_, args) =>
            {
                _stopSendProductsTimerRequested = true;

                if (args.Error is not null)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + " : " + args.Error.Message, logMessage: true);
                }
            };
            worker.RunWorkerAsync();
        }
        catch (Exception ex)
        {
            _stopSendProductsTimerRequested = true;
            SetStatusMessage(FormMessage.OPERATION_FAILS + " : " + ex.Message, logMessage: true);
        }
    }

    private void Printer_OnFileSendingProgress(object? sender, OnFileSendingProgressEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var progRate = int.Parse(e.Data);
            if (progRate > 100)
            {
                progRate = 100;
            }

            labelUnderProgressBar.Text = _secondStage ? "Product DB Sending to ECR.." : "Product DB Creating...";
            progressBarSendProd.Value = progRate;

            if (progRate == 100)
            {
                if (!_secondStage)
                {
                    labelUnderProgressBar.Text = "Product DB Created";
                    _secondStage = true;
                }
                else
                {
                    labelUnderProgressBar.Text = "Product DB sent successfully!";
                    _secondStage = false;
                }
            }
        });
    }

    private void SendProductsTimer_OnTick(object? sender, EventArgs e)
    {
        if (_stopSendProductsTimerRequested)
        {
            _sendProductsTimer.Stop();
            return;
        }

        _sendProductsElapsedSeconds++;
        UpdateElapsedTimeLabel();
    }

    private void BtnGetMainCategory_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            _mainCategories.Clear();
            for (var i = 0; i < ProgramConfig.MAX_MAIN_CATEGORY_COUNT; i++)
            {
                if (i % 5 == 0)
                {
                    ProcessPendingUi();
                }

                var response = new CPResponse(_bridge.Printer.GetMainCategory(i));
                if (response.ErrorCode != 0)
                {
                    continue;
                }

                var id = response.GetNextParam();
                var name = response.GetNextParam();

                if (int.TryParse(id, out var parsedId))
                {
                    _mainCategories.Add(new MainCategoryRow
                    {
                        Id = parsedId + 1,
                        Name = name ?? string.Empty,
                        Commit = false
                    });
                }
            }

            dgvMainCategory.Items.Refresh();
            SetStatusMessage("Ana kategori bilgileri alındı.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnEorGet_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            for (var i = 1; i <= 6; i++)
            {
                var response = new CPResponse(_bridge.Printer.GetEndOfReceiptNote(i));
                if (response.ErrorCode == 0)
                {
                    GetEorTextBox(i).Text = response.GetNextParam() ?? string.Empty;
                }
            }

            SetStatusMessage("Fiş sonu notları alındı.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnEorSet_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            for (var i = 1; i <= 6; i++)
            {
                if (GetEorCheckBox(i).IsChecked != true)
                {
                    continue;
                }

                var response = new CPResponse(_bridge.Printer.SetEndOfReceiptNote(i, GetEorTextBox(i).Text ?? string.Empty));
                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                GetEorTextBox(i).Text = response.GetNextParam() ?? string.Empty;
            }

            SetStatusMessage("Fiş sonu notları kaydedildi.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnBrowseBitmap_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Bitmap (*.bmp)|*.bmp|JPEG(*.jpeg)|*.jpeg|PNG(*.png)|*.png|JPG (*.jpg)|*.jpg|GIF (*.gif)|*.gif",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Title = "Lütfen resim dosyası seçiniz"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            _lastLogoPath = dialog.FileName;
            _selectedLogoImage?.Dispose();
            using (var image = System.Drawing.Image.FromFile(_lastLogoPath))
            {
                _selectedLogoImage = (System.Drawing.Image)image.Clone();
            }
            txtBitmapPath.Text = _lastLogoPath;
            imgLogoPreview.Source = CreatePreviewBitmapSource(_selectedLogoImage);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSendBitmap_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (_selectedLogoImage is null)
            {
                SetStatusMessage("Önce bir logo dosyası seçiniz.");
                return;
            }

            var index = checkBoxForGibLogo.IsChecked == true ? ProgramConfig.GIB_LOGO_NO : 0;

            _bridge.Log(FormMessage.SAVE_BITMAP_MESSAGE);
            var response = new CPResponse(_bridge.Printer.LoadGraphicLogo(_selectedLogoImage, index));

            SetStatusMessage(response.ErrorCode == 0
                ? FormMessage.OPERATION_SUCCESSFULL
                : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSaveNetwork_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var ip = string.Empty;
            var subnet = string.Empty;
            var gateway = string.Empty;

            if (!string.IsNullOrEmpty(txtIp.Text))
            {
                if (chkIp.IsChecked == true)
                {
                    ip = txtIp.Text;
                }

                if (chkSubnet.IsChecked == true)
                {
                    subnet = txtSubnet.Text;
                }

                if (chkGateway.IsChecked == true)
                {
                    gateway = txtGateway.Text;
                }
            }

            var response = new CPResponse(_bridge.Printer.SaveNetworkSettings(ip ?? string.Empty, subnet ?? string.Empty, gateway ?? string.Empty));
            SetStatusMessage(response.ErrorCode == 0
                ? "Ağ ayarları kaydedildi."
                : FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnGetLogo_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            for (var i = 0; i < ProgramConfig.LENGTH_OF_LOGO_LINES; i++)
            {
                if (GetLogoCheckBox(i).IsChecked != true)
                {
                    continue;
                }

                var response = new CPResponse(_bridge.Printer.GetLogo(i));
                if (response.ErrorCode == 0)
                {
                    GetLogoTextBox(i).Text = response.GetNextParam() ?? string.Empty;
                }
            }

            SetStatusMessage("Logo satırları alındı.");
        }
        catch (TimeoutException)
        {
            SetStatusMessage(FormMessage.TIMEOUT_ERROR, logMessage: true);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSaveLogo_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            for (var i = 0; i < ProgramConfig.LENGTH_OF_LOGO_LINES; i++)
            {
                if (GetLogoCheckBox(i).IsChecked != true)
                {
                    continue;
                }

                var lineText = GetLogoTextBox(i).Text ?? string.Empty;

                if (i == ProgramConfig.LENGTH_OF_LOGO_LINES - 1)
                {
                    if (lineText.Length > 11)
                    {
                        System.Windows.MessageBox.Show("Last logo line length must be max 11!");
                        return;
                    }

                    if (!CheckInputIsNumeric(lineText))
                    {
                        System.Windows.MessageBox.Show("Last logo line have to be numeric!");
                        return;
                    }
                }

                var response = new CPResponse(_bridge.Printer.SetLogo(i, lineText));
                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                _bridge.Log(FormMessage.OPERATION_SUCCESSFULL);
            }

            SetStatusMessage(FormMessage.OPERATION_SUCCESSFULL);
        }
        catch (TimeoutException)
        {
            SetStatusMessage(FormMessage.TIMEOUT_ERROR, logMessage: true);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private static bool CheckInputIsNumeric(string input)
    {
        return input.All(char.IsDigit);
    }

    private void LblInfoPLU_OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        System.Windows.MessageBox.Show(
            FormMessage.INFO_PLU_CLICK_1 + "\n" +
            FormMessage.INFO_PLU_CLICK_2 + "\n" +
            FormMessage.INFO_PLU_CLICK_3 + "\n" +
            FormMessage.INFO_PLU_CLICK_4);
    }

    private void LogoTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var suffix = textBox.Name.Replace("txtLogo", string.Empty);
        if (FindName($"chkLogo{suffix}") is CheckBox checkBox)
        {
            checkBox.IsChecked = true;
        }
    }

    private void LogoCheckBox_OnCheckedChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            checkBox.Content = checkBox.IsChecked == true ? "Seçme" : "Seç";
        }
    }

    private TextBox GetLogoTextBox(int index) => (TextBox)FindName($"txtLogo{index}")!;

    private CheckBox GetLogoCheckBox(int index) => (CheckBox)FindName($"chkLogo{index}")!;

    private TextBox GetEorTextBox(int index) => (TextBox)FindName($"textBoxEORLine{index}")!;

    private CheckBox GetEorCheckBox(int index) => (CheckBox)FindName($"checkBoxEORLine{index}")!;

    private void BtnGetProgramOption_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            _programOptions.Clear();
            var names = GetProgramOptionNames();
            for (var i = 0; i < names.Length; i++)
            {
                if (i % 5 == 0)
                {
                    ProcessPendingUi();
                }

                var response = new CPResponse(_bridge.Printer.GetProgramOptions(i));
                if (response.ErrorCode != 0)
                {
                    continue;
                }

                _programOptions.Add(new ProgramOptionRow
                {
                    Id = i + 1,
                    Name = names[i],
                    Value = response.GetNextParam() ?? string.Empty,
                    Commit = false
                });
            }

            dgvPrmOption.Items.Refresh();
            SetStatusMessage("Program seçenekleri alındı.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSaveProgramOption_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var selectedRows = _programOptions.Where(x => x.Commit).ToList();
            if (selectedRows.Count == 0)
            {
                SetStatusMessage("Kaydetmek için en az bir program seçeneği satırı seçiniz.");
                return;
            }

            foreach (var row in selectedRows)
            {
                var response = new CPResponse(_bridge.Printer.SaveProgramOptions(row.Id - 1, row.Value ?? string.Empty));
                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                row.Value = response.GetNextParam() ?? string.Empty;
                _bridge.Log(string.Format("{2}: {0} {3}:{1}", row.Id, row.Value, FormMessage.PROG_OPT_ID, FormMessage.PROG_OPT_VALUE));
            }

            dgvPrmOption.Items.Refresh();
            SetStatusMessage("Program seçenekleri kaydedildi.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSaveMainCategory_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var selectedRows = _mainCategories.Where(x => x.Commit).ToList();
            if (selectedRows.Count == 0)
            {
                SetStatusMessage("Kaydetmek için en az bir ana kategori satırı seçiniz.");
                return;
            }

            foreach (var row in selectedRows)
            {
                var response = new CPResponse(_bridge.Printer.SetMainCategory(row.Id - 1, row.Name ?? string.Empty));
                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                var groupName = response.GetNextParam() ?? string.Empty;
                _bridge.Log(string.Format("{2}: {0} {3}:{1} ", row.Id, groupName, FormMessage.GROUP_ID, FormMessage.GROUP_NAME));
            }

            SetStatusMessage("Ana kategori kayıtları gönderildi.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnGetCashier_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            _cashiers.Clear();
            for (var i = 0; i < ProgramConfig.MAX_CASHIER_COUNT; i++)
            {
                if (i % 5 == 0)
                {
                    ProcessPendingUi();
                }

                var response = new CPResponse(_bridge.Printer.GetCashier(i + 1));
                if (response.ErrorCode != 0)
                {
                    continue;
                }

                _cashiers.Add(new CashierRow
                {
                    Id = i + 1,
                    Name = response.GetNextParam() ?? string.Empty,
                    Password = string.Empty,
                    Commit = false
                });
            }

            dgvCashier.Items.Refresh();
            SetStatusMessage("Kasiyer bilgileri alındı.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSaveCashier_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var selectedRows = _cashiers.Where(x => x.Commit).ToList();
            if (selectedRows.Count == 0)
            {
                SetStatusMessage("Kaydetmek için en az bir kasiyer satırı seçiniz.");
                return;
            }

            foreach (var row in selectedRows)
            {
                var password = string.Empty;
                if (!string.IsNullOrEmpty(row.Password))
                {
                    password = Convert.ToInt32(row.Password).ToString();
                }

                var response = new CPResponse(_bridge.Printer.SaveCashier(row.Id, row.Name ?? string.Empty, password));
                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                var cashierName = response.GetNextParam() ?? string.Empty;
                _bridge.Log(string.Format("{2}: {0} {3}:{1}", row.Id, cashierName, FormMessage.CASHIER_ID, FormMessage.CASHIER_NAME));
            }

            SetStatusMessage("Kasiyer kayıtları gönderildi.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnGetPlu_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            _plus.Clear();

            foreach (var pluNo in ParsePluAddresses(txtPluAddress.Text))
            {
                var response = new CPResponse(_bridge.Printer.GetProduct(pluNo));
                if (response.ErrorCode != 0)
                {
                    continue;
                }

                _plus.Add(new PluRow
                {
                    PluNo = pluNo,
                    Name = response.GetNextParam() ?? string.Empty,
                    Department = int.TryParse(response.GetNextParam(), out var dept) ? dept : 1,
                    Price = decimal.TryParse(response.GetNextParam(), out var price) ? price : 0,
                    Weighable = int.TryParse(response.GetNextParam(), out var weighable) && weighable == 1,
                    Barcode = response.GetNextParam() ?? string.Empty,
                    SubCategory = int.TryParse(response.GetNextParam(), out var subCat) ? subCat : 1,
                    Commit = false
                });

                if (_plus.Count % 10 == 0)
                {
                    ProcessPendingUi();
                }
            }

            SetStatusMessage("PLU bilgileri alındı.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSavePlu_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var selectedRows = _plus.Where(x => x.Commit).ToList();
            if (selectedRows.Count == 0)
            {
                SetStatusMessage("Kaydetmek için en az bir PLU satırı seçiniz.");
                return;
            }

            foreach (var row in selectedRows)
            {
                var response = new CPResponse(_bridge.Printer.SaveProduct(
                    row.PluNo,
                    row.Name ?? string.Empty,
                    row.Department,
                    row.Price,
                    row.Weighable ? 1 : 0,
                    row.Barcode ?? string.Empty,
                    row.SubCategory));

                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                var savedName = response.GetNextParam() ?? string.Empty;
                var savedDepartment = int.TryParse(response.GetNextParam(), out var parsedDepartment) ? parsedDepartment : 0;
                var savedPrice = decimal.TryParse(response.GetNextParam(), out var parsedSavedPrice) ? parsedSavedPrice : 0;
                var savedWeighable = int.TryParse(response.GetNextParam(), out var parsedSavedWeighable) ? parsedSavedWeighable : 0;
                var savedBarcode = response.GetNextParam() ?? string.Empty;
                var savedSubCategory = int.TryParse(response.GetNextParam(), out var parsedSavedSubCategory) ? parsedSavedSubCategory : 0;

                _bridge.Log(string.Format(
                    "{6}:{0,-20} {7}: {1,2} {8}: {2:#0.00} {9}:{3} {10}: {4,-20} {11}:{5}",
                    savedName,
                    savedDepartment,
                    savedPrice,
                    savedWeighable == 1 ? "E" : "H",
                    savedBarcode,
                    savedSubCategory,
                    FormMessage.PLU_NAME,
                    FormMessage.DEPARTMENT_ID,
                    FormMessage.PRICE,
                    FormMessage.WEIGHABLE,
                    FormMessage.BARCODE,
                    FormMessage.SUB_CATEGORY));
            }

            SetStatusMessage("PLU kayıtları gönderildi.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnGetCredits_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            for (var i = 0; i < _credits.Count; i++)
            {
                if (i % 5 == 0)
                {
                    ProcessPendingUi();
                }

                _credits[i].Name = string.Empty;
                _credits[i].Commit = false;

                var response = new CPResponse(_bridge.Printer.GetCreditInfo(i));
                if (response.ErrorCode != 0)
                {
                    _credits[i].Name = string.Empty;
                    continue;
                }

                var name = response.GetNextParam() ?? string.Empty;
                _credits[i].Name = name;
                MainWindow.SetCredit(i, name);
            }

            dgvCredits.Items.Refresh();
            SetStatusMessage("Kredi bilgileri alındı.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSaveCredits_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var selectedRows = _credits.Where(x => x.Commit).ToList();
            if (selectedRows.Count == 0)
            {
                SetStatusMessage("Kaydetmek için en az bir kredi satırı seçiniz.");
                return;
            }

            foreach (var row in selectedRows)
            {
                var response = new CPResponse(_bridge.Printer.SetCreditInfo(row.Id - 1, row.Name ?? string.Empty));
                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                row.Name = response.GetNextParam() ?? string.Empty;
            }

            dgvCredits.Items.Refresh();
            SetStatusMessage("Kredi kayıtları gönderildi.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnGetFCurrency_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            _currencies.Clear();
            for (var i = 0; i < ProgramConfig.MAX_FCURRENCY_COUNT; i++)
            {
                if (i % 5 == 0)
                {
                    ProcessPendingUi();
                }

                var response = new CPResponse(_bridge.Printer.GetCurrencyInfo(i));
                if (response.ErrorCode != 0)
                {
                    continue;
                }

                var name = response.GetNextParam() ?? string.Empty;
                var rate = response.GetNextParam();
                var parsedRate = decimal.TryParse(rate, out var currentRate) ? currentRate : 0;

                _currencies.Add(new CurrencyRow { Id = i + 1, Name = name, Rate = parsedRate, Commit = false });

                MainWindow.SetCurrency(i, new FCurrency { ID = i, Name = name, Rate = parsedRate });
            }

            dgvFCurrency.Items.Refresh();
            SetStatusMessage("Döviz bilgileri alındı.");
        }
        catch (TimeoutException)
        {
            SetStatusMessage(FormMessage.TIMEOUT_ERROR, logMessage: true);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSaveFCurrency_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var selectedRows = _currencies.Where(x => x.Commit).ToList();
            if (selectedRows.Count == 0)
            {
                SetStatusMessage("Kaydetmek için en az bir döviz satırı seçiniz.");
                return;
            }

            foreach (var row in selectedRows)
            {
                var response = new CPResponse(_bridge.Printer.SetCurrencyInfo(row.Id - 1, row.Name ?? string.Empty, row.Rate));
                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                var savedCurrency = new FCurrency
                {
                    ID = row.Id - 1,
                    Name = response.GetNextParam() ?? string.Empty,
                    Rate = decimal.TryParse(response.GetNextParam(), out var savedRate) ? savedRate : 0
                };
                row.Name = savedCurrency.Name;
                row.Rate = savedCurrency.Rate;
                MainWindow.SetCurrency(savedCurrency.ID, savedCurrency);
            }

            dgvFCurrency.Items.Refresh();
            SetStatusMessage("Döviz kayıtları gönderildi.");
        }
        catch (TimeoutException)
        {
            SetStatusMessage(FormMessage.TIMEOUT_ERROR, logMessage: true);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private static IEnumerable<int> ParsePluAddresses(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        var tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (token.Contains('-'))
            {
                var range = token.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (range.Length == 2 && int.TryParse(range[0], out var min) && int.TryParse(range[1], out var max) && max >= min)
                {
                    for (var i = min; i <= max; i++)
                    {
                        yield return i;
                    }
                }
                continue;
            }

            if (int.TryParse(token, out var single))
            {
                yield return single;
            }
        }
    }

    private void BtnSaveDepartment_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var selectedRows = _departments.Where(x => x.Commit).ToList();
            if (selectedRows.Count == 0)
            {
                SetStatusMessage("Kaydetmek için en az bir satır seçiniz.");
                return;
            }

            foreach (var row in selectedRows)
            {
                var response = new CPResponse(_bridge.Printer.SetDepartment(
                    row.DepartmentId,
                    row.DepartmentName ?? string.Empty,
                    row.VatGroupId,
                    row.Price,
                    row.Weighable ? 1 : 0,
                    row.Limit));

                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }

                var paramVal = response.GetNextParam();
                if (!string.IsNullOrEmpty(paramVal))
                {
                    row.DepartmentName = paramVal;
                }

                paramVal = response.GetNextParam();
                if (int.TryParse(paramVal, out var parsedVatGroup))
                {
                    row.VatGroupId = parsedVatGroup;
                }

                paramVal = response.GetNextParam();
                if (decimal.TryParse(paramVal, out var parsedPrice))
                {
                    row.Price = parsedPrice;
                }

                paramVal = response.GetNextParam();
                if (int.TryParse(paramVal, out var parsedWeighable))
                {
                    row.Weighable = parsedWeighable == 1;
                }

                paramVal = response.GetNextParam();
                if (decimal.TryParse(paramVal, out var parsedLimit))
                {
                    row.Limit = parsedLimit;
                }
            }

            dgvDepDefine.Items.Refresh();
            var message = "Departman kayıtları gönderildi.";
            SetStatusMessage(message, logMessage: true);
        }
        catch (TimeoutException)
        {
            SetStatusMessage(FormMessage.TIMEOUT_ERROR, logMessage: true);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnGetDepartment_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            foreach (var row in _departments)
            {
                if ((row.DepartmentId - 1) % 5 == 0)
                {
                    ProcessPendingUi();
                }

                row.DepartmentName = string.Empty;
                row.VatGroupId = 1;
                row.Price = 0;
                row.Limit = 0;
                row.Weighable = false;
                row.Commit = false;

                var response = new CPResponse(_bridge.Printer.GetDepartment(row.DepartmentId));
                if (response.ErrorCode != 0)
                {
                    return;
                }

                var name = response.GetNextParam();
                var vatGroup = response.GetNextParam();
                var price = response.GetNextParam();
                var weighable = response.GetNextParam();
                var limit = response.GetNextParam();

                if (!string.IsNullOrEmpty(name)) row.DepartmentName = name;
                if (int.TryParse(vatGroup, out var parsedVatGroup)) row.VatGroupId = parsedVatGroup;
                if (decimal.TryParse(price, out var parsedPrice)) row.Price = parsedPrice;
                if (int.TryParse(weighable, out var parsedWeighable)) row.Weighable = parsedWeighable == 1;
                if (decimal.TryParse(limit, out var parsedLimit)) row.Limit = parsedLimit;
            }

            dgvDepDefine.Items.Refresh();
            SetStatusMessage("Departman bilgileri alındı.");
        }
        catch (TimeoutException)
        {
            SetStatusMessage(FormMessage.TIMEOUT_ERROR, logMessage: true);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnSaveVat_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var selectedRows = _vatRates.Where(x => x.Commit).ToList();
            if (selectedRows.Count == 0)
            {
                SetStatusMessage("Kaydetmek için en az bir KDV satırı seçiniz.");
                return;
            }

            foreach (var row in selectedRows)
            {
                var response = new CPResponse(_bridge.Printer.SetVATRate(row.VatId - 1, row.VatRate));
                if (response.ErrorCode != 0)
                {
                    SetStatusMessage(FormMessage.OPERATION_FAILS + ": " + response.ErrorMessage);
                    return;
                }
            }

            var message = "KDV oranları kaydedildi.";
            SetStatusMessage(message, logMessage: true);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void BtnGetVat_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            foreach (var row in _vatRates)
            {
                if ((row.VatId - 1) % 5 == 0)
                {
                    ProcessPendingUi();
                }

                row.VatRate = 0;
                row.Commit = false;

                var response = new CPResponse(_bridge.Printer.GetVATRate(row.VatId - 1));
                if (response.ErrorCode != 0)
                {
                    continue;
                }

                var rate = response.GetNextParam();
                if (int.TryParse(rate, out var parsedRate))
                {
                    row.VatRate = parsedRate;
                }
            }

            dgvVatDefine.Items.Refresh();
            SetStatusMessage("KDV bilgileri alındı.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void SetError(Exception ex)
    {
        var message = FormMessage.OPERATION_FAILS + ": " + ex.Message;
        SetStatusMessage(message, logMessage: true);
    }
}

public class DepartmentRow
{
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int VatGroupId { get; set; }
    public decimal Price { get; set; }
    public decimal Limit { get; set; }
    public bool Weighable { get; set; }
    public bool Commit { get; set; }
}

public class VatRow
{
    public int VatId { get; set; }
    public int VatRate { get; set; }
    public bool Commit { get; set; }
}

public class PluRow
{
    public int PluNo { get; set; }
    public string? Name { get; set; }
    public int Department { get; set; }
    public decimal Price { get; set; }
    public bool Weighable { get; set; }
    public string? Barcode { get; set; }
    public int SubCategory { get; set; }
    public bool Commit { get; set; }
}

public class CreditRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Commit { get; set; }
}

public class CurrencyRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Rate { get; set; }
    public bool Commit { get; set; }
}

public class MainCategoryRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Commit { get; set; }
}

public class CashierRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Password { get; set; }
    public bool Commit { get; set; }
}

public class ProgramOptionRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    public bool Commit { get; set; }
}

public class LogoLineRow
{
    public int LineNo { get; set; }
    public string? Text { get; set; }
    public bool Selected { get; set; }
}

public class EorLineRow
{
    public int LineNo { get; set; }
    public string? Text { get; set; }
    public bool Save { get; set; }
}
