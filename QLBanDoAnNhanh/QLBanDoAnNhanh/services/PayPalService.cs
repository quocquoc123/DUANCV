using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace QLBanDoAnNhanh.Services
{
    public class PayPalService
    {
        private readonly PayPalHttpClient _client;

        public PayPalService(IConfiguration config)
        {
            var environment = new SandboxEnvironment(
                config["PayPal:ClientId"],
                config["PayPal:ClientSecret"]
            );
            _client = new PayPalHttpClient(environment);
        }

        public async Task<string> CreateOrderAsync(decimal amount, string currency)
        {
            var orderRequest = new OrderRequest
            {
                CheckoutPaymentIntent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        AmountWithBreakdown = new AmountWithBreakdown
                        {
                            CurrencyCode = currency,
                            Value = amount.ToString("F2")
                        }
                    }
                },
                ApplicationContext = new ApplicationContext
                {
                    ReturnUrl = "http://localhost:5074/",  // URL PayPal sẽ chuyển hướng sau khi thanh toán thành công
                    CancelUrl = "http://localhost:5074/"  // URL nếu thanh toán bị huỷ
                }
            };

            var request = new OrdersCreateRequest();
            request.Prefer("return=representation");
            request.RequestBody(orderRequest);

            var response = await _client.Execute(request);

            // Kiểm tra nếu có lỗi trong phản hồi từ PayPal
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception("Error creating order. Response code: " + response.StatusCode);
            }

            // Truy xuất liên kết trả về và tìm liên kết có rel='approve'
            var result = response.Result<Order>();
            var approvalLink = result.Links.FirstOrDefault(link => link.Rel.Equals("approve", StringComparison.OrdinalIgnoreCase))?.Href;

            return approvalLink; // Trả về link duyệt thanh toán
        }

        public async Task<Order> CaptureOrderAsync(string token)
        {
            var request = new OrdersCaptureRequest(token); // Sử dụng token để capture đơn hàng

            var response = await _client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Error capturing payment. Response code: " + response.StatusCode);
            }

            var capturedOrder = response.Result<Order>();
            return capturedOrder;
        }

        public PayPalHttpClient GetClient()
        {
            return _client;
        }
    }
}
