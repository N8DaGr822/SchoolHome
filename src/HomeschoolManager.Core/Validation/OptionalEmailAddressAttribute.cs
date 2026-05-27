using System.ComponentModel.DataAnnotations;

namespace HomeschoolManager.Core.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class OptionalEmailAddressAttribute : ValidationAttribute
{
    private readonly EmailAddressAttribute _emailAddress = new();

    public OptionalEmailAddressAttribute()
        : base("The {0} field is not a valid e-mail address.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is null
            || value is string text && string.IsNullOrWhiteSpace(text)
            || _emailAddress.IsValid(value);
    }
}
