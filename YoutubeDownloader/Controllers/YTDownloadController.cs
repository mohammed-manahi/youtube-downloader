using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Converter;

namespace YoutubeDownloader.Controllers;

[ApiController]
[Route("[controller]")]
public class YTDownloadController : ControllerBase
{
    private readonly ILogger<YTDownloadController> _logger;
    private readonly IConfiguration _configuration;
    private readonly YoutubeClient _youtubeClient;

    public YTDownloadController(ILogger<YTDownloadController> logger, IConfiguration configuration,
        YoutubeClient youtubeClient)
    {
        _logger = logger;
        _configuration = configuration;
        _youtubeClient = youtubeClient;
    }

    [HttpGet]
    [Route("GetYTMediaInfo")]
    public async Task<IActionResult> GetYTMediaInfo(string contentUrl)
    {
        var content = await _youtubeClient.Videos.GetAsync(contentUrl);
        _logger.LogInformation($"Getting information for {contentUrl}");
        if (content == null) return BadRequest("Unable to get video from Youtube");
        var title = content.Title;
        var author = content.Author;
        var duration = content.Duration;
        return Ok(new { message = "Metadata retrieved successfully", title, author, duration });
    }

    [HttpPost]
    [Route("DownloadYTMedia")]
    public async Task<IActionResult> DownloadYTMedia([Required] string contentUrl,
        [Required] ContentFormat contentFormat)
    {
        string mimeType = GetMimeType(contentFormat);
        if (contentFormat == ContentFormat.Video)
        {
            var (downloadFilePath, extension) = await DownloadContent(contentUrl, "mp4");
            _logger.LogInformation($"Downloading video from path {contentUrl}");
            return PhysicalFile(downloadFilePath, mimeType, $"download.{extension}");
        }

        else if (contentFormat == ContentFormat.Audio)
        {
            var (downloadFilePath, extension) = await DownloadContent(contentUrl, "mp3");
            _logger.LogInformation($"Downloading audio from path {contentUrl}");
            return PhysicalFile(downloadFilePath, mimeType, $"download.{extension}");
        }

        else
        {
            return Problem("Unknown content format");
        }
    }

    private async Task<(string, string)> DownloadContent(string contentUrl, string extension)
    {
        string downloadFilePath = Path.GetTempFileName() + $".{extension}";
        
        // FFmpeg is required on the system
        // Current version on the system is ffmpeg version 7.1.1 built with gcc 14 (GCC) on Fedora 41
        var ffmpegPath = _configuration.GetValue<string>("FfmpegPath");
        
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {
            _logger.LogError("FFmpeg path was not found");
            throw new InvalidOperationException("FFmpeg path was not found");
        }

        await _youtubeClient.Videos.DownloadAsync(contentUrl, downloadFilePath,
            config => config
                .SetContainer(extension)
                .SetPreset(ConversionPreset.UltraFast)
                .SetFFmpegPath(ffmpegPath));

        return (downloadFilePath, extension);
    }
    
    private string GetMimeType(ContentFormat contentFormat)
    {
        // Decides physical file content type based on format
        return contentFormat switch
        {
            ContentFormat.Video => "video/mp4",
            ContentFormat.Audio => "audio/mp3",
            _ => "application/octet-stream"
        };
    }
}

public enum ContentFormat
{
    Video,
    Audio
}