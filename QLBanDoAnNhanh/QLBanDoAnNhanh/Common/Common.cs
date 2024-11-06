using System.Net.Mail;
using System.Net;

namespace QLBanDoAnNhanh.Common
{
    public class Common
    {
        private static string email;
        private static string password;

        // Constructor để khởi tạo cấu hình từ IConfiguration
        public Common(IConfiguration configuration)
        {
            email = configuration["EmailSettings:Email"];
            password = configuration["EmailSettings:PasswordEmail"];
        }
        // Xóa thuộc tính Common để tránh xung đột tên
        // public static object Common { get; internal set; }

        public static bool SendMail(string name, string subject, string content, string toMail)
        {
            bool rs = false;
            try
            {
                MailMessage message = new MailMessage();
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com", // host name
                    Port = 587, // port number
                    EnableSsl = true, // whether your smtp server requires SSL
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential()
                    {
                        UserName = email,
                        Password = password
                    }
                };

                MailAddress fromAddress = new MailAddress(email, name);
                message.From = fromAddress;
                message.To.Add(toMail);
                message.Subject = subject;
                message.IsBodyHtml = true;
                message.Body = content;
                smtp.Send(message);
                rs = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                rs = false;
            }
            return rs;
        }

        public static string FormatNumber(object value, int SoSauDauPhay = 2)
        {
            bool isNumber = IsNumeric(value);
            decimal GT = 0;
            if (isNumber)
            {
                GT = Convert.ToDecimal(value);
            }
            string str = "";
            string thapPhan = "";
            for (int i = 0; i < SoSauDauPhay; i++)
            {
                thapPhan += "#";
            }
            if (thapPhan.Length > 0) thapPhan = "." + thapPhan;
            string snumformat = string.Format("0:#,##0{0}", thapPhan);
            str = String.Format("{" + snumformat + "}", GT);

            return str;
        }

        private static bool IsNumeric(object value)
        {
            return value is sbyte
                       || value is byte
                       || value is short
                       || value is ushort
                       || value is int
                       || value is uint
                       || value is long
                       || value is ulong
                       || value is float
|| value is double
                       || value is decimal;
        }

        public static string HtmlRate(int rate)
        {
            var str = "";
            if (rate == 1)
            {
                str = @"<li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>";
            }
            if (rate == 2)
            {
                str = @"<li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>";
            }
            if (rate == 3)
            {
                str = @"<li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>";
            }
            if (rate == 4)
            {
                str = @"<li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star-o' aria-hidden='true'></i></li>";
            }
            if (rate == 5)
            {
                str = @"<li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>
                    <li><i class='fa fa-star' aria-hidden='true'></i></li>";
            }
            return str;
        }
    }
}