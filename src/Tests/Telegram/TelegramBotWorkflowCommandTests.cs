using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Moq;
using NUnit.Framework;
using Shouldly;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Channels;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Telegram.BusinessObjects;
using Xpand.XAF.Modules.Telegram.Services;
using Xpand.XAF.Modules.Telegram.Tests.Common;
using Xpand.XAF.Modules.Workflow.BusinessObjects;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;
using Xpand.XAF.Modules.Workflow.Services;
using TelegramBotClient = Xpand.XAF.Modules.Telegram.Services.TelegramBotClient;
using TransactionAbortedException = Xpand.Extensions.Reactive.Relay.Transaction.TransactionAbortedException;

namespace Xpand.XAF.Modules.Telegram.Tests{
    public class TelegramBotWorkflowCommandTests:BaseTelegramTest {

        [Test][Apartment(ApartmentState.STA)]
        public async Task Command_is_available_in_the_new_object_action() {
            await using var application = NewApplication();
            TelegramModule(application);
            
            await application.StartWinTest(frame => frame.Application.Navigate(typeof(CommandSuite)).Take(1)
                    .Do(frame1 => frame1.GetController<NewObjectViewController>().NewObjectAction.DoExecute(typeof(CommandSuite))).IgnoreElements()
                    .MergeToUnit(frame.Application.WhenFrame(typeof(WorkflowCommand),ViewType.ListView).Take(1)
                        .SelectMany(frame1 => frame1.GetController<NewObjectViewController>().NewObjectAction.Items
                            .Select(item => item.Data).OfType<Type>().Where(type => type==typeof(TelegramBotWorkflowCommand)))))
                .Take(1);
        }

