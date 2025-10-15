using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class FastLoggerFilteringTests {
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

        private void MethodA() => LogFast($"Message from A");
        private void MethodB() => LogFast($"Message from B");
        private void MethodC() => LogFast($"Message from C");

        [Test]
        public void Filters_By_Member_Name() {
            using (LogFastFilter(p => p.method == nameof(MethodA))) {
                MethodA();
                MethodB();
            }

            _logOutput.ShouldHaveSingleItem();
            _logOutput.Single().ShouldContain(nameof(MethodA));
        }

        [Test]
        public void Filters_By_File_Path() {
            using (LogFastFilter(p => 
                string.Equals(Path.GetFileNameWithoutExtension(p.path), "FastLoggerFilteringTests", StringComparison.OrdinalIgnoreCase))) {
                MethodA(); 
            }
            
            _logOutput.ShouldHaveSingleItem();
            _logOutput.Single().ShouldContain("FastLoggerFilteringTests");
        }

        [Test]
        public void Multiple_Filters_In_Same_Scope_Are_Composed_With_AND() {
            using (LogFastFilter(p => p.method.StartsWith("Method")))
            using (LogFastFilter(p => p.method.EndsWith("B"))) {
                MethodA();
                MethodB();
                MethodC();
            }

            _logOutput.ShouldHaveSingleItem();
            _logOutput.Single().ShouldContain(nameof(MethodB));
        }

        [Test]
        public void Nested_Filters_Are_Applied_And_Restored_Correctly() {
            using (LogFastFilter(p => p.method == nameof(MethodA) || p.method == nameof(MethodB))) { 
                MethodA();
                MethodB();

                using (LogFastFilter(p => p.method == nameof(MethodB))) {
                    MethodA();
                    MethodB();
                }

                MethodA();
                MethodC();
            }
            
            _logOutput.Count.ShouldBe(4);
            _logOutput.Count(s => s.Contains(nameof(MethodA))).ShouldBe(2);
            _logOutput.Count(s => s.Contains(nameof(MethodB))).ShouldBe(2);
        }
    }}