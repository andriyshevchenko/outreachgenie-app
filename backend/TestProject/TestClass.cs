namespace TestProject;

/// <summary>
/// Test class implementing ITestInterface.
/// </summary>
public sealed class TestClass : ITestInterface
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestClass"/> class.
    /// </summary>
    /// <param name="name">The name value.</param>
    public TestClass(string name)
    {
        _name = name;
    }

    /// <inheritdoc/>
    public string GetName()
    {
        return _name;
    }
}
