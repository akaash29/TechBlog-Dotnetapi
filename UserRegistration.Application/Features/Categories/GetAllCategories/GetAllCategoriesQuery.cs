using MediatR;
using UserRegistration.Application.DTOs.Categories;

namespace UserRegistration.Application.Features.Categories.GetAllCategories;

public sealed class GetAllCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>
{
}
