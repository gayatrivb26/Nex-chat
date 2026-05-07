using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public class GetUserProfileHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetUserProfileQuery q, CancellationToken ct)
    {
        var cacheKey = $"user:{q.TargetUserId}:profile";
        var cached = await cache.GetAsync<UserProfileDto>(cacheKey, ct);
        if (cached != null) return cached;

        var user = await uow.Users.GetByIdAsync(q.TargetUserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var dto = new UserProfileDto(user.Id, user.Username, user.AvatarUrl,
            user.DisplayName, user.Bio, user.Status.ToString(), user.LastSeen, user.IsVerified);

        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct);
        return dto;
    }
}
