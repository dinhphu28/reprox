using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RecordingProxy.Services
{
    public class Wasabi
    {
        public IConfiguration Configuration { get; }
        public Wasabi(IConfiguration Configuration)
        {
            this.Configuration = Configuration;
        }
        public async Task<Stream> GetFileUrl(string file)
        {
            try
            {
                var config = new AmazonS3Config { ServiceURL = Configuration["Wasabi:endpoint"] };
                using (IAmazonS3 s3Client = new AmazonS3Client(Configuration["Wasabi:accessKey"], Configuration["Wasabi:secretAccesskey"], config))
                {
                    var url= s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" +file, DateTime.Now.AddMinutes(15), null);
                    if (string.IsNullOrEmpty(url))
                    {
                        return null;
                    }
                    else
                    {
                        using (var c=new HttpClient())
                        {
                            var rp = await c.GetAsync(url);
                            if (rp.IsSuccessStatusCode)
                            {
                                return await rp.Content.ReadAsStreamAsync();
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }
    }
}
