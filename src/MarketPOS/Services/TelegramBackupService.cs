using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MarketPOS.Data;
using MarketPOS.Data.Repositories;

namespace MarketPOS.Services;

public class TelegramBackupService
{
    private static System.Threading.Timer? _backgroundTimer;
    private static bool _isSending;

    // Resilient HttpClient with intelligent DNS fallback (immune to ISP DNS failures)
    private static readonly HttpClient _httpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(15),
            ConnectCallback = async (context, cancellationToken) =>
            {
                Socket? socket = null;
                try
                {
                    // 1. Try normal system DNS resolution
                    var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
                    socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception) when (context.DnsEndPoint.Host.Equals("api.telegram.org", StringComparison.OrdinalIgnoreCase))
                {
                    socket?.Dispose();

                    // 2. Fallback: If Turkish ISP DNS fails to resolve api.telegram.org (No such host is known),
                    // connect directly to Telegram's official public servers.
                    var fallbackIps = new[]
                    {
                        IPAddress.Parse("149.154.166.110"),
                        IPAddress.Parse("149.154.167.220"),
                        IPAddress.Parse("149.154.167.221")
                    };

                    var fallbackSocket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    await fallbackSocket.ConnectAsync(fallbackIps, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(fallbackSocket, ownsSocket: true);
                }
            }
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    private readonly SettingsRepository _settingsRepo = new();
    private readonly SaleRepository _saleRepo = new();
    private readonly ProductRepository _productRepo = new();

    public static void StartBackgroundWorker()
    {
        if (_backgroundTimer != null) return;

        // Check every 60 seconds
        _backgroundTimer = new System.Threading.Timer(async _ =>
        {
            if (_isSending) return;
            var service = new TelegramBackupService();
            await service.CheckAndSendPeriodicBackupAsync();
        }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
    }

    public async Task<bool> CheckAndSendPeriodicBackupAsync()
    {
        if (_isSending) return false;

        string enabled = _settingsRepo.Get("TelegramEnabled", "0");
        if (enabled != "1") return false;

        string token = _settingsRepo.Get("TelegramBotToken", "");
        string chatId = _settingsRepo.Get("TelegramChatId", "");
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId)) return false;

        int periodMinutes = int.TryParse(_settingsRepo.Get("TelegramPeriodMinutes", "60"), out int p) ? p : 60;
        string lastBackupStr = _settingsRepo.Get("TelegramLastBackupTime", "");

        if (!string.IsNullOrEmpty(lastBackupStr) &&
            DateTime.TryParse(lastBackupStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastDt))
        {
            if ((DateTime.Now - lastDt).TotalMinutes < periodMinutes)
            {
                // Not yet time
                return false;
            }
        }

        // Fast network hardware check: if no network adapter is connected, skip silently
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return false;
        }

