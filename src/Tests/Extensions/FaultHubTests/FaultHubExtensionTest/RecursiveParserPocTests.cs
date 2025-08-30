using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest {
    public record PocNode(string Name, IReadOnlyList<PocNode> Children, Exception Cause = null);
    public class PocException(string name, Exception inner = null) : Exception(name, inner);

    [TestFixture]
    public class RecursiveParserPocTests {
        private static PocNode Parse(Exception ex) {
            PocNode Union(IEnumerable<PocNode> source) {
                var nodes = source?.Where(n => n != null).ToList();
                if (nodes == null || !nodes.Any()) return null;
                if (nodes.Count == 1) return nodes.Single();
                return new PocNode("Multiple Operations", nodes, null);
            }

            if (ex is AggregateException aggEx) {
                return Union(aggEx.InnerExceptions.Select(Parse));
            }

            if (ex is not PocException pocEx) {
                return null;
            }

            var children = new List<PocNode>();
            var childNode = Parse(pocEx.InnerException);
            if (childNode != null) {
                if (childNode.Name == "Multiple Operations") {
                    children.AddRange(childNode.Children);
                } else {
                    children.Add(childNode);
                }
            }
            
            var rootCause = (pocEx.InnerException is null or PocException or AggregateException) ? null : pocEx.InnerException;

            return new PocNode(pocEx.Message, children, rootCause);
        }

        [Test]
        public void Poc_Parser_Correctly_Builds_Branched_Tree() {
            var upcomingEx = new InvalidOperationException("Upcoming");
            var pocUpcoming = new PocException("When Upcoming Urls", new PocException("Web Site Urls", upcomingEx));

            var startParsingEx = new InvalidOperationException("StartParsing");
            var pocStartParsing = new PocException("Project Parse Transaction", new PocException("Start Parsing", startParsingEx));
            var pocParseProjects = new PocException("Parse Upcoming Projects", pocStartParsing);

            var aggEx = new AggregateException(pocUpcoming, pocParseProjects);
            var pocRoot = new PocException("Parse Up Coming", aggEx);

            var result = Parse(pocRoot);

            result.Name.ShouldBe("Parse Up Coming");
            result.Children.Count.ShouldBe(2);

            var upcomingBranch = result.Children.Single(c => c.Name == "When Upcoming Urls");
            var upcomingLeaf = upcomingBranch.Children.Single();
            upcomingLeaf.Name.ShouldBe("Web Site Urls");
            upcomingLeaf.Cause.ShouldBe(upcomingEx);

            var startParsingBranch = result.Children.Single(c => c.Name == "Parse Upcoming Projects");
            var startParsingLeaf = startParsingBranch.Children.Single().Children.Single();
            startParsingLeaf.Name.ShouldBe("Start Parsing");
            startParsingLeaf.Cause.ShouldBe(startParsingEx);
        }
    }
}