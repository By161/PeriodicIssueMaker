using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Exchange.WebServices.Data;
using Microsoft.Identity.Client;
using PeriodicIssueMaker.Properties;

namespace PeriodicIssueMaker
{
    public enum EmailBodyTypeEnum : byte
    {
        HTML = 0,
        Text = 1
    }
    public class EmailHelper
    {        
        public string EmailFromAddress { get; set; }


        public string SmtpAccount { get; set; }



        /// <summary>
        /// send email using the new MS auth (though still ews and not MS Graph yet)
        /// </summary>
        /// <param name="args">send email arguments</param>
        /// <returns>task which would contain result of email send request</returns>
        /// <remarks>TODO:  replace ews with MS Graph API</remarks>
        public async System.Threading.Tasks.Task SendEmail(SendEmailArgs args)
        {
            var ewsClient = new ExchangeService();
            
            var cca = ConfidentialClientApplicationBuilder
                        .Create(Settings.Default.NewAuthClientId)
                        .WithClientSecret(Settings.Default.NewAuthClientSecret)
                        .WithTenantId(Settings.Default.NewAuthTenantId)
                        .Build();
            var ewsScopes = new string[] { "https://outlook.office365.com/.default" };
            
            var authResult = await cca.AcquireTokenForClient(ewsScopes)
                .ExecuteAsync();
            
            ewsClient.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");
            ewsClient.Credentials = new OAuthCredentials(authResult.AccessToken);
            ewsClient.ImpersonatedUserId =
                new ImpersonatedUserId(ConnectingIdType.SmtpAddress, Settings.Default.NewAuthImpersonatedUserId);
            
            //Include x-anchormailbox header
            ewsClient.HttpHeaders.Add("X-AnchorMailbox", Settings.Default.NewAuthAnchorMailbox);
            var message = new EmailMessage(ewsClient);
            message.Subject = args.Subject;
            message.Body = args.Body;
            message.Body.BodyType = args.BodyType.HasValue ? (BodyType)args.BodyType : BodyType.HTML;

            message.From =
                new EmailAddress(new string[] { Settings.Default.NewAuthAnchorMailbox, EmailFromAddress, SmtpAccount }
                    .First(s => !string.IsNullOrWhiteSpace(s)));

            message.ToRecipients.AddRange(args.ToAddresses);
            message.Send();

        }

    }
}
