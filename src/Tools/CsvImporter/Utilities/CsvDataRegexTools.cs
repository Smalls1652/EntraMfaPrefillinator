using System.Text.RegularExpressions;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

/// <summary>
/// Houses <see cref="Regex"/> methods for parsing CSV data.
/// </summary>
internal static partial class CsvDataRegexTools
{
    /// <summary>
    /// Determines if a line from a CSV file is valid.
    /// </summary>
    /// <param name="csvLine">The line from a CSV file.</param>
    /// <returns>True if the line is valid; otherwise, false.</returns>
    public static bool IsValidCsvLine(string csvLine) => ValidCsvLineRegex().IsMatch(csvLine);

    [GeneratedRegex(
        pattern: """(".*?"(?:,|)){4}"""
    )]
    private static partial Regex ValidCsvLineRegex();

    [GeneratedRegex(
        pattern: "\"(?'employeeNumber'.*?)\",\"(?'userName'.*?)\",\"(?'emailAddress'.*?)\",\"(?'phoneNumber'.*?)\",\"(?'homePhoneNumber'.*?)\""
    )]
    public static partial Regex UserDetailsCsvLineRegex();

    /// <summary>
    /// Checks if the phone number has a country code.
    /// </summary>
    /// <param name="phoneNumber">The phone number to check.</param>
    /// <returns>True if the phone number has a country code; otherwise, false.</returns>
    public static bool PhoneNumberHasCountryCode(string phoneNumber) => PhoneNumberHasCountryCodeRegex().IsMatch(phoneNumber);

    [GeneratedRegex(
        pattern: @"\+\d{1,} .+"
    )]
    private static partial Regex PhoneNumberHasCountryCodeRegex();

    /// <summary>
    /// Checks if the provided email address is valid.
    /// </summary>
    /// <param name="emailAddress">The email address to check.</param>
    /// <returns>True if the email address is valid; otherwise, false.</returns>
    public static bool IsValidEmailAddress(string emailAddress) => EmailAddressRegex().IsMatch(emailAddress);

    [GeneratedRegex(
        pattern: @".+?\@.+"
    )]
    private static partial Regex EmailAddressRegex();

    [GeneratedRegex(
        pattern: @"(?'leadingZeroes'0{1,})(?'number'\d+)"
    )]
    public static partial Regex LeadingZeroesRegex();
}