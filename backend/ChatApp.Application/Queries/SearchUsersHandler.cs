using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public class SearchUsersHandler(IUnitOfWork uow) : IRequestHandler<SearchUsersQuery, IEnumerable<UserProfileDto>>
{
    public async Task<IEnumerable<UserProfileDto>> Handle(SearchUsersQuery q, CancellationToken ct)
    {
        var users = await uow.Users.SearchUsersAsync(q.Query, q.Limit, ct);
        return users
            .Where(u => u.Id != q.RequestingUserId && !u.IsDeleted)
            .Select(u => new UserProfileDto(u.Id, u.Username, u.AvatarUrl,
                u.DisplayName, u.Bio, u.Status.ToString(), u.LastSeen, u.IsVerified));
    }
}
