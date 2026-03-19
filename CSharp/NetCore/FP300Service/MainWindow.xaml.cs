using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Hugin.Common;
using Hugin.POS.CompactPrinter.FP300;
using FP300Service;
using FP300NetCoreService.Views;

namespace FP300NetCoreService;

public partial class MainWindow : Window, IBridge
{
    private sealed class MainMenuItem
    {
        public required string Text { get; init; }
        public required string Glyph { get; init; }
    }

    private static string fiscalId = "FP12345678";
    //public static Encoding DefaultEncoding = Encoding.GetEncoding(1254);
    private static readonly string[] credits = new string[ProgramConfig.MAX_CREDIT_COUNT];
    private static readonly FCurrency[] currencies = new FCurrency[ProgramConfig.MAX_FCURRENCY_COUNT];
    private static readonly string[] foodCards = new string[ProgramConfig.MAX_CREDIT_COUNT];

    private static ICompactPrinter? printer;
    private static IConnection? conn;
    private static bool isMatchedBefore;
    private static int seqNumber = 1;

    public MainWindow()
    {
        InitializeComponent();
        SetLanguageOption();
        ApplyLegacySettings();
        FillMenu();
        UpdateTitle();
        Closing += MainWindow_Closing;
    }

    public ICompactPrinter Printer => printer!;

    public IConnection Connection
    {
        get => conn!;
        set => conn = value;
    }

    public static string[] Credits => credits;
    public static string[] FoodCards => foodCards;
    public static FCurrency[] Currencies => currencies;

    public static int SequenceNumber
    {
        get => seqNumber;
        set => seqNumber = value;
    }

    public static string FiscalId => fiscalId;

    public static void SetCredit(int id, string creditName)
    {
        credits[id] = creditName;
    }

    public static void SetFoodCard(int id, string foodCardName)
    {
        foodCards[id] = foodCardName;
    }

    public static void SetCurrency(int id, FCurrency currency)
    {
        if (id < currencies.Length)
        {
            currencies[id] = currency;
        }
    }

    public static void SetFiscalId(string strId)
    {
        if (!strId.StartsWith("FP", StringComparison.OrdinalIgnoreCase) || strId.Length < 3)
        {
            throw new Exception("Geçersiz mali numara.");
        }

        var id = int.Parse(strId[2..]);
        if (id == 0 || id > 99999999)
        {
            throw new Exception("Geçersiz mali numara.");
        }

        fiscalId = strId.ToUpperInvariant();

        if (printer != null)
        {
            printer.FiscalRegisterNo = fiscalId;
        }
    }

    private void SetLanguageOption()
    {
        btnConnect.Content = FormMessage.CONNECT;
        tabComPort.Header = FormMessage.SERIAL_PORT;
        lblFiscalId.Text = "FISCAL ID :";
        lblTcpIp.Text = "IP :";
        lblTcpPort.Text = "Port :";
        lblComPort.Text = "PORT :";
        lblBaudrate.Text = "BAUDRATE :";
    }

    private void ApplyLegacySettings()
    {
        try
        {
            var legacyFiscalId = GetLegacyAppSetting("FiscalId");
            if (!string.IsNullOrWhiteSpace(legacyFiscalId))
            {
                txtFiscalId.Text = legacyFiscalId;
            }

            var legacyIp = GetLegacyAppSetting("IP");
            if (!string.IsNullOrWhiteSpace(legacyIp))
            {
                txtTCPIP.Text = legacyIp;
            }

            var legacyPort = GetLegacyAppSetting("Port");
            if (!string.IsNullOrWhiteSpace(legacyPort))
            {
                txtTcpPort.Text = legacyPort;
            }
        }
        catch
        {
        }
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
                .Elements("add")?
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

    private void FillMenu()
    {
        lvievMenu.Items.Clear();
        lvievMenu.Items.Add(new MainMenuItem { Text = FormMessage.STATUS_CHECK, Glyph = "\uE9D9" });
        lvievMenu.Items.Add(new MainMenuItem { Text = FormMessage.CASH_REGISTER_INFO, Glyph = "\uE8D2" });
        lvievMenu.Items.Add(new MainMenuItem { Text = FormMessage.PROGRAM, Glyph = "\uE713" });
        lvievMenu.Items.Add(new MainMenuItem { Text = FormMessage.REPORTS, Glyph = "\uE9D2" });
        lvievMenu.Items.Add(new MainMenuItem { Text = FormMessage.SALE, Glyph = "\uE719" });
        lvievMenu.Items.Add(new MainMenuItem { Text = FormMessage.SERVICE, Glyph = "\uE9CE" });
    }

    private void UpdateTitle()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var suffix = version.Length >= 3 ? version[^3..] : version;
        Title = $"ECR TEST v{suffix}";
    }

