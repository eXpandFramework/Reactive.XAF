using System;
using System.Linq;
using System.Net.Mail;

namespace Xpand.Extensions.Exception{
    public static partial class ExceptionExtensions{
        public static MailMessage ToMailMessage(this System.Exception exception, string email){
            return new MailMessage{
                From = new MailAddress(email), To = {email}, Body = exception.GetAllInfo(),
                Subject =
                    $"{exception.GetType().Name}:{exception.Message.Split(Environment.NewLine.ToCharArray()).First()}"
            };
        }
    }
}