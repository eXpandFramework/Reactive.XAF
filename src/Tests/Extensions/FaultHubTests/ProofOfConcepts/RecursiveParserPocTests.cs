using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts {
    public record PocNode(string Name, IReadOnlyList<PocNode> Children, Exception Cause = null);
    public class PocException(string name, Exception inner = null) : Exception(name, inner);
    [TestFixture]
    public class RecursiveParserPocTests {
        private static PocNode Parse(Exception ex) {
            PocNode Union(IEnumerable<PocNode> source) {
                var nodes = source?.Where(n => n != null).ToList();
                if (nodes == null || !nodes.Any()) return null;
                if (nodes.Count == 1) return nodes.Single();
                return new PocNode("Multiple Operations", nodes);
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
            
            var rootCause = (pocEx.InnerException is null or PocException or AggregateException) ?
                null : pocEx.InnerException;

            return new PocNode(pocEx.Message, children, rootCause);
        }

        [Test]
        public void Poc_Parser_Correctly_Builds_Branched_Tree() {
            var urlsEx = new InvalidOperationException("Failed to fetch URLs");
            var pocGetLinks = new PocException("Get Page Links", new PocException("Fetch Initial Urls", urlsEx));

            var extractEx = new InvalidOperationException("Failed to extract content");
            var pocExtract = new PocException("Data Extraction Transaction", new PocException("Extract Content", extractEx));
            var pocScrapeData = new PocException("Scrape Data From Links", pocExtract);
            var aggEx = new AggregateException(pocGetLinks, pocScrapeData);
            var pocRoot = new PocException("Extract And Process Links", aggEx);

            var result = Parse(pocRoot);
            result.Name.ShouldBe("Extract And Process Links");
            result.Children.Count.ShouldBe(2);

            var getLinksBranch = result.Children.Single(c => c.Name == "Get Page Links");
            var getLinksLeaf = getLinksBranch.Children.Single();
            getLinksLeaf.Name.ShouldBe("Fetch Initial Urls");
            getLinksLeaf.Cause.ShouldBe(urlsEx);

            var scrapeDataBranch = result.Children.Single(c => c.Name == "Scrape Data From Links");
            var extractDataLeaf = scrapeDataBranch.Children.Single().Children.Single();
            extractDataLeaf.Name.ShouldBe("Extract Content");
            extractDataLeaf.Cause.ShouldBe(extractEx);
        }
    }
}