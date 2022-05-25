using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RecordingProxy.Models;
using RecordingProxy.Utils;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RecordingProxy.Controllers
{
    [Route("api/v1/auth")]
    public class AuthController : Controller
    {
        private IConfiguration conf;

        public AuthController(IConfiguration conf)
        {
            this.conf = conf;
        }

        public IConfiguration Conf { get => conf; set => conf = value; }

        // Login
        // POST api/v1/auth
        [HttpPost]
        public ObjectResult Post([FromBody] UserIdentification user)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Credentials", "true");

            string res = "no";
            string token = "";

            try
            {
                res = DatabaseProvider.ReturnDataFromSQL(Conf["dbConnection"].ToString(), user.Username, user.Password);

                if (res.Equals("yes"))
                {
                    token = JwtUtils.generateToken();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"MultipartFileController --> DownloadFileStream: Database query error: " + ex.ToString());

                return StatusCode((int)HttpStatusCode.InternalServerError, new NoticeReturn("500", "Interal server error"));
            }

            if (user == null)
            {
                return BadRequest(new NoticeReturn("400", "Username and password cannot be null"));
            }
            else if (string.IsNullOrEmpty(user.Username) && string.IsNullOrEmpty(user.Password))
            {
                return BadRequest(new NoticeReturn("400", "Username and password cannot be null"));
            }
            else if (res.Equals("yes"))
            {
                // if username & password are valid => return access token
                return StatusCode((int)HttpStatusCode.Created, new NoticeReturn("Access Token", token));
            }
            else
            {
                return BadRequest(new NoticeReturn("400", "Invalid username or password"));
            }
        }
    }
}
