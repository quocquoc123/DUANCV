using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Services;

public class MomoService
{
    private readonly MomoSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MomoService> _logger;

    public MomoService(IOptions<MomoSettings> settings, HttpClient httpClient, ILogger<MomoService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.PartnerCode) &&
        !string.IsNullOrWhiteSpace(_settings.AccessKey) &&
        !string.IsNullOrWhiteSpace(_settings.SecretKey) &&
        _settings.SecretKey.Length >= 20;

    public async Task<MomoCreatePaymentResult> CreatePaymentAsync(string orderId, long amount, string orderInfo)
    {
        var requestId = Guid.NewGuid().ToString();
        var extraData = string.Empty;
        var requestType = _settings.RequestType;
        var safeAmount = Math.Max(amount, 1000);
        var safeOrderInfo = string.IsNullOrWhiteSpace(orderInfo) ? $"Thanh toan {orderId}" : orderInfo;
        var redirectUrl = _settings.ReturnUrl?.Trim() ?? string.Empty;
        var ipnUrl = _settings.NotifyUrl?.Trim() ?? string.Empty;

        if (!IsConfigured)
        {
            _logger.LogWarning("MoMo credentials chưa cấu hình — dùng QR dự phòng cho đơn {OrderId}", orderId);
            return BuildFallbackResult(orderId, requestId, safeAmount, "Chưa cấu hình MoMo credentials");
        }

        var rawSignature = BuildCreateSignature(
            _settings.AccessKey,
            safeAmount,
            extraData,
            ipnUrl,
            orderId,
            safeOrderInfo,
            _settings.PartnerCode,
            redirectUrl,
            requestId,
            requestType);

        var signature = Sign(rawSignature);

        var payload = new Dictionary<string, object>
        {
            ["partnerCode"] = _settings.PartnerCode,
            ["partnerName"] = _settings.PartnerName,
            ["storeId"] = _settings.StoreId,
            ["requestId"] = requestId,
            ["amount"] = safeAmount,
            ["orderId"] = orderId,
            ["orderInfo"] = safeOrderInfo,
            ["redirectUrl"] = redirectUrl,
            ["ipnUrl"] = ipnUrl,
            ["lang"] = "vi",
            ["extraData"] = extraData,
            ["requestType"] = requestType,
            ["signature"] = signature
        };

        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false
            });
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_settings.Endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("MoMo create payment response: {Response}", responseBody);

            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            var resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
            var payUrl = root.TryGetProperty("payUrl", out var payUrlEl) ? payUrlEl.GetString() ?? string.Empty : string.Empty;
            var qrData = root.TryGetProperty("qrCodeUrl", out var qr) ? qr.GetString() ?? string.Empty : string.Empty;
            var message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? string.Empty : "Không có phản hồi từ MoMo";

            if (resultCode != 0)
            {
                _logger.LogWarning("MoMo API lỗi {Code}: {Message} — dùng QR dự phòng", resultCode, message);
                var fallback = BuildFallbackResult(orderId, requestId, safeAmount, message);
                fallback.PayUrl = payUrl;
                return fallback;
            }

            var qrPayload = !string.IsNullOrWhiteSpace(qrData) ? qrData : payUrl;

            return new MomoCreatePaymentResult
            {
                OrderId = orderId,
                RequestId = requestId,
                Amount = safeAmount,
                ResultCode = resultCode,
                Message = message,
                PayUrl = payUrl,
                QrCodeUrl = qrPayload,
                QrImageUrl = BuildQrImageUrl(qrPayload),
                Success = true,
                UseFallbackQr = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MoMo create payment failed for order {OrderId}", orderId);
            return BuildFallbackResult(orderId, requestId, safeAmount, ex.Message);
        }
    }

    public MomoCreatePaymentResult BuildFallbackResult(string orderId, string requestId, long amount, string message)
    {
        var payload = BuildFallbackQrPayload(orderId, amount);
        return new MomoCreatePaymentResult
        {
            Success = true,
            UseFallbackQr = true,
            OrderId = orderId,
            RequestId = requestId,
            Amount = amount,
            ResultCode = 0,
            Message = message,
            QrCodeUrl = payload,
            QrImageUrl = BuildQrImageUrl(payload)
        };
    }

    public static string BuildFallbackQrPayload(string orderId, long amount)
    {
        return $"MOMO|{orderId}|{amount}|FoodFast";
    }

    /// <summary>QR MoMo thật từ API bắt đầu bằng chuỗi EMV 000201...</summary>
    public static bool IsRealMomoQrPayload(string payload)
    {
        return !string.IsNullOrWhiteSpace(payload) && payload.StartsWith("000201", StringComparison.Ordinal);
    }

    /// <summary>
    /// MoMo trả về qrCodeUrl là chuỗi EMV QR (không phải URL ảnh) — chuyển thành URL ảnh QR.
    /// </summary>
    public static string BuildQrImageUrl(string qrPayload)
    {
        if (string.IsNullOrWhiteSpace(qrPayload))
        {
            return string.Empty;
        }

        return $"https://api.qrserver.com/v1/create-qr-code/?size=280x280&margin=10&data={Uri.EscapeDataString(qrPayload)}";
    }

    public bool VerifyPaymentAsync(MomoIpnRequest request)
    {
        var rawSignature =
            $"accessKey={_settings.AccessKey}" +
            $"&amount={request.Amount}" +
            $"&extraData={request.ExtraData ?? string.Empty}" +
            $"&message={request.Message}" +
            $"&orderId={request.OrderId}" +
            $"&orderInfo={request.OrderInfo}" +
            $"&orderType={request.OrderType}" +
            $"&partnerCode={request.PartnerCode}" +
            $"&payType={request.PayType}" +
            $"&requestId={request.RequestId}" +
            $"&responseTime={request.ResponseTime}" +
            $"&resultCode={request.ResultCode}" +
            $"&transId={request.TransId}";

        var expected = Sign(rawSignature);
        return string.Equals(expected, request.Signature, StringComparison.OrdinalIgnoreCase);
    }

    public MomoIpnProcessResult ProcessIpnAsync(MomoIpnRequest request)
    {
        if (!VerifyPaymentAsync(request))
        {
            return new MomoIpnProcessResult
            {
                IsValid = false,
                OrderId = request.OrderId,
                Message = "Chữ ký IPN không hợp lệ"
            };
        }

        return new MomoIpnProcessResult
        {
            IsValid = true,
            IsPaid = request.ResultCode == 0,
            OrderId = request.OrderId,
            TransactionId = request.TransId.ToString(),
            Message = request.Message
        };
    }

    public static string BuildCreateSignature(
        string accessKey,
        long amount,
        string extraData,
        string ipnUrl,
        string orderId,
        string orderInfo,
        string partnerCode,
        string redirectUrl,
        string requestId,
        string requestType)
    {
        return
            $"accessKey={accessKey}" +
            $"&amount={amount}" +
            $"&extraData={extraData}" +
            $"&ipnUrl={ipnUrl}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&partnerCode={partnerCode}" +
            $"&redirectUrl={redirectUrl}" +
            $"&requestId={requestId}" +
            $"&requestType={requestType}";
    }

    private string Sign(string rawData)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
