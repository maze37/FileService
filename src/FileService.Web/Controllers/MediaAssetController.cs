using Amazon.S3.Model;
using Core.Abstractions;
using FileService.Contracts;
using FileService.Core.UseCases.Commands.AbortMultipartUpload;
using FileService.Core.UseCases.Commands.CancelUpload;
using FileService.Core.UseCases.Commands.CompleteMultipartUpload;
using FileService.Core.UseCases.Commands.CompleteUpload;
using FileService.Core.UseCases.Commands.DeleteFile;
using FileService.Core.UseCases.Commands.InitiateUpload;
using FileService.Core.UseCases.Commands.StartMultipartUpload;
using FileService.Core.UseCases.Queries.GetFile;
using FileService.Core.UseCases.Queries.GetFilesByTargetEntity;
using Framework.ResponseExtensions;
using Microsoft.AspNetCore.Mvc;
using Shared.Result;
using AbortMultipartUploadRequest = FileService.Contracts.AbortMultipartUploadRequest;
using AbortMultipartUploadResponse = FileService.Contracts.AbortMultipartUploadResponse;
using CompleteMultipartUploadRequest = FileService.Contracts.CompleteMultipartUploadRequest;
using CompleteMultipartUploadResponse = FileService.Contracts.CompleteMultipartUploadResponse;

namespace FileService.Web.Controllers;

[ApiController]
[Route("api/files")]
public class MediaAssetController : ControllerBase
{
    private readonly ICommandHandler<InitiateUploadCommand, InitiateUploadResponse> _initiateUploadHandler;
    private readonly ICommandHandler<CompleteUploadCommand, CompleteUploadResponse> _completeUploadHandler;
    private readonly IQueryHandlerWithResult<GetFileQuery, GetFileResponse> _getFileHandler;
    private readonly IQueryHandlerWithResult<GetFilesByTargetEntityQuery, GetFilesByTargetEntityResponse> _getFilesByTargetEntityHandler;
    private readonly ICommandHandler<CancelUploadCommand, CancelUploadResponse> _cancelUploadHandler;
    private readonly ICommandHandler<DeleteFileCommand, DeleteFileResponse> _deleteFileHandler;
    private readonly ICommandHandler<StartMultipartUploadCommand, StartMultipartUploadResponse> _startMultipartHandler;
    private readonly ICommandHandler<CompleteMultipartUploadCommand, CompleteMultipartUploadResponse> _completeMultipartHandler;
    private readonly ICommandHandler<AbortMultipartUploadCommand, AbortMultipartUploadResponse> _abortMultipartHandler;
    
    public MediaAssetController(
        ICommandHandler<InitiateUploadCommand, InitiateUploadResponse> initiateUploadHandler,
        ICommandHandler<CompleteUploadCommand, CompleteUploadResponse> completeUploadHandler,
        IQueryHandlerWithResult<GetFileQuery, GetFileResponse> getFileHandler,
        IQueryHandlerWithResult<GetFilesByTargetEntityQuery, GetFilesByTargetEntityResponse> getFilesByTargetEntityHandler,
        ICommandHandler<CancelUploadCommand, CancelUploadResponse> cancelUploadHandler,
        ICommandHandler<DeleteFileCommand, DeleteFileResponse> deleteFileHandler,
        ICommandHandler<StartMultipartUploadCommand, StartMultipartUploadResponse> startMultipartHandler,
        ICommandHandler<CompleteMultipartUploadCommand, CompleteMultipartUploadResponse> completeMultipartHandler,
        ICommandHandler<AbortMultipartUploadCommand, AbortMultipartUploadResponse> abortMultipartHandler)
    {
        _initiateUploadHandler = initiateUploadHandler;
        _completeUploadHandler = completeUploadHandler;
        _getFileHandler = getFileHandler;
        _getFilesByTargetEntityHandler = getFilesByTargetEntityHandler;
        _cancelUploadHandler = cancelUploadHandler;
        _deleteFileHandler = deleteFileHandler;
        _startMultipartHandler = startMultipartHandler;
        _completeMultipartHandler = completeMultipartHandler;
        _abortMultipartHandler = abortMultipartHandler;
    }
    
    [HttpPost("upload/initiate")]
    public async Task<IActionResult> InitiateUpload(
        [FromBody] InitiateUploadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new InitiateUploadCommand(
            request.FileName, 
            request.ContentType, 
            request.FileSize, 
            request.Context, 
            request.EntityId,
            request.AssetType);
        var response = await _initiateUploadHandler.HandleAsync(command, cancellationToken);
        if (response.IsFailure)
            return response.Error.ToResponse();
        
        return Ok(Envelope.Ok(response.Value));
    }

    [HttpPost("upload/{mediaAssetId:guid}/complete")]
    public async Task<IActionResult> CompleteUpload(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        var command = new CompleteUploadCommand(mediaAssetId);
        var response = await _completeUploadHandler.HandleAsync(command, cancellationToken);
        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }

    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> GetFile(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var query = new GetFileQuery(fileId);
        var response = await _getFileHandler.HandleAsync(query, cancellationToken);

        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }

    [HttpGet]
    public async Task<IActionResult> GetFilesByTargetEntity(
        [FromQuery] string context,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken)
    {
        var query = new GetFilesByTargetEntityQuery(context, entityId);
        var response = await _getFilesByTargetEntityHandler.HandleAsync(query, cancellationToken);

        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }

    [HttpPatch("{fileId:guid}/cancel")]
    public async Task<IActionResult> CancelUpload(
        [FromRoute] Guid fileId,
        CancellationToken cancellationToken)
    {
        var command = new CancelUploadCommand(fileId);
        var response = await _cancelUploadHandler.HandleAsync(command, cancellationToken);

        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }

    [HttpDelete("{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(
        [FromRoute] Guid fileId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFileCommand(fileId);
        var response = await _deleteFileHandler.HandleAsync(command, cancellationToken);

        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }

    [HttpPost("multipart/start")]
    public async Task<IActionResult> StartMultipart(
        [FromBody] StartMultipartUploadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StartMultipartUploadCommand(request);
        
        var response = await _startMultipartHandler.HandleAsync(command, cancellationToken);

        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }
    
    [HttpPost("multipart/complete")]
    public async Task<IActionResult> CompleteMultipart(
        [FromBody] CompleteMultipartUploadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteMultipartUploadCommand(request);
        
        var response = await _completeMultipartHandler.HandleAsync(command, cancellationToken);

        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }
    
    [HttpPost("multipart/abort")]
    public async Task<IActionResult> AbortMultipart(
        [FromBody] AbortMultipartUploadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AbortMultipartUploadCommand(request);
        
        var response = await _abortMultipartHandler.HandleAsync(command, cancellationToken);

        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }
}