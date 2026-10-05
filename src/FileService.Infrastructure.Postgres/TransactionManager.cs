using System.Data.Common;
using CSharpFunctionalExtensions;
using FileService.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SharedKernel;
using Wolverine.EntityFrameworkCore;

namespace FileService.Infrastructure.Postgres;

public class TransactionManager : ITransactionManager, IAsyncDisposable
{
    private readonly IDbContextOutbox<FileServiceDbContext> _outbox;
    private readonly ILogger<TransactionManager> _logger;

    private IDbContextTransaction? _currentTransaction;
    
    public TransactionManager(
        IDbContextOutbox<FileServiceDbContext> outbox, 
        ILogger<TransactionManager> logger)
    {
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _currentTransaction = await _outbox.DbContext.Database.BeginTransactionAsync(cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin transaction");
            return GeneralErrors.DatabaseError();
        }
    }
    
    public async Task<UnitResult<Error>> CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return GeneralErrors.DatabaseError();

        try
        {
            await _outbox.DbContext.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
            await _outbox.FlushOutgoingMessagesAsync();
            return UnitResult.Success<Error>();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict during commit.");
            await RollbackAsync(cancellationToken);
            return GeneralErrors.ConcurrencyConflict();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Operation cancelled during commit.");
            await RollbackAsync(cancellationToken);
            return GeneralErrors.OperationCancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during commit.");
            await RollbackAsync(cancellationToken);
            return GeneralErrors.DatabaseError();
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();
    }

    public void Dispose()
    {
        if (_currentTransaction is not null)
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }
    
    public DbConnection GetDbConnection() => _outbox.DbContext.Database.GetDbConnection();
    
    public async Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction is not null)
                await _outbox.DbContext.SaveChangesAsync(cancellationToken);
            else
                await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
            
            return UnitResult.Success<Error>();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict during save.");
            return GeneralErrors.ConcurrencyConflict();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Operation cancelled during save.");
            return GeneralErrors.OperationCancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during save.");
            return GeneralErrors.DatabaseError();
        }
    }
    
    private async Task RollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_currentTransaction is not null)
                await _currentTransaction.RollbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction.");
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}