using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MyBGList.Attributes;

public class SortColumnValidatorAttribute : ValidationAttribute
{
    public Type EntityType { get; set; }
    public SortColumnValidatorAttribute(Type entityType) : base("Value must match an existing column.")
    {
        EntityType = entityType;
    }
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
        if(EntityType == null) {
            return new ValidationResult(ErrorMessage);
        }

        var valueAsString = value as string;
        if(!string.IsNullOrEmpty(valueAsString)) {
            if(EntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.Name == valueAsString)) {
                    return ValidationResult.Success;
            }
        }

        return new ValidationResult(ErrorMessage);
    }
}
