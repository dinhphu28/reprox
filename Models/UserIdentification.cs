using System;
namespace RecordingProxy.Models
{
    public class UserIdentification
    {
        public UserIdentification()
        {
        }

        public UserIdentification(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        private string username;
        private string password;

        public string Username { get => username; set => username = value; }
        public string Password { get => password; set => password = value; }
    }
}