        return await SendBackupAsync(token, chatId, isManual: false);
    }

    public async Task<bool> SendBackupAsync(string token, string chatId, bool isManual = false)
    {
        if (_isSending) return false;
        _isSending = true;

        string tempBackupFile = Path.Combine(Path.GetTempPath(), $"market_pos_{DateTime.Now:yyyyMMdd_HHmmss}.db");

        try
        {
            // 1. Prepare clean database backup file
            SettingsRepository.BackupDatabase(tempBackupFile);

            // 2. Build informative Telegram message caption
            string marketName = _settingsRepo.Get("MarketName", "MARKET");
            var summary = _saleRepo.GetDailySummary(DateTime.Today);

            string safeMarketName = System.Net.WebUtility.HtmlEncode(marketName);
            string safeFileName = System.Net.WebUtility.HtmlEncode(Path.GetFileName(tempBackupFile));

            string caption = $"🛒 <b>Market POS Bulut Yedek</b>\n" +
                             $"🏪 <b>Market:</b> {safeMarketName}\n" +
                             $"📅 <b>Tarih:</b> {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n" +
                             $"💰 <b>Bugünkü Ciro:</b> {summary.TotalTurnover:N2} ₺ ({summary.ReceiptCount} Fiş)\n" +
                             $"💵 <b>Nakit:</b> {summary.CashTotal:N2} ₺ | 💳 <b>Kart:</b> {summary.CardTotal:N2} ₺\n" +
                             $"📁 <b>Dosya:</b> {safeFileName}";

            byte[] fileBytes = await File.ReadAllBytesAsync(tempBackupFile);
            string url = $"https://api.telegram.org/bot{token.Trim()}/sendDocument";

            // 3. Send with automatic retry (up to 3 attempts with progressive delay)
            Exception? lastException = null;
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var form = new MultipartFormDataContent();
                    form.Add(new StringContent(chatId.Trim()), "chat_id");
                    form.Add(new StringContent(caption), "caption");
                    form.Add(new StringContent("HTML"), "parse_mode");

                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    form.Add(fileContent, "document", Path.GetFileName(tempBackupFile));

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var response = await _httpClient.PostAsync(url, form, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        _settingsRepo.Set("TelegramLastBackupTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                        return true;
                    }

                    string errorBody = await response.Content.ReadAsStringAsync();

                    // Fallback for HTML entity parsing
                    if (errorBody.Contains("parse", StringComparison.OrdinalIgnoreCase))
                    {
                        using var plainForm = new MultipartFormDataContent();
                        plainForm.Add(new StringContent(chatId.Trim()), "chat_id");
                        plainForm.Add(new StringContent($"Market POS Bulut Yedek\nMarket: {marketName}\nTarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\nCiro: {summary.TotalTurnover:N2} TL"), "caption");
                        var plainContent = new ByteArrayContent(fileBytes);
                        plainContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        plainForm.Add(plainContent, "document", Path.GetFileName(tempBackupFile));

                        var retryResp = await _httpClient.PostAsync(url, plainForm, cts.Token);
                        if (retryResp.IsSuccessStatusCode)
                        {
                            _settingsRepo.Set("TelegramLastBackupTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                            return true;
                        }
                    }

                    // Check for definitive user configuration errors (do not retry if credentials are wrong)
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new InvalidOperationException("Telegram Bot Token geçersiz! Lütfen BotFather'dan aldığınız token'ı doğru girdiğinizden emin olun.");
                    }
                    if (response.StatusCode == HttpStatusCode.BadRequest && errorBody.Contains("chat not found", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Telegram Chat ID bulunamadı! Lütfen botu sohbetinize veya kanalınıza eklediğinizden ve bir kez /start mesajı gönderdiğinizden emin olun.");
                    }

                    throw new InvalidOperationException($"Telegram API Hatası ({response.StatusCode}): {errorBody}");
                }
                catch (Exception ex) when (attempt < maxRetries && (ex is HttpRequestException || ex is TaskCanceledException || ex is OperationCanceledException || ex is IOException || ex is SocketException))
                {
                    lastException = ex;
                    // Wait before next retry: 1.5s on attempt 1, 3s on attempt 2
                    await Task.Delay(attempt * 1500);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break;
                }
            }

            if (isManual && lastException != null)
            {
                if (lastException is TaskCanceledException || lastException is OperationCanceledException)
                {
                    throw new InvalidOperationException("Telegram sunucularına bağlanırken zaman aşımı oluştu. Lütfen internet bağlantınızı kontrol edip tekrar deneyin.");
                }
                if (lastException is HttpRequestException || lastException is SocketException)
                {
                    throw new InvalidOperationException($"İnternet veya Telegram sunucu erişiminde ağ hatası oluştu: {lastException.Message}");
                }
                throw lastException;
            }

            return false;
        }
        finally
        {
            _isSending = false;
            try
            {
                if (File.Exists(tempBackupFile))
                    File.Delete(tempBackupFile);
            }
            catch { }
        }
    }

    public static async Task<bool> IsInternetAvailableAsync()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return false;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            using var response = await _httpClient.GetAsync("https://api.telegram.org", HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return (int)response.StatusCode < 500;
        }
        catch
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("149.154.166.110", 2500);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}
