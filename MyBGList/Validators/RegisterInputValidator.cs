using FluentValidation;
using MyBGList.Models;

namespace MyBGList.Validators;

public class RegisterInputValidator : AbstractValidator<RegisterDTO>
{
    public RegisterInputValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(7);

        RuleFor(x => x.Email)
            .EmailAddress();
    }
}
