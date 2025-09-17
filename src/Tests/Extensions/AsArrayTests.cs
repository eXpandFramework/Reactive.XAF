using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ObjectExtensions;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests;
[TestFixture]
public class AsArrayTests:BaseTest {
    [Test]
    [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
    public void AsArray_WithNullInput_ReturnsEmptyArray() {
        object input = null;
        var result = input!.AsArray<int>();
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Test]
    public void AsArray_WithPreTypedArray_ReturnsSameArray() {
        var input = new[] { "a", "b" };
        var result = input.AsArray<string>();
        result.ShouldBeSameAs(input);
    }

    [Test]
    public void AsArray_WithListOfIntsAndTCurrentObject_ReturnsObjectArrayOfInts() {
        var input = new List<int> { 1, 2, 3 };
        var result = input.AsArray<object>();
        result.ShouldBe([1, 2, 3]);
    }

    [Test]
    public void AsArray_WithStringAndTCurrentObject_ReturnsObjectArrayOfChars() {
        string input = "abc";
        var result = input.AsArray<object>();
        result.ShouldBe(['a', 'b', 'c']);
    }

    [Test]
    public void AsArray_WithListOfIntsAndTCurrentIsList_ReturnsArrayContainingTheSingleList() {
        var input = new List<int> { 1, 2, 3 };
        var result = input.AsArray<List<int>>();
        result.Length.ShouldBe(1);
        result[0].ShouldBeSameAs(input);
    }

    [Test]
    public void AsArray_WithListOfIntsAndTCurrentIsInt_ReturnsIntArray() {
        var input = new List<int> { 1, 2, 3 };
        var result = input.AsArray<int>();
        result.ShouldBe([1, 2, 3]);
    }

    [Test]
    public void AsArray_WithStringAndTCurrentIsChar_ReturnsCharArray() {
        string input = "abc";
        var result = input.AsArray<char>();
        result.ShouldBe(['a', 'b', 'c']);
    }

    [Test]
    public void AsArray_WithSingleInt_ReturnsIntArray() {
        object input = 42;
        var result = input.AsArray<int>();
        result.ShouldBe([42]);
    }

    [Test]
    public void AsArray_WithSingleString_ReturnsStringArray() {
        object input = "test";
        var result = input.AsArray<string>();
        result.ShouldBe(["test"]);
    }

    [Test]
    public void AsArray_FromObjectListWithDirectlyCastableItems_ReturnsTCurrentArray() {
        var input = new object[] { 1, 2, 3 };
        var result = input.AsArray<int>();
        result.ShouldBe([1, 2, 3]);
    }

    [Test]
    public void AsArray_FromObjectListWithNestedEnumerables_ReturnsFlattenedTCurrentArray() {
        var input = new object[] { new List<int> { 1, 2 }, new[] { 3, 4 } };
        var result = input.AsArray<int>();
        result.ShouldBe([1, 2, 3, 4]);
    }

    [Test]
    public void AsArray_FromObjectListWithUncastableButConvertibleItems_ThrowsInvalidCastException() {
        var input = new object[] { 1, 2, 3 };
        Should.Throw<InvalidCastException>(() => input.AsArray<long>());
        
    }
    
    [Test]
    public void AsArray_FromObjectListWhenTCurrentIsList_ReturnsArrayOfNewlyCreatedLists() {
        var input = new object[] { "a", "b" };
        var result = input.AsArray<List<string>>();
        result.Length.ShouldBe(2);
        result[0].ShouldBe(["a"]);
        result[1].ShouldBe(["b"]);
    }

    [Test]
    public void AsArray_FromObjectListWithPromotableElements_ReturnsArrayOfPromotedArrays() {
        var input = new object[] { 1, 2 };
        var result = input.AsArray<int[]>();
        result.Length.ShouldBe(2);
        result[0].ShouldBe([1]);
        result[1].ShouldBe([2]);
    }

    [Test]
    public void AsArray_FromMixedObjectListWithPromotableAndPreExistingArrays_ReturnsCorrectlyCombinedArray() {
        var input = new object[] { "a", new[] { "b", "c" } };
        var result = input.AsArray<string[]>();
        result.Length.ShouldBe(2);
        result[0].ShouldBe(["a"]);
        result[1].ShouldBe(["b", "c"]);
    }

    [Test]
    public void AsArray_FromMixedObjectListWithPromotableAndUncastableElements_FiltersUncastableElements() {
        var input = new object[] { 10, "fail", 20 };
        var result = input.AsArray<int[]>();
        result.Length.ShouldBe(2);
        result[0].ShouldBe([10]);
        result[1].ShouldBe([20]);
    }
}