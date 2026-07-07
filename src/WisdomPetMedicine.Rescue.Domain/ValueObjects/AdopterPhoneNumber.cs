using System;
using System.Collections.Generic;
using System.Text;

namespace WisdomPetMedicine.Rescue.Domain.ValueObjects;

public record AdopterPhoneNumber
{
    internal AdopterPhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; init; }

    public static AdopterPhoneNumber Create(string value)
    {
        Validate(value);
        return new AdopterPhoneNumber(value);
    }

    public static void Validate(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("Phone value must not be null");

        }

        if (value.Length > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Phone number must be longer than 15 characters");
        }
    }
}
