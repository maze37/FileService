using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Core.Abstractions;

public interface ITransactionManager
{
    Task<UnitResult<Error>> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<UnitResult<Error>> CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken = default);
}