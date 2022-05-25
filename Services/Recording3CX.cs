using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RecordingProxy.Services
{
    public class Recording3CX
    {
        IConfiguration configuration;
        public Recording3CX(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<string> GetRecordingByIdAsync(int id)
        {
            string url = null;
            using (NpgsqlConnection conn = new NpgsqlConnection(configuration["ConnectionString3CX"]))
            {
                await conn.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand("select recording_url from recordings join recording_participant on fkid_recordings=recordings.id_recording where id_recording=:id order by id_participant limit 1", conn))
                {
                    command.Parameters.AddWithValue("id", id);
                    command.CommandTimeout = 0;
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            url=reader.GetString(0);
                    }
                }
            }
            return url;
        }
        public async Task<Stream> GetURLAsync(string file)
        {
            try
            {
                using (var c = new HttpClient())
                {
                    c.Timeout = TimeSpan.FromMinutes(10);
                    var rp = await c.GetAsync(configuration["Recording3CXUrl"] + Uri.EscapeDataString(file));
                    if (rp.IsSuccessStatusCode) return await rp.Content.ReadAsStreamAsync();
                    else return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
