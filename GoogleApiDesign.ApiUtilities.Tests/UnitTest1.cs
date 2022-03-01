using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;

namespace GoogleApiDesign.ApiUtilities.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            // Arrange
            var firstNames = new[]
            {
                "John",
                "William",
                "Alice"
            };

            var filter = "John OR Alice";

            // Act

            // Assert

        }
    }
}