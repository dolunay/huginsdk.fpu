using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace FP300Service.Views;

public partial class TestServer : Window, IDisposable
{
    private static readonly Encoding DefaultEncoding = Encoding.GetEncoding(1254);

    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }

    private TcpListener? listener;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? serverTask;

    public TestServer()
    {
        InitializeComponent();
    }

    private async Task StartTestServerAsync(string tcpIp, int port)
    {
        try
        {
            var address = ParseIpAddress(tcpIp);
            listener?.Stop();
            listener = new TcpListener(address, port);
            listener.Start();
            Log("Ip ve port dinleme başladı");

            cancellationTokenSource = new CancellationTokenSource();
            serverTask = AcceptClientAsync(cancellationTokenSource.Token);
            await Task.Yield();
        }
        catch (Exception ex)
        {
            Log("Hata :" + ex.Message);
        }
    }

    private IPAddress ParseIpAddress(string tcpIp)
    {
        var split = tcpIp.Split(',');
        if (split.Length != 4)
        {
            throw new Exception("IP Geçersiz");
        }

        var ip = new byte[4];
        for (var i = 0; i < split.Length; i++)
        {
            ip[i] = Convert.ToByte(split[i].Trim());
        }

        return new IPAddress(ip);
    }

    private void Log(string line)
    {
        Dispatcher.Invoke(() =>
        {
            txtLog.AppendText(line + Environment.NewLine);
            txtLog.ScrollToEnd();
        });
    }

    private async Task AcceptClientAsync(CancellationToken cancellationToken)
    {
        try
        {
            Log("Client bekleniyor.");
            var client = await listener!.AcceptTcpClientAsync(cancellationToken);
            client.ReceiveTimeout = 500;
            await WaitMessageAsync(client, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log("Hata: " + ex.Message);
        }
    }

    private async Task WaitMessageAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            var stream = client.GetStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                Log("Mesaj bekleniyor");

                while (!stream.DataAvailable && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(10, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var buffer = new byte[1024];
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                var recvMsg = DefaultEncoding.GetString(buffer);
                Log("Mesaj alındı: " + recvMsg);

                if (recvMsg.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    var resBuff = DefaultEncoding.GetBytes("DONE");
                    buffer = new byte[resBuff.Length + 2];
                    buffer[0] = (byte)(resBuff.Length / 256);
                    buffer[1] = (byte)(resBuff.Length % 256);
                    Array.Copy(resBuff, 0, buffer, 2, resBuff.Length);
                    await stream.WriteAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    Log("Cevap gönderildi.");
                }
                else
                {
                    Log("Gelen mesaj çözümlenemedi.");
                }
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    private async void BtnStart_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (serverTask != null && !serverTask.IsCompleted)
            {
                cancellationTokenSource?.Cancel();
                listener?.Stop();
                Log("Server kapatıldı.");
                btnStart.Content = "Başlat";
                return;
            }

            btnStart.Content = "Durdur";
            await StartTestServerAsync(IpAddress, Port);
        }
        catch
        {
            btnStart.Content = "Başlat";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        try
        {
            if (serverTask != null && !serverTask.IsCompleted)
            {
                Log("Server kapatıldı.");
            }

            cancellationTokenSource?.Cancel();
            listener?.Stop();
        }
        catch
        {
        }
    }
}
