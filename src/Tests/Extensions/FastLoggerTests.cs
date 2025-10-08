using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Tracing;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class FastLoggerTests {
        private List<string> _logOutput;
        private Action<string> _originalWriter;

        [SetUp]
        public void SetUp() {
            _logOutput = new List<string>();
            _originalWriter = Write;
            Write = message => _logOutput.Add(message);
            Enabled = true;
        }

        [TearDown]
        public void TearDown() {
            Write = _originalWriter;
            Enabled = false;
        }

        private void TestLogMethod() {
            var message = "Test message";
            LogFast($"{message}");
            
            _logOutput.ShouldHaveSingleItem();
            var expected = $"FastLoggerTests - {nameof(TestLogMethod)} | {message}";
            _logOutput.Single().ShouldBe(expected);
        }

        [Test]
        public void LogFast_Includes_Caller_Info_When_Enabled() {
            TestLogMethod();
        }

        [Test]
        public void LogFast_Does_Nothing_When_Disabled() {
            Enabled = false;
            var message = "This should not be logged";
            
            LogFast($"{message}");
            
            _logOutput.ShouldBeEmpty();
        }

        [Test]
        public void LogError_Includes_Caller_Info_And_Color_Codes() {
            var message = "Error message";
            
            LogError($"{message}");
            
            _logOutput.ShouldHaveSingleItem();
            var output = _logOutput.Single();
            
            output.ShouldStartWith("\x1b[91m");
            output.ShouldEndWith("\x1b[0m");
            output.ShouldContain($" - {nameof(LogError_Includes_Caller_Info_And_Color_Codes)} | {message}");
        }

        [Test]
        public void LogWarning_Includes_Caller_Info_And_Color_Codes() {
            var message = "Warning message";
            
            LogWarning($"{message}");
            
            _logOutput.ShouldHaveSingleItem();
            var output = _logOutput.Single();
            
            output.ShouldStartWith("\x1b[33m");
            output.ShouldEndWith("\x1b[0m");
            output.ShouldContain($" - {nameof(LogWarning_Includes_Caller_Info_And_Color_Codes)} | {message}");
        }

    
        [Test]
        public void Filter_Allows_Only_Specified_LogLevel() {
            var infoMessage = "This is an info message.";
            var warningMessage = "This is a warning message.";
            var errorMessage = "This is an error message.";

            using (LogFastFilter(log => log.level == FastLogLevel.Warning)) {
                LogFast($"{infoMessage}");
                LogWarning($"{warningMessage}");
                LogError($"{errorMessage}");
            }

            _logOutput.ShouldHaveSingleItem();
            _logOutput.Single().ShouldContain(warningMessage);
        }

        [Test]
        public void Filter_Can_Allow_Multiple_LogLevels() {
            var infoMessage = "This is an info message.";
            var warningMessage = "This is a warning message.";
            var errorMessage = "This is an error message.";

            using (LogFastFilter(log => log.level == FastLogLevel.Error || log.level == FastLogLevel.Info)) {
                LogFast($"{infoMessage}");
                LogWarning($"{warningMessage}");
                LogError($"{errorMessage}");
            }

            _logOutput.Count.ShouldBe(2);
            _logOutput.ShouldContain(item => item.Contains(infoMessage));
            _logOutput.ShouldContain(item => item.Contains(errorMessage));
            _logOutput.ShouldNotContain(item => item.Contains(warningMessage));
        }
        
    }
}