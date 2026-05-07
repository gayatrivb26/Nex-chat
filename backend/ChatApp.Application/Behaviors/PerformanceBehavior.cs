using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
namespace ChatApp.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : notnull
{
	private const int WarningThresholdMs = 500;

	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
	{
		var sw = Stopwatch.StartNew();
		var response = await next();
		sw.Stop();

		if (sw.ElapsedMilliseconds > WarningThresholdMs)
			logger.LogWarning("Slow request detected: {RequestType} took {ElapsedMs}ms",
				typeof(TRequest).Name, sw.ElapsedMilliseconds);

		return response;
	}
}
