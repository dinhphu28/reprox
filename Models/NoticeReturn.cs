using System;
namespace RecordingProxy.Models
{
    public class NoticeReturn
    {
        public NoticeReturn()
        {
        }

        public NoticeReturn(string code, string description)
        {
            this.Code = code;
            this.Description = description;
        }

        private string code;
        private string description;

        public string Code { get => code; set => code = value; }
        public string Description { get => description; set => description = value; }
    }
}
