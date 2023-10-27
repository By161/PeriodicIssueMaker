using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeriodicIssueMaker
{
    internal class Email
    {
        private string recipient;
        private string subject;
        private string body; 
        public Email(string emailRecipient, string emailSubject, string emailBody) 
        {
            recipient = emailRecipient;
            subject = emailSubject;
            body = emailBody;
        }

        public void SetRecipient(string recipient)
        {
            this.recipient = recipient;
        }

        public string GetRecipient()
        {
            return recipient;
        }

        public void SetSubject(string subject)
        {
            this.subject = subject;
        }

        public string GetSubject() 
        {
            return subject;
        }

        public void SetBody(string body)
        {
            this.body = body;
        }

        public string GetBody()
        {
            return body;
        }
    }
}
