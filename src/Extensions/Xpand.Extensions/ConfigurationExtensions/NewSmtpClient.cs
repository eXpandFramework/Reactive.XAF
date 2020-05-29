using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Mail;

namespace Xpand.Extensions.ConfigurationExtensions{
    public static class ConfigurationExtensions{
        public static SmtpClient NewSmtpClient(this NameValueCollection appSettings, string reportEnableSsl = "reportEnableSsl", string reportEmailPass = "reportEmailPass",
            string reportEmail = "reportEmail", string reportEmailPort = "reportEmailPort", string reportEmailserver = "reportEmailserver"){
            var smtpClient = new SmtpClient(appSettings[reportEmailserver]){
                Port = Convert.ToInt32(appSettings[reportEmailPort]),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(appSettings[reportEmail],
                    appSettings[reportEmailPass]),
                EnableSsl = Convert.ToBoolean(appSettings[reportEnableSsl])
            };
            return smtpClient;
        }

    }
}