using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SftpController : ControllerBase
{
    private readonly SftpService _sftpService;

    public SftpController(SftpService sftpService)
    {
        _sftpService = sftpService;
    }

    [HttpGet("list")]
    public IActionResult ListRemoteFiles([FromQuery] string remotePath)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
            return BadRequest("Remote path is required.");

        try
        {
            var files = _sftpService.ListFiles(remotePath);
            return Ok(files);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error listing files: {ex.Message}");
        }
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadFiles([FromQuery] string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return BadRequest("Remote path is required.");

        try
        {
            var downloadedFiles = await _sftpService.DownloadAllFilesAsync(folderName);
            return Ok(new
            {
                Message = $"Downloaded {downloadedFiles.Count} file(s).",
                Files = downloadedFiles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error downloading files: {ex.Message}");
        }
    }
}
