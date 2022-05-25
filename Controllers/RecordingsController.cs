using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RecordingProxy.Services;
using Serilog;

namespace RecordingProxy.Controllers
{
    public class RecordingsController : Controller
    {
        Wasabi wasabi;
        Recording3CX recording3CX;
        IConfiguration configuration { get; }
        public RecordingsController(Wasabi Wasabi, IConfiguration configuration, Recording3CX recording3CX)
        {
            this.wasabi = Wasabi;
            this.configuration = configuration;
            this.recording3CX = recording3CX;
        }
        public async Task<IActionResult> Index(string file="")
        {
            try
            {
                Log.Information($"Index --> file: {file}");
                if (string.IsNullOrEmpty(file)) return NotFound();
                var url3cx =await recording3CX.GetURLAsync(file);
                if (url3cx!=null) return new FileStreamResult(url3cx, "audio/wav");
                var url =await wasabi.GetFileUrl(file);
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
