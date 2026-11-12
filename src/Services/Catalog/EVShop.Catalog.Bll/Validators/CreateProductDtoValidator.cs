using EVShop.Catalog.Bll.DTOs;
using FluentValidation;

namespace EVShop.Catalog.Bll.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва товару є обов'язковою")
            .MaximumLength(200).WithMessage("Назва не повинна перевищувати 200 символів");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU є обов'язковим")
            .Matches("^[A-Za-z0-9-]+$").WithMessage("SKU повинен містити лише латинські літери, цифри та дефіс");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Ціна повинна бути більшою за 0");

        RuleFor(x => x.AvailableStock)
            .GreaterThanOrEqualTo(0).WithMessage("Залишок не може бути від'ємним");

        RuleFor(x => x.OperatingVoltageV)
            .InclusiveBetween(12, 1000).WithMessage("Робоча напруга компонента повинна бути від 12 до 1000 В")
            .When(x => x.OperatingVoltageV.HasValue);
    }
}
