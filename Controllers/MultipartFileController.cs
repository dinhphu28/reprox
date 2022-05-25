using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using RecordingProxy.Models;
using RecordingProxy.Utils;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RecordingProxy.Controllers
{
    [Route("api/v1/files")]
    public class MultipartFileController : Controller
    {
        //private string localFilePath = @"/Users/dinhphu/tmp/filetotest/";

        private IConfiguration conf;

        public MultipartFileController(IConfiguration conf)
        {
            this.conf = conf;
        }

        public IConfiguration Conf { get => conf; set => conf = value; }

        //// GET: api/v1/files
        //[HttpGet]
        //public FileStreamResult DownloadFileStream()
        //{
        //    string fileName = "chaongaymoi.wav";

        //    var filePath = Path.Combine(localFilePath, fileName);
        //    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //    return File(fileStream, "audio/wav", fileName);
        //}

        [HttpGet("{id}")]
        public Object DownloadFileStream(string id)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Credentials", "true");

            string authorization = HttpContext.Request.Headers["Authorization"];

            Log.Information($"MultipartFileController --> DownloadFileStream: INPUT: id=" + id + "; Authorization=" + authorization);

            if (string.IsNullOrEmpty(authorization))
            {
                return Unauthorized(new NoticeReturn("401", "Unauthorized"));
            }
            else
            {
                if (JwtUtils.validateToken(authorization.Substring(7)))
                {
                    //string fileName = "chao-ngay-moi.wav";

                    //string fileResource = "https://teamsoft.ringbot.co/files/";
                    string fileResource = Conf["fileResources:baseUrl"].ToString();
                    string fileName = "";

                    //int determineCode = 0;
                    try
                    {
                        //determineCode = 1;
                        //fileName = DatabaseProvider.ReturnDataFromSQL("Password=Abc@1234;Username=sa2;Port=5433;Database=sms_survey;Host=103.146.20.129", "10038", "abc123", id);
                        fileName = Base64Utils.Base64Decode(id);

                        if (string.IsNullOrEmpty(fileName))
                        {
                            return NotFound(new NoticeReturn("404", "Not found"));
                        }
                        else
                        {
                            // Get file from Internet source
                            //determineCode = 2;
                            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(fileResource + fileName);
                            httpRequest.Method = WebRequestMethods.Http.Get;

                            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                            Stream httpResponseStream = httpResponse.GetResponseStream();


                            //var filePath = Path.Combine(localFilePath, fileName);
                            //var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                            //return File(fileStream, "audio/wav", fileName);
                            return File(httpResponseStream, "audio/wav", "audio.wav");// fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        //if (determineCode == 1)
                        //{
                        //    Log.Error($"MultipartFileController --> DownloadFileStream: Database query error: " + ex.ToString());
                        //}
                        //else if(determineCode == 2)
                        //{
                        //    Log.Error($"MultipartFileController --> DownloadFileStream: File resources cannot be connected: " + fileResource + fileName);
                        //}

                        Log.Error($"MultipartFileController --> DownloadFileStream: File resources cannot be connected: " + fileResource + fileName);

                        return NotFound(new NoticeReturn("404", "Not found"));
                    }
                }
                else
                {
                    return Unauthorized(new NoticeReturn("401", "Unauthorized"));
                }
            }
        }
    }
}
