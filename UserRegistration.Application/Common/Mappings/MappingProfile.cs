using AutoMapper;
using UserRegistration.Application.DTOs.Auth;
using UserRegistration.Application.DTOs.BlogPosts;
using UserRegistration.Application.DTOs.Categories;
using UserRegistration.Application.DTOs.Roles;
using UserRegistration.Application.DTOs.Users;
using UserRegistration.Application.Features.Auth.Login;
using UserRegistration.Application.Features.Auth.RefreshToken;
using UserRegistration.Application.Features.Auth.RevokeToken;
using UserRegistration.Application.Features.BlogPosts.Commands.CreateBlogPost;
using UserRegistration.Application.Features.Users.Commands.ChangePassword;
using UserRegistration.Application.Features.Users.Commands.CreateUser;
using UserRegistration.Application.Features.Users.Commands.RegisterUser;
using UserRegistration.Application.Features.Users.Commands.SetUserStatus;
using UserRegistration.Application.Features.Users.Commands.UpdateUser;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Common.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Name));

        CreateMap<Role, RoleDto>();

        CreateMap<Category, CategoryDto>();

        CreateMap<RegisterUserRequest, RegisterUserCommand>();
        CreateMap<CreateUserRequest, CreateUserCommand>();
        CreateMap<UpdateUserRequest, UpdateUserCommand>();
        CreateMap<ChangePasswordRequest, ChangePasswordCommand>();
        CreateMap<SetUserStatusRequest, SetUserStatusCommand>();

        CreateMap<LoginRequest, LoginCommand>();
        CreateMap<RefreshTokenRequest, RefreshTokenCommand>();
        CreateMap<RefreshTokenRequest, RevokeTokenCommand>();

        CreateMap<CreateBlogPostRequest, CreateBlogPostCommand>();
    }
}