        [Test][Apartment(ApartmentState.STA)]
        public async Task Successful_Message_Transmission() {
            await using var application = NewApplication();
            var payloadSubject = new ReplaySubject<object>();
            const string messageToSend = "Hello from Workflow";
            long botOid = -1;
            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var bot = space.CreateObject<TelegramBot>();
                    bot.Name = "TestBot";
                    bot.Active = true;
                    botOid = bot.Oid;

                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.CommandSuite = suite;

                    var telegramCommand = space.CreateObject<TelegramBotWorkflowCommand>();
                    telegramCommand.TelegramBot = bot;
                    telegramCommand.StartAction = triggerCommand;
                    telegramCommand.CommandSuite = suite;
                    
                    return suite.Commit();
                }))
                .MergeToUnit(AppDomain.CurrentDomain.HandleRequest()
                    .With<SendBotTextPayload, IObservable<Message>>(payload => {
                        payloadSubject.OnNext(payload);
                        payloadSubject.OnCompleted();
                        return Observable.Empty<Message>().Observe();
                    }))
                .Subscribe();
            TelegramModule(application);
            
            await application.StartWinTest(_ => payloadSubject).FirstOrDefaultAsync();

            var result = await payloadSubject.FirstAsync();
            var payload = result.ShouldBeOfType<SendBotTextPayload>();
            
            payload.BotId.ShouldBe(botOid);
            payload.Messages.Length.ShouldBe(1);
            payload.Messages.Single().ShouldBe(messageToSend);
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Fails_Gracefully_When_Bot_Is_Inactive() {
            await using var application = NewApplication();
    
            var payloadCount = 0;
            var triggerExecuted = new ReplaySubject<Unit>();

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
            
                    var bot = space.CreateObject<TelegramBot>();
                    bot.Name = "TestBot";
                    bot.Active = false;

                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.CommandSuite = suite;

                    var telegramCommand = space.CreateObject<TelegramBotWorkflowCommand>();
                    telegramCommand.TelegramBot = bot;
                    telegramCommand.StartAction = triggerCommand;
                    telegramCommand.CommandSuite = suite;
            
                    return suite.Commit().To(triggerCommand);
                }))
                .SelectMany(triggerCommand => triggerCommand.WhenExecuted().Take(1)
                    .Do(_ => {
                        triggerExecuted.OnNext(Unit.Default);
                        triggerExecuted.OnCompleted();
                    })
                    .ToUnit()
                )
                .MergeToUnit(AppDomain.CurrentDomain.HandleRequest()
                    .With<SendBotTextPayload, IObservable<Message>>(_ => {
                        payloadCount++;
                        return Observable.Empty<Message>().Observe();
                    }))
                .Subscribe();
            TelegramModule(application);
    
            await application.StartWinTest(_ => triggerExecuted, 10.ToSeconds());

            payloadCount.ShouldBe(0);
            BusEvents.Count.ShouldBe(0);
            
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Fails_Gracefully_When_Bot_Secret_Is_Invalid() {
            await using var application = NewApplication();
    
            var triggerExecuted = new ReplaySubject<Unit>();
            TelegramBotService.ClientFactory = _ => Observable.Throw<TelegramBotClient>(new ApiRequestException("Unauthorized", 401));

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
            
                    var bot = space.CreateObject<TelegramBot>();
                    bot.Name = "TestBot";
                    bot.Active = true;
                    bot.Secret = "InvalidSecret";

                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.CommandSuite = suite;

                    var telegramCommand = space.CreateObject<TelegramBotWorkflowCommand>();
                    telegramCommand.TelegramBot = bot;
                    telegramCommand.StartAction = triggerCommand;
                    telegramCommand.CommandSuite = suite;
            
                    return suite.Commit().To(triggerCommand);
                }))
                .SelectMany(triggerCommand => triggerCommand.WhenExecuted().Take(1)
                    .Do(_ => {
                        triggerExecuted.OnNext(Unit.Default);
                        triggerExecuted.OnCompleted();
                    })
                    .ToUnit()
                )
                .Subscribe();
            TelegramModule(application);
    
            await application.StartWinTest(_ => triggerExecuted, 10.Seconds());

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.FindRootCauses().Single().ShouldBeOfType<ApiRequestException>().Message.ShouldBe("Unauthorized");
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Handles_Empty_Input_From_Previous_Command() {
            await using var application = NewApplication();
    
            var payloadCount = 0;
            var triggerExecuted = new ReplaySubject<Unit>();

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
            
                    var bot = space.CreateObject<TelegramBot>();
                    bot.Name = "TestBot";
                    bot.Active = true;
                    
                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.ReturnEmpty = true;
                    triggerCommand.CommandSuite = suite;

                    var telegramCommand = space.CreateObject<TelegramBotWorkflowCommand>();
                    telegramCommand.TelegramBot = bot;
                    telegramCommand.StartAction = triggerCommand;
                    telegramCommand.CommandSuite = suite;
            
                    return suite.Commit().To(triggerCommand);
                }))
                .SelectMany(triggerCommand => triggerCommand.WhenExecuted().Take(1)
                    .Do(_ => {
                        triggerExecuted.OnNext(Unit.Default);
                        triggerExecuted.OnCompleted();
                    })
                    .ToUnit()
                )
                .MergeToUnit(AppDomain.CurrentDomain.HandleRequest()
                    .With<SendBotTextPayload, IObservable<Message>>(_ => {
                        Interlocked.Increment(ref payloadCount);
                        return Observable.Empty<Message>().Observe();
                    }))
                .Subscribe();
            TelegramModule(application);
    
            await application.StartWinTest(_ => triggerExecuted, 10.Seconds());

            payloadCount.ShouldBe(0);
            BusEvents.Count.ShouldBe(0);
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Sends_Multiple_Messages_From_Multiple_Input_Objects() {
            await using var application = NewApplication();
    
            var messagesSent = new ReplaySubject<string>();
            var expectedMessages="Message 1;Message 2";

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
            
                    var bot = space.CreateObject<TelegramBot>();
                    bot.Name = "TestBot";
                    bot.Active = true;

                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.OutputMessages = expectedMessages;
                    triggerCommand.CommandSuite = suite;

                    var telegramCommand = space.CreateObject<TelegramBotWorkflowCommand>();
                    telegramCommand.TelegramBot = bot;
                    telegramCommand.StartAction = triggerCommand;
                    telegramCommand.CommandSuite = suite;
            
                    return suite.Commit();
                }))
                .MergeToUnit(AppDomain.CurrentDomain.HandleRequest()
                    .With<SendBotTextPayload, IObservable<Message>>(payload => {
                        messagesSent.OnNext(payload.Messages.Join(";"));
                        messagesSent.OnCompleted();
                        return Observable.Empty<Message>().Observe();
                    }))
                .Subscribe();
            TelegramModule(application);
    
            await application.StartWinTest(_ => messagesSent, 10.Seconds()).FirstOrDefaultAsync();

            var sent = await messagesSent.FirstAsync();
            sent.ShouldBe(expectedMessages);
            BusEvents.Count.ShouldBe(0);
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Handles_Network_Failure_During_Transmission() {
            await using var application = NewApplication();
    
            var triggerExecuted = new ReplaySubject<Unit>();

            application.WhenSetupComplete()
                .SelectMany(_ => AppDomain.CurrentDomain.HandleRequest()
                    .With<SendBotTextPayload, IObservable<Message>>(_ =>
                        Observable.Throw<Message>(new HttpRequestException("Network unavailable")).Observe()))
                .Subscribe();
                
            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
            
                    var bot = space.CreateObject<TelegramBot>();
                    bot.Name = "TestBot";
                    bot.Active = true;

                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.CommandSuite = suite;

                    var telegramCommand = space.CreateObject<TelegramBotWorkflowCommand>();
                    telegramCommand.TelegramBot = bot;
                    telegramCommand.StartAction = triggerCommand;
                    telegramCommand.CommandSuite = suite;
            
                    return suite.Commit().To(triggerCommand);
                })
                )
                .SelectMany(triggerCommand => triggerCommand.WhenExecuted().Take(1)
                    .Do(_ => {
                        triggerExecuted.OnNext(Unit.Default);
                        triggerExecuted.OnCompleted();
                    })
                    .ToUnit()
                )
                
                .Subscribe();
            TelegramModule(application);
    
            await application.StartWinTest(_ => triggerExecuted);
            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            fault.FindRootCauses().Single().ShouldBeOfType<HttpRequestException>().Message.ShouldBe("Network unavailable");
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Does_Not_Execute_If_TelegramBot_Is_Null() {
            await using var application = NewApplication();
    
            var payloadCount = 0;
            var triggerExecuted = new ReplaySubject<Unit>();

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
            
                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.CommandSuite = suite;

                    var telegramCommand = space.CreateObject<TelegramBotWorkflowCommand>();
                    telegramCommand.TelegramBot = null;
                    telegramCommand.StartAction = triggerCommand;
                    telegramCommand.CommandSuite = suite;
            
                    return suite.Commit().To(triggerCommand);
                }))
                .SelectMany(triggerCommand => triggerCommand.WhenExecuted().Take(1)
                    .Do(_ => {
                        triggerExecuted.OnNext(Unit.Default);
                        triggerExecuted.OnCompleted();
                    })
                    .ToUnit()
                )
                .MergeToUnit(AppDomain.CurrentDomain.HandleRequest()
                    .With<SendBotTextPayload, IObservable<Message>>(_ => {
                        Interlocked.Increment(ref payloadCount);
                        return Observable.Empty<Message>().Observe();
                    }))
                .Subscribe();
            TelegramModule(application);
    
            await application.StartWinTest(_ => BusEvents.ToNowObservable(), 10.Seconds()).FirstOrDefaultAsync();

            payloadCount.ShouldBe(0);
            BusEvents.Count.ShouldBe(1);
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Sends_To_Multiple_Active_Chats_And_Tolerates_Single_Chat_Failure() {
            await using var application = NewApplication();
            
            var successfulMessages = new List<string>();
            var disabledPayloads = new List<SendChatDisabledPayload>();
            var triggerExecuted = new ReplaySubject<Unit>();
            long failingChatId = 123;
            long successChatId = 456;

            TelegramBotService.ClientFactory = _ => {
                var mockClient = new Mock<ITelegramBotClient>();

                mockClient.Setup(client => client.SendRequest(It.IsAny<SetMyCommandsRequest>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(true));

                mockClient.Setup(client => client.SendRequest(It.Is<SendMessageRequest>(req => req.ChatId.Identifier == failingChatId), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromException<Message>(new ApiRequestException("Forbidden: bot was blocked by the user", 403)));

                mockClient.Setup(client => client.SendRequest(It.Is<SendMessageRequest>(req => req.ChatId.Identifier == successChatId), It.IsAny<CancellationToken>()))
                    .Callback<IRequest<Message>, CancellationToken>((req, _) => {
                        if (req is SendMessageRequest sendMessageRequest) {
                            successfulMessages.Add(sendMessageRequest.Text);
                        }
                    })
                    .Returns(Task.FromResult(new Message()));
            
                return mockClient.Object.Observe();
            };

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    
                    var bot = space.CreateObject<TelegramBot>();
                    bot.Name = "TestBot";
                    bot.Active = true;
                    bot.Secret = "ASecret";

                    var failingChat = space.CreateObject<TelegramChat>();
                    failingChat.Id = failingChatId;
                    failingChat.Active = true;
                    bot.Chats.Add(failingChat);

                    var successChat = space.CreateObject<TelegramChat>();
                    successChat.Id = successChatId;
                    successChat.Active = true;
                    bot.Chats.Add(successChat);

                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.CommandSuite = suite;

                    var telegramCommand = space.CreateObject<TelegramBotWorkflowCommand>();
                    telegramCommand.TelegramBot = bot;
                    telegramCommand.StartAction = triggerCommand;
                    telegramCommand.CommandSuite = suite;
                    
                    return suite.Commit().To(triggerCommand);
                }))
                .SelectMany(triggerCommand => triggerCommand.WhenExecuted().Take(1)
                    .Do(_ => {
                        triggerExecuted.OnNext(Unit.Default);
                        triggerExecuted.OnCompleted();
                    })
                    .ToUnit()
                )
                .MergeToUnit(AppDomain.CurrentDomain.HandleRequest()
                    .With<SendChatDisabledPayload, TelegramChat>(payload => {
                        disabledPayloads.Add(payload);
                        return Observable.Empty<TelegramChat>();
                    }))
                .Subscribe();
            TelegramModule(application);
            
            await application.StartWinTest(_ => triggerExecuted);

            successfulMessages.Count.ShouldBe(1);
            
            disabledPayloads.Count.ShouldBe(1);
            disabledPayloads.Single().ChatId.ShouldBe(failingChatId);
            
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().FindRootCauses().Single().ShouldBeOfType<ApiRequestException>();    
        }
    }
}