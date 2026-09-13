using Core.Abstractions;
using FileService.Contracts;
using FileService.Core.UseCases.Commands.CompleteUpload;
using FileService.Core.UseCases.Commands.InitiateUpload;
using FileService.Core.UseCases.Queries.GetFile;
using Framework.ResponseExtensions;
using Microsoft.AspNetCore.Mvc;
using Shared.Result;

namespace FileService.Web.Controllers;

[ApiController]
public class MediaAssetController : ControllerBase
{
    private readonly ICommandHandler<InitiateUploadCommand, InitiateUploadResponse> _initiateUploadHandler;
    private readonly ICommandHandler<CompleteUploadCommand, CompleteUploadResponse> _completeUploadHandler;
    private readonly IQueryHandlerWithResult<GetFileQuery, GetFileResponse> _getFileHandler;
    
    public MediaAssetController(
        ICommandHandler<InitiateUploadCommand, InitiateUploadResponse> initiateUploadHandler,
        ICommandHandler<CompleteUploadCommand, CompleteUploadResponse> completeUploadHandler,
        IQueryHandlerWithResult<GetFileQuery, GetFileResponse> getFileHandler)
    {
        _initiateUploadHandler = initiateUploadHandler;
        _completeUploadHandler = completeUploadHandler;
        _getFileHandler = getFileHandler;
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

    [HttpGet("{mediaAssetId:guid}")]
    public async Task<IActionResult> GetFile(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        var query = new GetFileQuery(mediaAssetId);
        var response = await _getFileHandler.HandleAsync(query, cancellationToken);
        
        if (response.IsFailure)
            return response.Error.ToResponse();

        return Ok(Envelope.Ok(response.Value));
    }
}