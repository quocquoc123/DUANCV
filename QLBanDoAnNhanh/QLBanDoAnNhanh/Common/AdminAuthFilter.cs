using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QLBanDoAnNhanh.Common
{
    /// <summary>
    /// Action filter kiểm tra phân quyền Admin dựa trên Session.
    /// Dùng khoá session "adminLogin" (nhất quán với UserController).
    /// Áp dụng bằng attribute [AdminAuth] trên Controller hoặc Action.
    /// </summary>
    public class AdminAuthFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var adminLogin = context.HttpContext.Session.GetString("adminLogin");

            if (string.IsNullOrEmpty(adminLogin))
            {
                // Chưa đăng nhập hoặc không phải Admin → chuyển về trang login
                context.Result = new RedirectToActionResult("Index", "LoginUser", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
