using MediatR;

namespace ChatApp.Application.Commands.Auth;

public record SendOtpCommand(string Phone) : IRequest<bool>;
