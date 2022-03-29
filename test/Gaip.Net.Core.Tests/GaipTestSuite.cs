using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Gaip.Net.Core.Contracts;
using NUnit.Framework;

namespace Gaip.Net.Core.Tests;

public class TestClass
{
    public int Id { get; set; }
}

[TestFixture]
public abstract class GaipTestSuite<T, TFilterResult> where T : IFilterAdapter<TFilterResult>
{
    /// <summary>
    /// Allows the test suite to insert TestClass objects into the database.
    /// </summary>
    public abstract Task InsertManyAsync(TestClass[] testClasses);

    /// <summary>
    /// Allows the test suite to query the database for TestClass objects.
    /// </summary>
    public abstract Task<IList<TestClass>> UseFilterResultAsync(TFilterResult result);

    [SetUp]
    public abstract Task SetupAsync();

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    [Test]
    public void Test2()
    {
        Assert.Pass();
    }
}
