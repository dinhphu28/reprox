using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RecordingProxy.Services;
using Serilog;

namespace RecordingProxy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecordingListController : ControllerBase
    {
        IConfiguration configuration;
        Recording3CX recording3CX;
        Wasabi wasabi;
        public RecordingListController(IConfiguration configuration, Recording3CX recording3CX, Wasabi wasabi)
        {
            this.configuration = configuration;
            this.recording3CX = recording3CX;
            this.wasabi = wasabi;
        }
        [HttpGet("download")]
        public async Task<IActionResult> Download(int file)
        {
            try
            {
                Log.Information($"Download --> file: {file}");
                var file1 = await recording3CX.GetRecordingByIdAsync(file);
                if (string.IsNullOrEmpty(file1)) return NotFound();
                var url3cx = await recording3CX.GetURLAsync(file1);
                //if (url3cx != null) return new FileStreamResult(url3cx, "audio/wav") { FileDownloadName = $"{file1}.wav" };
                if (url3cx != null) return new FileStreamResult(url3cx, "audio/wav");
                var url = await wasabi.GetFileUrl(file1);
                if (url == null) return NotFound();
                
                return new FileStreamResult(url, "audio/wav");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return StatusCode(500);
            }
        }

        [HttpGet("DownloadRecord")]
        public async Task<IActionResult> DownloadRecord(string file)
        {
            try
            {
                Log.Information($"DownloadRecord --> file: {file}");
                var file1 = file;
                var url3cx = await recording3CX.GetURLAsync(file1);
                if (url3cx != null) return new FileStreamResult(url3cx, "audio/wav");
                var url = await wasabi.GetFileUrl(file1);
                if (url == null) return NotFound();
                return new FileStreamResult(url, "audio/wav");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return StatusCode(500);
            }
        }

    }
}
