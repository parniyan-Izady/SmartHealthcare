using AutoMapper;
using SmartHealthcare.Application.Common.Interfaces;
using SmartHealthcare.Application.DTOs;
using SmartHealthcare.Application.Repositories;
using SmartHealthcare.Domain.Entities;
using SmartHealthcare.Domain.Exceptions;

namespace SmartHealthcare.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher, 
        IJwtTokenGenerator jwtTokenGenerator, 
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new DomainException($"Email '{request.Email}' is already registered.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(request.FirstName, request.LastName, request.Email, passwordHash, request.Role);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(user.Id, $"{user.FirstName} {user.LastName}", user.Email, user.Role.ToString(), token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new DomainException("Invalid email or password.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(user.Id, $"{user.FirstName} {user.LastName}", user.Email, user.Role.ToString(), token);
    }
}
