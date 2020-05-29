using System;
using System.Linq;
using System.Net.Mail;

namespace Xpand.Extensions.ExceptionExtensions{
    public static partial class ExceptionExtensions{
        public static MailMessage ToMailMessage(this Exception exception, string email) => new MailMessage{
                From = new MailAddress(email), To = {email}, Body = exception.GetAllInfo(),
                Subject =
                    $"{exception.GetType().Name}:{exception.Message.Split(Environment.NewLine.ToCharArray()).First()}"
            };
    }
}