    private void LvievMenu_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lvievMenu.SelectedItem is not MainMenuItem selected)
        {
            return;
        }

        ShowSection(selected.Text);
        Log($"{selected.Text} ekranı seçildi.");
    }

    private void ShowSection(string sectionName)
    {
        if (sectionName == FormMessage.STATUS_CHECK)
        {
            mainContentHost.Content = new UtililtyFuncsView(this);
            return;
        }

        if (sectionName == FormMessage.CASH_REGISTER_INFO)
        {
            mainContentHost.Content = new FiscalInfoView(this);
            return;
        }

        if (sectionName == FormMessage.PROGRAM)
        {
            mainContentHost.Content = new ProgramView(this);
            return;
        }

        if (sectionName == FormMessage.SALE)
        {
            mainContentHost.Content = new SaleView(this);
            return;
        }

        if (sectionName == FormMessage.REPORTS)
        {
            mainContentHost.Content = new ReportsView(this);
            return;
        }

        if (sectionName == FormMessage.SERVICE)
        {
            mainContentHost.Content = new ServiceView(this);
            return;
        }

        var description = sectionName switch
        {
            _ => "Bu ekran için içerik henüz tanımlanmadı."
        };

        mainContentHost.Content = new MainSectionView(sectionName, description);
    }

    private void CmbPorts_OnDropDownOpened(object sender, EventArgs e)
    {
        cmbPorts.Items.Clear();
        foreach (var name in System.IO.Ports.SerialPort.GetPortNames())
        {
            cmbPorts.Items.Add(name);
        }
    }

    private void BtnConnect_OnClick(object sender, RoutedEventArgs e)
    {
        var errPrefix = FormMessage.CONNECTION_ERROR + ": ";

        try
        {
            if (Connection == null)
            {
                if (tabConn.SelectedItem == tabComPort)
                {
                    if (string.IsNullOrWhiteSpace(cmbPorts.Text))
                    {
                        throw new InvalidOperationException("Port seçiniz.");
                    }

                    Connection = new SerialConnection(cmbPorts.Text, int.Parse(txtBaudrate.Text));
                }
                else
                {
                    Connection = new TCPConnection(txtTCPIP.Text, int.Parse(txtTcpPort.Text));
                }

                Log(FormMessage.CONNECTING + "... (" + FormMessage.PLEASE_WAIT + ")");
                Connection.Open();

                errPrefix = FormMessage.MATCHING_ERROR + ": ";
                MatchExDevice();

                SetFiscalId(txtFiscalId.Text);
                btnConnect.Content = FormMessage.DISCONNECT;
                Log(FormMessage.CONNECTED);
            }
            else
            {
                Connection.Close();
                Connection = null!;
                btnConnect.Content = FormMessage.CONNECT;
                Log(FormMessage.DISCONNECTED);
            }
        }
        catch (Exception ex)
        {
            Log(FormMessage.OPERATION_FAILS + ": " + errPrefix + ex.Message);

            if (conn != null)
            {
                btnConnect.Content = FormMessage.DISCONNECT;
            }
        }
    }

    public void Log(string log)
    {
        Dispatcher.Invoke(() =>
        {
            txtLog.AppendText($"# {log}{Environment.NewLine}");
            txtLog.ScrollToEnd();
        });
    }

    public void Log()
    {
        Dispatcher.Invoke(() =>
        {
            if (printer == null)
            {
                return;
            }

            var lastLog = printer.GetLastLog();
            if (string.IsNullOrEmpty(lastLog))
            {
                return;
            }

            txtLog.AppendText("***************************************************" + Environment.NewLine);

            if (!lastLog.Contains('|'))
            {
                Log(lastLog);
                return;
            }

            var parsed = lastLog.Split('|');
            if (parsed.Length != 5)
            {
                Log(lastLog);
                return;
            }

            var command = parsed[0];
            var sequence = parsed[1];
            var state = parsed[2];
            var errorCode = parsed[3];
            var errorMessage = parsed[4];

            if (command != "NULL")
            {
                txtLog.AppendText($"{sequence} {FormMessage.COMMAND}: {command}{Environment.NewLine}");
                txtLog.AppendText($"  {FormMessage.FPU_STATE}: {state}{Environment.NewLine}");
            }

            txtLog.AppendText($"  {FormMessage.RESPONSE}: {errorMessage} ({errorCode}){Environment.NewLine}");
            txtLog.ScrollToEnd();
        });
    }

    private void MatchExDevice()
    {
        SetFiscalId(txtFiscalId.Text);

        var serverInfo = new DeviceInfo
        {
            IP = IPAddress.Parse(GetIPAddress()),
            IPProtocol = IPProtocol.IPV4,
            Brand = "HUGIN",
            Model = "HUGIN COMPACT",
            Port = int.Parse(txtTcpPort.Text),
            TerminalNo = txtFiscalId.Text.PadLeft(8, '0'),
            Version = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime.ToShortDateString(),
            SerialNum = CreateMD5(Environment.MachineName).Substring(0, 8)
        };

        if (!conn!.IsOpen)
        {
            return;
        }

        if (isMatchedBefore && printer != null)
        {
            printer.SetCommObject(conn.ToObject());
            return;
        }

        printer = new CompactPrinter
        {
            FiscalRegisterNo = fiscalId
        };

        if (!printer.Connect(conn.ToObject(), serverInfo))
        {
            throw new OperationCanceledException(FormMessage.UNABLE_TO_MATCH);
        }

        if (printer.PrinterBufferSize != conn.BufferSize)
        {
            conn.BufferSize = printer.PrinterBufferSize;
        }

        printer.SetCommObject(conn.ToObject());
        isMatchedBefore = true;
        CPResponse.Bridge = this;
    }

    public static string CreateMD5(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        foreach (var hashByte in hashBytes)
        {
            sb.Append(hashByte.ToString("X2"));
        }

        return sb.ToString();
    }

    private static string GetIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        return "127.0.0.1";
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        conn?.Close();
    }

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);
}
