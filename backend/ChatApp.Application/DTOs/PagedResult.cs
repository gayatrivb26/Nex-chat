using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record PagedResult<T>(
	IEnumerable<T> Items,
	int TotalCount,
	int Page,
	int PageSize,
	bool HasMore)
{
	public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
		=> new(items, totalCount, page, pageSize, (page * pageSize) < totalCount);
}
