using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using DevExpress.ExpressApp;
using Humanizer;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Channels;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Telegram.BusinessObjects;
using Message = Telegram.Bot.Types.Message;

namespace Xpand.XAF.Modules.Telegram.Services{
    public static class TelegramChatService{
        internal record SendChatDisabledPayload(long ChatId);
        internal static IObservable<Unit> TelegramChatConnect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.UpdateChats()
                .MergeToUnit(application.WhenSetupComplete(_ => application.WhenSendChatDisabledPayload()))
                .MergeToUnit(application.WhenProviderCommitted<TelegramChatMessage>(ObjectModification.Updated).ToObjects().Where(message => message.HasReply)
                    .SelectManyItemResilient(message => message.TelegramChat.SendText($@"
> {message.Message.EncloseHTMLImportant()}

{message.Reply}
"))));

        private static IObservable<Unit> WhenSendChatDisabledPayload(this XafApplication application)
            => application.ObjectSpaceProvider.HandleDataLayerRequest()
                .With<SendChatDisabledPayload, TelegramChat>(payload => application.UseProviderObjectSpace(space => space.GetObjectsQuery<TelegramChat>()
                    .Where(chat => chat.Id == payload.ChatId).ToArray().ToNowObservable()
                    .Do(chat => chat.Active = false).Commit()));
        
        public static IObservable<Message> SendText(this TelegramChat chat,ParseMode parseMode,params string[] messages) 
            => chat.Bot.ClientSource().SelectMany(client => client.SendText(chat,parseMode, messages));

        public static IObservable<Message> SendText(this TelegramChat chat,params string[] messages) 
            => chat.SendText(ParseMode.Html,messages);
        static IObservable<Unit> UpdateChats(this XafApplication application)
            => application.WhenProviderCommittedDetailed<TelegramChat>(ObjectModification.NewOrUpdated,modifiedProperties:[nameof(TelegramChat.Active)]).ToObjects()
                .SelectMany(chat => chat.Bot.MessageTemplates
                    .ToNowObservable().Where(template => template.Type == (chat.Active ? TelegramMessageTemplateType.Start : TelegramMessageTemplateType.Stop)).Take(1)
                    .SelectManyItemResilient(template => chat.SendText(template.Text))
                    .ToUnit())
                .ToUnit();

        internal static TelegramChat EnsureTelegramChat(this TelegramBot bot, Update update){
            var chatId = update.Message?.Chat.Id;
            return chatId == null ? null
                : bot.ObjectSpace.EnsureObject<TelegramChat>(chat => chat.Id == chatId, update: chat
                    => {
                    chat.Id = update.Message!.Chat.Id;
                    chat.User = bot.ObjectSpace.EnsureTelegramUser(update, chat);
                    chat.Bot = bot;
                }, inTransaction: true);
        }
        
        private static TelegramUser EnsureTelegramUser(this IObjectSpace space, Update update, TelegramChat chat) 
            => space.EnsureObject<TelegramUser>(
                user => user.Id == update.Message.From.Id,
                user => {
                    user.Id = update.Message!.From!.Id;
                    user.Chats.Add(chat);
                }, user => {
                    user.UserName = update.Message?.From?.Username;
                    user.LastName = update.Message?.From?.LastName;
                    user.FirstName = update.Message?.From?.FirstName;
                },true);


        internal static TelegramChatMessage CreateMessage(this TelegramChat chat, Update update){
            var message = chat.ObjectSpace.CreateObject<TelegramChatMessage>();
            message.Message = update.Message!.Text;
            message.Created = update.Message!.Date;
            message.TelegramChat=chat;
            if (message.Message.RegexIsMatch(chat.Bot.StartMessageRegex)){
                if (chat.Active){
                    chat.Active=false;
                    chat.CommitChanges();
                }
                chat.Active = true;
            }
            if (message.Message.RegexIsMatch( chat.Bot.StopMessageRegex)){
                chat.Active = false;
            }
            return message;
        }

        internal static IObservable<Message> SendText(this TelegramBotClient client,  TelegramChat chat,ParseMode parseMode, string[] messages) 
            => messages.ToNowObservable().WhenNotDefaultOrEmpty()
                .SelectManyItemResilient(msg => client.SendMessage(new ChatId(chat.Id), msg,parseMode:parseMode,
                    linkPreviewOptions:new LinkPreviewOptions(){IsDisabled = chat.Bot.DisableWebPagePreview}).ToObservable()
                    .Catch<Message,ApiRequestException>(e =>e.Observe().If(_ => e.ErrorCode==403, _ => chat.Session?.DataLayer.MakeRequest()
                        .With<SendChatDisabledPayload, TelegramChat>(new SendChatDisabledPayload(chat.Id)).To<Message>(),_ => e.Throw<Message>()) ));
    
        public static IObservable<(TelegramChatMessage chatMessage, string query, string commandText)> WhenValid(this TelegramChatMessage chatMessage){
            var message = chatMessage.Message;
            if (!message.StartsWith("/")) return Observable.Empty<(TelegramChatMessage chatMessage, string query, string commandText)>();
            var query = message.Substring(1);
            var commandText = query.Split(' ').First();
            if (new[]{ "start", "stop" }.Contains(commandText)){
                return Observable.Empty<(TelegramChatMessage chatMessage, string query, string commandText)>();
            }
            var botCommand = chatMessage.TelegramChat.Bot.Commands.FirstOrDefault(command => command.Name==commandText);
            if (botCommand == null){
                return chatMessage.TelegramChat.SendText($"{commandText} is not a valid command").ToUnit()
                    .ContinueOnFault(context: [chatMessage])
                    .IgnoreElements().To<(TelegramChatMessage chatMessage, string query, string commandText)>();
            }
            var requiredParametersCount = botCommand.Parameters.Count(parameter => parameter.Required);
            return requiredParametersCount == query.Split(' ').Skip(1).TrimAll().Count() ? Unit.Default.Observe()
                    .Select(_ => (chatMessage,query,commandText))
                : chatMessage.TelegramChat.SendText($"Wrong required parameters count, expected {requiredParametersCount.ToWords()}").ToUnit()
                    .ContinueOnFault(context: [chatMessage])
                    .IgnoreElements()
                    .To<(TelegramChatMessage chatMessage, string query, string commandText)>();
        }
    }
}