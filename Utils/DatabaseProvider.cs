using System;
using System.Data;
using Npgsql;
namespace RecordingProxy.Utils
{
    public static class DatabaseProvider
    {
        public static string ReturnDataFromSQL(string DataConnect, string username, string password) //, string id) //
        {
            string kq = "";
            //string sql = $@" select * from public.get_name_recording('{username}','{password}','{id}');";
            string sql = $@" select* from public.check_username_password('{username}','{password}');";
            
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand();
                DataTable tbl = new DataTable();
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;



                using (Npgsql.NpgsqlConnection conn = new Npgsql.NpgsqlConnection(DataConnect))
                {
                    cmd.Connection = conn;
                    if (conn.State != ConnectionState.Open) conn.Open();
                    Npgsql.NpgsqlDataAdapter da = new Npgsql.NpgsqlDataAdapter(cmd);
                    da.Fill(tbl);
                    if (conn.State != ConnectionState.Closed) conn.Close();
                }
                if (tbl.Rows.Count > 0)
                {
                    kq = tbl.Rows[0]["data_"].ToString();
                }
                else
                {
                    //Log.Error($"ReturnDataFromSQL --> ERROR --> no result --> INPUT --> sql: {sql}");
                }
            }
            catch (Exception ex)
            {
                //Log.Error($"ReturnDataFromSQL --> ERROR --> EXCEPTION:  {ex.Message} --> INPUT --> sql: {sql} ");
            }
            return kq;
        }
    }
}
