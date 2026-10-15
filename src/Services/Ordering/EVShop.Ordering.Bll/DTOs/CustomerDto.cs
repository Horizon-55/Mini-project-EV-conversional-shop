using System.ComponentModel.DataAnnotations;

namespace EVShop.Ordering.Bll.DTOs;

public record CustomerDto(
    long Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    DateTime CreatedAt
);

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Email є обов'язковим.")]
    [EmailAddress(ErrorMessage = "Некоректний формат email адреси.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ім'я та прізвище є обов'язковими.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Ім'я має містити від 2 до 150 символів.")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Некоректний формат номера телефону.")]
    public string? PhoneNumber { get; set; }
}
