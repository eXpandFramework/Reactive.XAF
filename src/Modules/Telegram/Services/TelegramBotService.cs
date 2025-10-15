using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Templates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Relay.Transaction;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Telegram.BusinessObjects;
using Message = Telegram.Bot.Types.Message;

namespace Xpand.XAF.Modules.Telegram.Services{
    public static class TelegramBotService{
        record SendBotTextPayload(long BotId, ParseMode ParseMode, string[] Messages);
        
        public static readonly ConcurrentDictionary<TelegramBot, IObservable<TelegramBotClient>> Clients = new(new BotComparer());
        internal class BotComparer : IEqualityComparer<TelegramBot> {
            public bool Equals(TelegramBot x, TelegramBot y) 
                => ReferenceEquals(x, y) || x is not null && y is not null && x.Secret == y.Secret;

            public int GetHashCode(TelegramBot obj) 
                => obj == null ? throw new ArgumentNullException(nameof(obj)) : obj.Secret?.GetHashCode() ?? 0;
        }
        
        public static IObservable<Message> SendText(this TelegramBot bot, ParseMode parseMode, params string[] messages) 
            => bot.Session?.DataLayer.MakeRequest()
                .With<SendBotTextPayload, IObservable<Message>>(new SendBotTextPayload(bot.Oid, parseMode, messages))
                .Switch();

        internal static IObservable<Unit> TelegramBotConnect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenSetupComplete()
                    .SelectMany(_ => application.CacheClients()
                        .MergeToUnit(application.UpdateBots())
                        .MergeToUnit(application.RefreshDetailViewWhenObjectCommitted<TelegramChatMessage>(typeof(TelegramBot)))
                        .Merge(application.WhenSendBotTextPayload())
                    )
                )
                .MergeToUnit(manager.SendMessage())
                .MergeToUnit(manager.TelegramUpdate());
        
        private static IObservable<Unit> WhenSendBotTextPayload(this XafApplication application) 
            => application.ObjectSpaceProvider.HandleDataLayerRequest()
                .With<SendBotTextPayload, IObservable<Message>>(payload => 
                    application.UseProviderObjectSpace(space => space.SendTextMessagesToActiveChats( payload)
                        .BeginWorkflow($"SendTextPayload-{payload.BotId}", context: [payload])
                        .RunToEnd()
                        .SelectMany(messages => messages.ToObservable()),typeof(TelegramBot)).Observe());
        

        private static IObservable<Message> SendTextMessagesToActiveChats(this IObjectSpace space, SendBotTextPayload payload) 
            => Observable.Defer(() => space.GetObjectsQuery<TelegramBot>().Where(telegramBot => telegramBot.Oid==payload.BotId).ToArray().ToNowObservable()
                .ThrowIfEmpty(new InvalidOperationException($"Bot {payload.BotId} not found"))
                .SelectMany(bot => bot.ClientSource().SelectMany(client =>
                    bot.Chats.Where(chat => chat.Active).ToObservable()
                        .SelectMany(chat => client.SendText(chat, payload.ParseMode, payload.Messages))
                )));

        public static IObservable<Message> SendText(this TelegramBot bot,params string[] messages) 
            => bot.SendText(ParseMode.Html, messages);

        internal static IObservable<TelegramBotClient> ClientSource(this TelegramBot bot){
            Clients.TryGetValue(bot, out var clientSource);
            return clientSource?? Observable.Empty<TelegramBotClient>();
        }
        
        private static IObservable<Unit> SendMessage(this ApplicationModulesManager manager) 
            => manager.RegisterViewSimpleAction(nameof(SendMessage), action => {
                    action.TargetObjectType = typeof(TelegramBot);
                    action.SetImage(CommonImage.Send);
                    action.QuickAccess = true;
                    action.ImageName = "TelegramSendMessage";
                })
                .WhenExecuted(e => e.CreateDialogController(e.NewDetailView(typeof(TelegramMessage)),"Send")
                    .SelectMany(controller => controller.AcceptAction.WhenConcatExecution(e1 => ((TelegramBot)e.View().CurrentObject)
                        .SendText(((TelegramMessage)e1.View().CurrentObject).Text))))
                .ToUnit();

        
        static IObservable<TelegramChatMessage> UpdateBots(this XafApplication application) 
            => application.WhenProviderObjects<TelegramBot>(ObjectModification.NewOrUpdated,criteriaExpression:bot => bot.Active,modifiedProperties:[nameof(TelegramBot.Secret),nameof(TelegramBot.UpdateInterval)])
                .Select(bots => bots)
                .Merge(application.WhenFrame(typeof(TelegramBot)).SelectMany(frame => frame.SimpleAction(nameof(TelegramUpdate)).WhenExecuted().Select(e => e.SelectedObjects.Cast<TelegramBot>().ToArray())))
                .Publish(source => source.WhenUpdate(application));


