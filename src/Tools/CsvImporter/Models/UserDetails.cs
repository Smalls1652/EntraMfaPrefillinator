using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Models;

/// <summary>
/// Holds data imported, from a CSV file, for a user's details.
/// </summary>
public sealed class UserDetails
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserDetails"/> class.
    /// </summary>
    public UserDetails()
    {}

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDetails"/> class.
    /// </summary>
    /// <param name="employeeNumber">The user's employee number.</param>
    /// <param name="userName">The user's username.</param>
    /// <param name="secondaryEmail">The user's secondary email address.</param>
    /// <param name="phoneNumber">The user's phone number.</param>
    public UserDetails(string? employeeNumber, string? userName, string? secondaryEmail, string? phoneNumber)
    {
        EmployeeNumber = employeeNumber;
        UserName = userName;
        SecondaryEmail = secondaryEmail;
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDetails"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is used for parsing the line from a CSV file.
    /// </remarks>
    /// <param name="csvLine">The line from a CSV file.</param>
    public UserDetails(string csvLine)
    {
        Match userDetailsMatch = CsvDataRegexTools.UserDetailsCsvLineRegex().Match(csvLine);

        EmployeeNumber = ParseEmployeeNumber(userDetailsMatch.Groups["employeeNumber"].Value);
        UserName = ParseUserName(userDetailsMatch.Groups["userName"].Value);
        SecondaryEmail = ParseSecondaryEmail(userDetailsMatch.Groups["emailAddress"].Value);
        PhoneNumber = ParsePhoneNumber(userDetailsMatch.Groups["phoneNumber"].Value);
    }

    /// <summary>
    /// The user's employee number.
    /// </summary>
    [Key]
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// The user's username.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The user's secondary email address.
    /// </summary>
    public string? SecondaryEmail { get; set; }

    /// <summary>
    /// The user's phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Whether or not was in a previous run.
    /// </summary>
    public bool IsInLastRun { get; set; } = false;

    /// <summary>
    /// Parse the employee number provided.
    /// </summary>
    /// <param name="employeeNumber">The employee number to parse.</param>
    /// <returns>The parsed employee number.</returns>
    private static string? ParseEmployeeNumber(string? employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return null;
        }

        return employeeNumber.TrimStart('0');
    }

    /// <summary>
    /// Parse the username provided.
    /// </summary>
    /// <param name="userName">The username to parse.</param>
    /// <returns>The parsed username.</returns>
    private static string? ParseUserName(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return userName;
    }

    /// <summary>
    /// Parse the secondary email provided.
    /// </summary>
    /// <param name="secondaryEmail">The secondary email to parse.</param>
    /// <returns>The parsed secondary email.</returns>
    private static string? ParseSecondaryEmail(string? secondaryEmail)
    {
        if (string.IsNullOrWhiteSpace(secondaryEmail) || !CsvDataRegexTools.IsValidEmailAddress(secondaryEmail))
        {
            return null;
        }

        return secondaryEmail;
    }

    /// <summary>
    /// Parse the phone number provided.
    /// </summary>
    /// <param name="phoneNumber">The phone number to parse.</param>
    /// <returns>The parsed phone number.</returns>
    private static string? ParsePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        if (!CsvDataRegexTools.PhoneNumberHasCountryCode(phoneNumber))
        {
            phoneNumber = $"+1 {phoneNumber}";
        }

        return phoneNumber;
    }
}