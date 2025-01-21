using EntraMfaPrefillinator.Lib.Utilities;

namespace EntraMfaPrefillinator.Lib.Tests.Regex;

[TestClass]
public sealed class CsvDataRegexTests
{
    [TestMethod]
    [DataRow("(555) 555-5555")]
    [DataRow("(555) 5555555")]
    [DataRow("(555)-5555555")]
    [DataRow("555 555-5555")]
    [DataRow("555-555-5555")]
    public void IsValidPhoneNumber_ReturnTrue(string value)
    {
        bool result = CsvDataRegexTools.IsValidPhoneNumber(value);

        Assert.IsTrue(result, $"{value} should be true");
    }

    [TestMethod]
    [DataRow("(555) 5555-5555")]
    [DataRow("(5555) 5555-5555")]
    [DataRow("[555] 555-55555")]
    public void IsValidPhoneNumber_ReturnFalse(string value)
    {
        bool result = CsvDataRegexTools.IsValidPhoneNumber(value);

        Assert.IsFalse(result, message: $"{value} should be false");
    }
}
