using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NPOI.OpenXml4Net.OPC;
using RecordingProxy.Services;
using Serilog;

namespace RecordingProxy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddSingleton<Wasabi>();
            services.AddSingleton<Recording3CX>();

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "clientapp/build";
            });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        private void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            context.Response.Headers.Remove("transfer-encoding");
        }

        
        private void HandleRecording(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                
                var file = context.Request.Path.Value??"";

                Log.Information($"HandleRecording --> INPUT --> file: {file}");
                if (file.StartsWith("/"))
                {
                    file = file.Remove(0,1);
                }

                try
                {
                    if (string.IsNullOrEmpty(file)) {
                        context.Response.StatusCode = 400;
                    }
                    using (var c = new HttpClient())
                    {


                       
                        c.Timeout = TimeSpan.FromMinutes(10);
                        string url_stringee_api = $"{Configuration["Stringee:stringee_url"]}/recordfile/play?callId={file.Replace(".mp3", "")}&access_token={Configuration["Stringee:stringee_auth"]}";
                        HttpClient client = new HttpClient();
                        
                        var rp2 = await client.GetAsync(url_stringee_api);
                        //var input_file = get_recording_file_async(file);

                        //await context.Response.Body.WriteAsync(input_file.Result.data);
                        if (rp2.IsSuccessStatusCode)
                        {

                            //var byte_tmp = streamToByteArray(input_file.Result.data);

                            CopyFromTargetResponseHeaders(context, rp2);
                            context.Response.ContentType = "audio/wav";
                            context.Response.StatusCode = 200;
                            var content2 = await rp2.Content.ReadAsByteArrayAsync();
                            await context.Response.Body.WriteAsync(content2);
                        }
                        else
                        {
                            var link_tmp = Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file);
                            var link_tmp_2 = Configuration["Recording3CXUrl"] + file;
                            var link_get_final = Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file).Replace("%252F", "/").Replace("%2F", "/");


                            var rp = await c.GetAsync(link_get_final);
                            //var rp = await c.GetAsync(Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file));
                            if (rp.IsSuccessStatusCode)
                            {
                                CopyFromTargetResponseHeaders(context, rp);
                                context.Response.ContentType = "audio/wav";
                                context.Response.StatusCode = 200;
                                var content = await rp.Content.ReadAsByteArrayAsync();
                                await context.Response.Body.WriteAsync(content);
                            }
                            else
                            {

                                var config = new AmazonS3Config { ServiceURL = Configuration["Wasabi:endpoint"] };
                                using (IAmazonS3 s3Client = new AmazonS3Client(Configuration["Wasabi:accessKey"], Configuration["Wasabi:secretAccesskey"], config))
                                {
                                    //var url1 = s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" + file, DateTime.Now.AddMinutes(15), null);
                                    var url1 = s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" + file.Replace("%2F", "/"), DateTime.Now.AddMinutes(15), null);
                                    Log.Information($"HandleRecording --> Wasabi -> url1: {url1}");
                                    if (string.IsNullOrEmpty(url1))
                                    {
                                        context.Response.StatusCode = 400;
                                        return;
                                    }
                                    else
                                    {
                                        using (var c1 = new HttpClient())
                                        {
                                            var rp1 = await c.GetAsync(url1);
                                            if (rp1.IsSuccessStatusCode)
                                            {
                                                CopyFromTargetResponseHeaders(context, rp1);
                                                context.Response.ContentType = "audio/wav";
                                                context.Response.StatusCode = 200;
                                                var content1 = await rp1.Content.ReadAsByteArrayAsync();
                                                await context.Response.Body.WriteAsync(content1);
                                            }
                                            else
                                            {
                                                    context.Response.StatusCode = 400;
                                                    return;

                                            }
                                        }
                                    }
                                }
                              
                            }    
                           
                        }


                    
                        
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"HandleRecording --> INPUT --> file: {file} --> ERROR --> EXCEPTION --> Message: {ex.Message}");
                    context.Response.StatusCode = 400;
                    return;
                }

                context.Response.Redirect($"/Recordings?file={Uri.EscapeDataString(file)}");
            });
        }


        private void HandleRecordingCheckDownload(IApplicationBuilder app)
        {
            app.Run(async context =>
            {

                var file = context.Request.Path.Value ?? "";

                Log.Information($"HandleRecordingCheckDownload --> INPUT --> file: {file}");
                if (file.StartsWith("/"))
                {
                    file = file.Remove(0, 1);
                }

                try
                {
                    if (string.IsNullOrEmpty(file))
                    {
                        context.Response.StatusCode = 400;
                    }
                    using (var c = new HttpClient())
                    {

                        var link_tmp = Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file);
                        var link_tmp_2 = Configuration["Recording3CXUrl"] + file;
                        var link_get_final = Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file).Replace("%252F", "/").Replace("%2F", "/");
                      

                        var rp = await c.GetAsync(link_get_final);
                        //var rp = await c.GetAsync(Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file));
                        
                        if (rp.IsSuccessStatusCode)
                        {
                            CopyFromTargetResponseHeaders(context, rp);
                            context.Response.ContentType = "audio/wav";
                            context.Response.StatusCode = 200;
                            var content = await rp.Content.ReadAsByteArrayAsync();
                            await context.Response.Body.WriteAsync(content);
                        }
                        else
                        {

                            var config = new AmazonS3Config { ServiceURL = Configuration["Wasabi:endpoint"] };
                            using (IAmazonS3 s3Client = new AmazonS3Client(Configuration["Wasabi:accessKey"], Configuration["Wasabi:secretAccesskey"], config))
                            {
                                //var url1 = s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" + file, DateTime.Now.AddMinutes(15), null);
                                var url1 = s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" + file.Replace("%2F", "/"), DateTime.Now.AddMinutes(15), null);
                                Log.Information($"HandleRecordingCheckDownload --> Wasabi -> url1: {url1}");
                                if (string.IsNullOrEmpty(url1))
                                {
                                    context.Response.StatusCode = 400;
                                    return;
                                }
                                else
                                {
                                    using (var c1 = new HttpClient())
                                    {
                                        var rp1 = await c.GetAsync(url1);
                                        if (rp1.IsSuccessStatusCode)
                                        {
                                            CopyFromTargetResponseHeaders(context, rp1);
                                            context.Response.ContentType = "audio/wav";
                                            context.Response.StatusCode = 200;
                                            var content1 = await rp1.Content.ReadAsByteArrayAsync();
                                            await context.Response.Body.WriteAsync(content1);
                                        }
                                        else
                                        {
                                            context.Response.StatusCode = 400;
                                            return;

                                        }
                                    }
                                }
                            }

                        }






                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"HandleRecordingCheckDownload --> INPUT --> file: {file} --> ERROR --> EXCEPTION --> Message: {ex.Message}");
                    context.Response.StatusCode = 400;
                    return;
                }

                context.Response.Redirect($"/Recordings?file={Uri.EscapeDataString(file)}");
            });
        }


        private void HandleRecordingCheckHash(IApplicationBuilder app)
        {
            app.Run(async context =>
            {

                var file = context.Request.Path.Value ?? "";

                Log.Information($"HandleRecording --> INPUT --> file: {file}");
                if (file.StartsWith("/"))
                {
                    file = file.Remove(0, 1);
                }

                try
                {
                    if (string.IsNullOrEmpty(file))
                    {
                        context.Response.StatusCode = 400;
                    }
                    using (var c = new HttpClient())
                    {



                        c.Timeout = TimeSpan.FromMinutes(10);

                        HttpClient client = new HttpClient();
                        var rp2 = await client.GetAsync($"{Configuration["Stringee:stringee_url"]}/recordfile/play?callId={file.Replace(".mp3", "")}&access_token={Configuration["Stringee:stringee_auth"]}");
                        //var input_file = get_recording_file_async(file);

                        //await context.Response.Body.WriteAsync(input_file.Result.data);
                        if (rp2.IsSuccessStatusCode)
                        {

                            //var byte_tmp = streamToByteArray(input_file.Result.data);

                            CopyFromTargetResponseHeaders(context, rp2);
                            context.Response.ContentType = "audio/wav";
                            context.Response.StatusCode = 200;
                            var content2 = await rp2.Content.ReadAsByteArrayAsync();
                            await context.Response.Body.WriteAsync(content2);
                        }
                        else
                        {
                            var link_tmp = Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file);
                            var link_tmp_2 = Configuration["Recording3CXUrl"] + file;
                            var link_get_final = Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file).Replace("%252F", "/").Replace("%2F", "/");


                            var rp = await c.GetAsync(link_get_final);
                            //var rp = await c.GetAsync(Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file));
                            if (rp.IsSuccessStatusCode)
                            {
                                CopyFromTargetResponseHeaders(context, rp);
                                context.Response.ContentType = "audio/wav";
                                context.Response.StatusCode = 200;
                                var content = await rp.Content.ReadAsByteArrayAsync();
                                await context.Response.Body.WriteAsync(content);
                            }
                            else
                            {

                                var config = new AmazonS3Config { ServiceURL = Configuration["Wasabi:endpoint"] };
                                using (IAmazonS3 s3Client = new AmazonS3Client(Configuration["Wasabi:accessKey"], Configuration["Wasabi:secretAccesskey"], config))
                                {
                                    //var url1 = s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" + file, DateTime.Now.AddMinutes(15), null);
                                    var url1 = s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" + file.Replace("%2F", "/"), DateTime.Now.AddMinutes(15), null);
                                    Log.Information($"HandleRecording --> Wasabi -> url1: {url1}");
                                    if (string.IsNullOrEmpty(url1))
                                    {
                                        context.Response.StatusCode = 400;
                                        return;
                                    }
                                    else
                                    {
                                        using (var c1 = new HttpClient())
                                        {
                                            var rp1 = await c.GetAsync(url1);
                                            if (rp1.IsSuccessStatusCode)
                                            {
                                                CopyFromTargetResponseHeaders(context, rp1);
                                                context.Response.ContentType = "audio/wav";
                                                context.Response.StatusCode = 200;
                                                var content1 = await rp1.Content.ReadAsByteArrayAsync();
                                                await context.Response.Body.WriteAsync(content1);
                                            }
                                            else
                                            {
                                                context.Response.StatusCode = 400;
                                                return;

                                            }
                                        }
                                    }
                                }

                            }

                        }




                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"HandleRecording --> INPUT --> file: {file} --> ERROR --> EXCEPTION --> Message: {ex.Message}");
                    context.Response.StatusCode = 400;
                    return;
                }

                context.Response.Redirect($"/Recordings?file={Uri.EscapeDataString(file)}");
            });
        }


        private void HandleRecording_old(IApplicationBuilder app)
        {
            app.Run(async context =>
            {

                var file = context.Request.Path.Value ?? "";

                Log.Information($"HandleRecording --> INPUT --> file: {file}");
                if (file.StartsWith("/"))
                {
                    file = file.Remove(0, 1);
                }

                try
                {
                    if (string.IsNullOrEmpty(file))
                    {
                        context.Response.StatusCode = 400;
                    }
                    using (var c = new HttpClient())
                    {



                        c.Timeout = TimeSpan.FromMinutes(10);




                        var link_tmp = Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file);
                        var link_tmp_2 = Configuration["Recording3CXUrl"] + file;
                        var link_get_final = Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file).Replace("%252F", "/").Replace("%2F", "/");
                        var rp = await c.GetAsync(link_get_final);
                        //var rp = await c.GetAsync(Configuration["Recording3CXUrl"] + Uri.EscapeDataString(file));
                        if (rp.IsSuccessStatusCode)
                        {
                            CopyFromTargetResponseHeaders(context, rp);
                            context.Response.ContentType = "audio/wav";
                            context.Response.StatusCode = 200;
                            var content = await rp.Content.ReadAsByteArrayAsync();
                            await context.Response.Body.WriteAsync(content);
                        }
                        else
                        {
                            var config = new AmazonS3Config { ServiceURL = Configuration["Wasabi:endpoint"] };
                            using (IAmazonS3 s3Client = new AmazonS3Client(Configuration["Wasabi:accessKey"], Configuration["Wasabi:secretAccesskey"], config))
                            {
                                //var url1 = s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" + file, DateTime.Now.AddMinutes(15), null);
                                var url1 = s3Client.GeneratePreSignedURL(Configuration["Wasabi:bucketName"], Configuration["Wasabi:folderName"] + "/" + file.Replace("%2F", "/"), DateTime.Now.AddMinutes(15), null);
                                Log.Information($"HandleRecording --> Wasabi -> url1: {url1}");
                                if (string.IsNullOrEmpty(url1))
                                {
                                    context.Response.StatusCode = 400;
                                    return;
                                }
                                else
                                {
                                    using (var c1 = new HttpClient())
                                    {
                                        var rp1 = await c.GetAsync(url1);
                                        if (rp1.IsSuccessStatusCode)
                                        {
                                            CopyFromTargetResponseHeaders(context, rp1);
                                            context.Response.ContentType = "audio/wav";
                                            context.Response.StatusCode = 200;
                                            var content1 = await rp1.Content.ReadAsByteArrayAsync();
                                            await context.Response.Body.WriteAsync(content1);
                                        }
                                        else
                                        {
                                            HttpClient client = new HttpClient();
                                            var rp2 = await client.GetAsync($"{Configuration["Stringee:stringee_url"]}/recordfile/play?callId={file.Replace(".mp3", "")}&access_token={Configuration["Stringee:stringee_auth"]}");
                                            //var input_file = get_recording_file_async(file);

                                            //await context.Response.Body.WriteAsync(input_file.Result.data);
                                            if (rp2.IsSuccessStatusCode)
                                            {

                                                //var byte_tmp = streamToByteArray(input_file.Result.data);

                                                CopyFromTargetResponseHeaders(context, rp2);
                                                context.Response.ContentType = "audio/wav";
                                                context.Response.StatusCode = 200;
                                                var content2 = await rp2.Content.ReadAsByteArrayAsync();
                                                await context.Response.Body.WriteAsync(content2);
                                            }
                                            else
                                            {
                                                context.Response.StatusCode = 400;
                                                return;
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"HandleRecording --> INPUT --> file: {file} --> ERROR --> EXCEPTION --> Message: {ex.Message}");
                    context.Response.StatusCode = 400;
                    return;
                }

                context.Response.Redirect($"/Recordings?file={Uri.EscapeDataString(file)}");
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // sửa lại chỗ này nếu sử dụng text recording khác như recording3cx, recording
            //app.Map(Configuration["name_sub_recording"], HandleRecording);
            app.Map(Configuration["name_sub_recording_download"], HandleRecordingCheckDownload);

            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            //app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                    //pattern: "{controller}/{action=Index}/{id?}");
            });

            //app.UseFileServer(new FileServerOptions
            //{
            //    FileProvider = new PhysicalFileProvider(
            //        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
            //    RequestPath = "/wwwroot",
            //    EnableDefaultFiles = true
            //});

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "clientapp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
        private async Task<api_result<Stream>> get_recording_file_async(string call_id)
        {
            try
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync($"{Configuration["Stringee:stringee_url"]}/recordfile/play?callId={call_id}&access_token={Configuration["Stringee:stringee_auth"]}");
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return new api_result<Stream>
                    {
                        is_success = true,
                        data = await response.Content.ReadAsStreamAsync()
                    };
                }
                else
                {
                    return new api_result<Stream>
                    {
                        is_success = false,
                        error_message = $"Status code: {(int)response.StatusCode}. Content: {content}"
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new api_result<Stream>
                {
                    is_success = false,
                    error_message = ex.ToString()
                };
            }
        }

        private async Task<api_result<Stream>> get_recording_file_async_1(string call_id)
        {
            try
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync($"{Configuration["Stringee:stringee_url"]}/recordfile/play?callId={call_id}&access_token={Configuration["Stringee:stringee_auth"]}");
                var content = await response.Content.ReadAsStringAsync();
                //var a11 = await response.Content.ReadAsByteArrayAsync();
                if (response.IsSuccessStatusCode)
                {
                    return new api_result<Stream>
                    {
                        is_success = true,
                        data = await response.Content.ReadAsStreamAsync(),
                        content_file = await response.Content.ReadAsByteArrayAsync()

                    };
                }
                else
                {
                    return new api_result<Stream>
                    {
                        is_success = false,
                        error_message = $"Status code: {(int)response.StatusCode}. Content: {content}"
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new api_result<Stream>
                {
                    is_success = false,
                    error_message = ex.ToString()
                };
            }
        }



        private byte[] streamToByteArray(Stream input)
        {
            MemoryStream ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }

        public class api_result<T>
        {
            public bool is_success { get; set; }
            public string error_message { get; set; }
            public T data { get; set; }

            public byte[] content_file { get; set; }
        }
    }
}
