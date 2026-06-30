namespace QLBanDoAnNhanh.Models;

public class MomoSettings
{
    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string RequestType { get; set; } = "captureWallet";
    public string PartnerName { get; set; } = "Food Fast";
    public string StoreId { get; set; } = "FoodFastStore";
    public int PaymentTimeoutMinutes { get; set; } = 15;
}
