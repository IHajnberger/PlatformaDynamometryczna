using System;

namespace UI_Test_Avalonia;

public class Patient
{
    // Unikalny identyfikator generowany automatycznie dla każdego nowego pacjenta
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    // Właściwość pomocnicza łącząca imię i nazwisko w jeden ciąg tekstowy
    public string FullName => $"{FirstName} {LastName}".Trim();

    public DateTime BirthDate { get; set; }

    public string Notes { get; set; } = "";

    public string PhoneNumber { get; set; } = "";
}