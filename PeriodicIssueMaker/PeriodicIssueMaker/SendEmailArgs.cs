using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeriodicIssueMaker
{
    public class SendEmailArgs
    {
        public SendEmailArgs()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public SendEmailArgs(string toAddress, string subject, string body)
        {
            ToAddresses = toAddress.Contains(";") ? toAddress.Split(new char[] { ';' }).ToList<string>() : new List<string> { toAddress };
            Subject = subject;
            Body = body;
        }
        public ICollection<string> ToAddresses { get; set; }
        public ICollection<string> CcAddresses { get; set; }
        public ICollection<string> BccAddresses { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public EmailBodyTypeEnum? BodyType { get; set; }

    }
}
