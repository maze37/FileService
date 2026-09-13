using System.Transactions;
using Core.Database;

namespace FileService.Infrastructure.Postgres.Database;

using System.Data;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Result;

/// <inheritdoc/>
public class TransactionManager : ITransactionManager
{
    private readonly AppDbContext _context;
    private readonly ILogger<TransactionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    
    public TransactionManager(
        AppDbContext context, 
        ILogger<TransactionManager> logger, 
        ILoggerFactory loggerFactory)
    {
        _context = context;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }
    
    public async Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            if (pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
                return Error.Conflict(
                    "record.already.exist",
                    "Unique constraint violated",
                    pgEx.ConstraintName);

            return Error.Failure(
                "db.update.failed",
                "Database update failed");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DbUpdateException: {Message}, Inner: {Inner}", 
                ex.Message, ex.InnerException?.Message);
            return Error.Failure("db.update.failed", "Database update failed");
        }
    }

    public async Task<Result<ITransactionScope, Error>> BeginTransactionAsync(
        CancellationToken cancellationToken = default,
        IsolationLevel? level = null)
    {
        try
        {
            var transaction = await _context.Database
                .BeginTransactionAsync(level ?? IsolationLevel.ReadCommitted, cancellationToken);

            var logger = _loggerFactory.CreateLogger<TransactionScope>();
            
            var transactionScope = new TransactionScope(transaction.GetDbTransaction(), logger);
            
            return transactionScope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin transaction");
            return Error.Failure("database", "Failed to begin transaction");
        }
    }
}