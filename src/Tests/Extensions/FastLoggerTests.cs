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
            _originalWriter = FastLogger.Write;
            FastLogger.Write = message => _logOutput.Add(message);
            FastLogger.Enabled = true;
        }

        [TearDown]
        public void TearDown() {
            FastLogger.Write = _originalWriter;
            FastLogger.Enabled = false;
        }

        private void TestLogMethod() {
            var message = "Test message";
            FastLogger.LogFast($"{message}");
            
            _logOutput.ShouldHaveSingleItem();
//MODIFICATION: START
            var expected = $"FastLoggerTests - {nameof(TestLogMethod)} | {message}";
//MODIFICATION: END
            _logOutput.Single().ShouldBe(expected);
        }

        [Test]
        public void LogFast_Includes_Caller_Info_When_Enabled() {
            TestLogMethod();
        }

        [Test]
        public void LogFast_Does_Nothing_When_Disabled() {
            FastLogger.Enabled = false;
            var message = "This should not be logged";
            
            FastLogger.LogFast($"{message}");
            
            _logOutput.ShouldBeEmpty();
        }

        [Test]
        public void LogError_Includes_Caller_Info_And_Color_Codes() {
            var message = "Error message";
            
            FastLogger.LogError($"{message}");
            
            _logOutput.ShouldHaveSingleItem();
            var output = _logOutput.Single();
            
            output.ShouldStartWith("\x1b[91m"); // Red
            output.ShouldEndWith("\x1b[0m");   // Reset
            output.ShouldContain($" - {nameof(LogError_Includes_Caller_Info_And_Color_Codes)} | {message}");
        }

        [Test]
        public void LogWarning_Includes_Caller_Info_And_Color_Codes() {
            var message = "Warning message";
            
            FastLogger.LogWarning($"{message}");
            
            _logOutput.ShouldHaveSingleItem();
            var output = _logOutput.Single();
            
            output.ShouldStartWith("\x1b[33m"); // DarkYellow
            output.ShouldEndWith("\x1b[0m");   // Reset
            output.ShouldContain($" - {nameof(LogWarning_Includes_Caller_Info_And_Color_Codes)} | {message}");
        }

        [Test]
        public void Logging_Is_Suppressed_When_Message_Starts_With_Bracket() {
            var message = "[DIAGNOSTIC] Special message";
            
            FastLogger.LogFast($"{message}");
            FastLogger.LogError($"{message}");
            FastLogger.LogWarning($"{message}");
            
            _logOutput.ShouldBeEmpty("Logging should be suppressed for messages starting with '['.");
        }
    }
}