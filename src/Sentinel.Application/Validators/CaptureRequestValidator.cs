using FluentValidation;
using Sentinel.Application.Dtos;
using Sentinel.Domain.Enums;

namespace Sentinel.Application.Validators;

public sealed class CaptureRequestValidator : AbstractValidator<CaptureRequest>
{
    public CaptureRequestValidator()
    {
        RuleFor(x => x.SourceId)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Source)
            .IsInEnum()
            .NotEqual(CaptureSource.Unknown);

        RuleFor(x => x.RawText)
            .NotEmpty()
            .MaximumLength(20000);

        RuleFor(x => x.Url)
            .MaximumLength(2048);

        RuleFor(x => x.AuthorHandle)
            .MaximumLength(200);
    }
}
