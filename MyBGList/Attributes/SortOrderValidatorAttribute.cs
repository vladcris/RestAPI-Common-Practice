using MyBGList.Models;
using System.ComponentModel.DataAnnotations;

namespace MyBGList.Attributes;

public class SortOrderValidatorAttribute : ValidationAttribute
{
    // public so it can be used in other places
    public string[] AllowedValues { get; set; } = new[] { "ASC", "DESC" };

    public SortOrderValidatorAttribute() 
        : base("Value must be one of the following: {0}.") {}
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
        string? valueAsString = value as string;

        if(!string.IsNullOrEmpty(valueAsString) && AllowedValues.Contains(valueAsString, StringComparer.InvariantCultureIgnoreCase)) {
            return ValidationResult.Success;
        }


        return new ValidationResult(FormatErrorMessage(string.Join(',', AllowedValues)));
    }
}
