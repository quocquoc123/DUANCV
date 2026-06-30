using System.Text.Json.Serialization;

namespace QLBanDoAnNhanh.Models;

public class MomoCreatePaymentResult
{
    public bool Success { get; set; }
    public int ResultCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PayUrl { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
    public bool UseFallbackQr { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public long Amount { get; set; }
}

public class MomoIpnRequest
{
    [JsonPropertyName("partnerCode")]
    public string PartnerCode { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("orderInfo")]
    public string OrderInfo { get; set; } = string.Empty;

    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = string.Empty;

    [JsonPropertyName("transId")]
    public long TransId { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("payType")]
    public string PayType { get; set; } = string.Empty;

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    [JsonPropertyName("extraData")]
    public string ExtraData { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

public class MomoIpnProcessResult
{
    public bool IsValid { get; set; }
    public bool IsPaid { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
