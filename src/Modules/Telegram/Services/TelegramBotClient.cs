using System.Net.Http;
using Fasterflect;
using Telegram.Bot;
using Xpand.Extensions.Reactive.Transform.System.Net;

namespace Xpand.XAF.Modules.Telegram.Services{
    public class TelegramBotClient:global::Telegram.Bot.TelegramBotClient{
        public TelegramBotClient(TelegramBotClientOptions options, HttpClient httpClient = null) : base(options, httpClient??NetworkExtensions.HttpClient){
        }

        public TelegramBotClient(string token, HttpClient httpClient = null) : base(token, httpClient??NetworkExtensions.HttpClient){
            
        }

        public TelegramBotClientOptions Options => (TelegramBotClientOptions)this.GetFieldValue("_options");
    }
}