using Microsoft.AspNetCore.OutputCaching;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Infrastructure.Caching;

public sealed class OutputCacheInvalidator : ICacheInvalidator
{
    private readonly IOutputCacheStore _store;

    public OutputCacheInvalidator(IOutputCacheStore store)
    {
        _store = store;
    }

    public Task InvalidateAsync(string tag, CancellationToken cancellationToken = default) =>
        _store.EvictByTagAsync(tag, cancellationToken).AsTask();
}