        static IObservable<Unit> TelegramUpdate(this ApplicationModulesManager manager) 
            => manager.RegisterViewSimpleAction(nameof(TelegramUpdate), action => {
                    action.TargetObjectType = typeof(TelegramBot);
                    action.ImageName=nameof(TelegramUpdate);
                    action.QuickAccess = true;
                    action.PaintStyle=ActionItemPaintStyle.Image;
                    action.Caption = "Update";
                    action.SelectionDependencyType=SelectionDependencyType.RequireMultipleObjects;
                    action.SetTargetCriteria<TelegramBot>(bot => bot.Secret!=null);
                })
                .ToUnit();

        private static IObservable<TelegramChatMessage> WhenUpdate(this IObservable<TelegramBot[]> source, XafApplication application, TelegramBot bot) 
            => Observable.Using(() => new CancellationTokenSource(),tokenSource => application.UseObject(bot, telegramBot => telegramBot.Update(application, tokenSource.Token))
                .RepeatWhen(obs => obs.TakeUntil(_ => bot.UpdateInterval==TimeSpan.Zero).SelectMany(_ => bot.UpdateInterval.Timer()))
                .TakeUntil(source.Do(_ => tokenSource.Cancel())) ) ;

        
        private static IObservable<TelegramChatMessage> Update(this TelegramBot bot, XafApplication application, CancellationToken token) 
            => bot.ClientSource().SelectMany(client => client
                .WhenUpdates(bot.LastUpdate + 1, timeout: (int?)bot.UpdateInterval.TotalSeconds.Round() - 1, token: token).SelectMany()
                .SelectManyItemResilient(update => application.UseObject(bot, telegramBot => telegramBot.Observe().Do(bot1 => {
                        bot1.LastUpdate = update.Id;
                        bot1.UpdateTime = DateTime.Now;
                    })
                    .Select(message => message.EnsureTelegramChat(update))
                    .SelectMany(chat => chat == null ? Observable.Empty<TelegramChatMessage>()
                        : chat.CreateMessage(update).Commit()))));

        static IObservable<IObservable<TelegramBotClient>> CacheClients(this XafApplication application)
            => application.WhenProviderObject<TelegramBot>(ObjectModification.NewOrUpdated,bot => bot.Secret!=null&&bot.Active)
                .Select(bot => {
                    var botCommands = bot.CreateCommands().ToArray();
                    return Clients.GetOrAdd(bot, telegramBot => (ClientFactory?.Invoke(telegramBot)??new TelegramBotClient(telegramBot.Secret).Observe())
                        .SelectMany(client => client.SetMyCommands(botCommands).ToObservable().To(client)));
                });


        public static Func<TelegramBot, IObservable<TelegramBotClient>> ClientFactory{ get; set; } 
            = telegramBot => telegramBot.NewClient();
        
        static IObservable<TelegramBotClient> NewClient(this TelegramBot telegramBot) => new TelegramBotClient(telegramBot.Secret).Observe();

        private static IObservable<TelegramChatMessage> WhenUpdate(this IObservable<TelegramBot[]> source,XafApplication application) 
            => source.Select(bots => bots.WhereNotDefault(bot => bot.Secret))
                .SelectMany(bots => bots.ToObservable().SelectMany(bot => source.WhenUpdate(application, bot)));

        internal static IEnumerable<BotCommand> CreateCommands(this TelegramBot telegramBot) 
            => telegramBot.Commands
                .Select(command => {
                    var paramDescriptions = command.Parameters.OrderBy(p => p.Oid)
                        .Select(p => p.Required ? $"<{p.Name}>" : $"[{p.Name}]").ToArray();
                    var paramText = !paramDescriptions.Any() ? null : string.Join(" ", paramDescriptions);
                    var descriptionParts = new[] { command.Description, paramText }.WhereNotNullOrEmpty();
                    return new BotCommand {
                        Command = command.Name,
                        Description = descriptionParts.JoinCommaSpace()
                    };
                });
        public static IObservable<Update[]> WhenUpdates(this TelegramBotClient client, int? offset = null,
            int? limit = 100,int? timeout=60,CancellationToken token=default,params UpdateType[] updateTypes)
            => client.GetUpdates(offset,limit,timeout,updateTypes,token).ToObservable()
                .Catch<Update[],TaskCanceledException>(_ => Observable.Empty<Update[]>() );


    }
}