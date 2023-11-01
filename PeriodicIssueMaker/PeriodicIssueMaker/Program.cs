using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationBlocks.Data;
using PeriodicIssueMaker.Properties;

namespace PeriodicIssueMaker
{
    internal class Program
    {
        static int issueNumber = 0;
        static string emailBody = "";
        private static async Task Main(string[] args)
        {
            //program starts
            await SendEmail(Settings.Default.CreatorEmail, Settings.Default.RunMessage, Settings.Default.RunMessage);
            LogMessage(Settings.Default.ProgramStart, Settings.Default.ProgramStartAndEnd);
            using (StreamReader read = new StreamReader(Settings.Default.FilePath))
            {
                //checks if file is valid
                if (FileValid(Settings.Default.FilePath))
                {
                    try
                    {
                        ProcessIssueJobFile();
                        await SendEmail(Settings.Default.CreatorEmail, Settings.Default.Subject, emailBody);
                    }
                    catch (Exception ex)
                    {
                        await SendEmail(Settings.Default.CreatorEmail, Settings.Default.ErrorSubject, ex.Message);
                    }
                }
                //file is invalid
                else
                {
                    await SendEmail(Settings.Default.CreatorEmail, Settings.Default.ErrorSubject, LogMessage(Settings.Default.InvalidFileMessage, Settings.Default.ProgramStartAndEnd));
                }
            }
        }

        //helper method to call the stored procedure
        private static DataSet CallSproc(string[] input, DateTime dueDate)
        {
            DataSet ds = SqlHelper.ExecuteDataset(Settings.Default.IssueTrackerConnectionString, CommandType.StoredProcedure, "InsertBugAndComment",
                                new SqlParameter("@bg_short_desc", input[4]),
                                new SqlParameter("@bg_reported_user", input[5]),
                                new SqlParameter("@bg_priority", input[6]),
                                new SqlParameter("bg_category", input[7]),
                                new SqlParameter("@bg_project", input[8]),
                                new SqlParameter("@bg_status", input[9]),
                                new SqlParameter("bg_assigned_to_user", input[10]),
                                new SqlParameter("bg_org", input[11]),
                                new SqlParameter("Date_Due", dueDate),
                                new SqlParameter("bg_tags", input[13]),
                                new SqlParameter("@Comment", input[14]));
            return ds;
        }

        //helper method to send the email
        private static async Task SendEmail(string emailAddress, string Subject, string EmailMessage)
        {
            SendEmailArgs sendEmailArgs = new SendEmailArgs(emailAddress, Subject, EmailMessage);
            EmailHelper emailHelper = new EmailHelper();
            await emailHelper.SendEmail(sendEmailArgs);
        }
        //helper methods to compare the input date to current date and return a DateTime based off input string
        public static DateTime LastDay(int year, int month, string day)
        {
            DateTime dt;
            if (month < 12)
                dt = new DateTime(year, month + 1, 1);
            else
                dt = new DateTime(year + 1, 1, 1);
            dt = dt.AddDays(-1);
            switch (day)
            {
                case "monday":
                    while (dt.DayOfWeek != DayOfWeek.Monday) dt = dt.AddDays(-1);
                    return dt;
                case "tuesday":
                    while (dt.DayOfWeek != DayOfWeek.Tuesday) dt = dt.AddDays(-1);
                    return dt;
                case "wednesday":
                    while (dt.DayOfWeek != DayOfWeek.Wednesday) dt = dt.AddDays(-1);
                    return dt;
                case "thursday":
                    while (dt.DayOfWeek != DayOfWeek.Thursday) dt = dt.AddDays(-1);
                    return dt;
                case "friday":
                    while (dt.DayOfWeek != DayOfWeek.Friday) dt = dt.AddDays(-1);
                    return dt;
                case "saturday":
                    while (dt.DayOfWeek != DayOfWeek.Saturday) dt = dt.AddDays(-1);
                    return dt;
                case "sunday":
                    while (dt.DayOfWeek != DayOfWeek.Sunday) dt = dt.AddDays(-1);
                    return dt;
            }
            return dt;
        }
        public static DateTime FirstDay(int year, int month, string day)
        {
            DateTime dt;
            if (month < 12)
                dt = new DateTime(year, month + 1, 1);
            else
                dt = new DateTime(year + 1, 1, 1);
            dt = dt.AddDays(-1);
            switch (day)
            {
                case "monday":
                    while (dt.DayOfWeek != DayOfWeek.Monday) dt = dt.AddDays(1);
                    return dt;
                case "tuesday":
                    while (dt.DayOfWeek != DayOfWeek.Tuesday) dt = dt.AddDays(1);
                    return dt;
                case "wednesday":
                    while (dt.DayOfWeek != DayOfWeek.Wednesday) dt = dt.AddDays(1);
                    return dt;
                case "thursday":
                    while (dt.DayOfWeek != DayOfWeek.Thursday) dt = dt.AddDays(1);
                    return dt;
                case "friday":
                    while (dt.DayOfWeek != DayOfWeek.Friday) dt = dt.AddDays(1);
                    return dt;
                case "saturday":
                    while (dt.DayOfWeek != DayOfWeek.Saturday) dt = dt.AddDays(1);
                    return dt;
                case "sunday":
                    while (dt.DayOfWeek != DayOfWeek.Sunday) dt = dt.AddDays(1);
                    return dt;
            }
            return dt;
        }
        //helper method to make log messages
        private static string LogMessage(string message, int logLevel)
        {
            if (logLevel >= Settings.Default.LogLevel)
            {
                using (StreamWriter streamWriter = new StreamWriter(Settings.Default.LogPath, true))
                {
                    {
                        streamWriter.WriteLine(DateTime.Now + Settings.Default.Colon + message);
                    }
                }
            }
            Console.WriteLine(DateTime.Now + Settings.Default.Colon + message);
            return (DateTime.Now + Settings.Default.Colon + message);
        }
        //helper method to check if the job file is valid
        private static bool FileValid(string file)
        {
            var linesRead = File.ReadLines(Settings.Default.FilePath);
            foreach (var lineRead in linesRead)
            {
                lineRead.Trim();
                //line is a comment
                if (string.IsNullOrWhiteSpace(lineRead))
                {
                    continue;
                }
                //line is whitespace
                else if (lineRead.StartsWith(Settings.Default.CommentString)) 
                {
                    continue;
                }
                //line is proper command
                string[] inputArr = lineRead.Split(Settings.Default.StringSplitArg);
                if ((inputArr.Length != 16) || (!(IsNumeric(inputArr[0]) && IsNumeric(inputArr[3]) && IsNumeric(inputArr[5]) &&
                    IsNumeric(inputArr[6]) && IsNumeric(inputArr[7]) && IsNumeric(inputArr[8])
                    && IsNumeric(inputArr[9]) && IsNumeric(inputArr[10]) && IsNumeric(inputArr[11])
                    && IsValidEmail(inputArr[15]))))
                {
                    return false;
                }
            }
            return true;
        }
        //helper method to check if input is a number
        private static bool IsNumeric(string input)
        {
            return int.TryParse(input, out int result);
        }
        //helper method to check if an inputted string is a valid email address
        private static bool IsValidEmail(string email)
        {
            if (email.Contains(Settings.Default.StringSplitArg))
            {
                string[] emails = email.Split(Settings.Default.StringSplitArg);
                for (int i = 0; i < emails.Length; i++)
                {
                    if (!emails[i].Contains("@") || emails[i].IndexOf("@") < 0 || emails[i].IndexOf("@") >= emails[i].Length)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        //create and insert issue
        private static async void ProcessIssueJobFile()
        {
            //iterates through each line 
            //program already ensured the file has correct amount of elements by calling FileValid in the main method
            //so when the input is split into an array, there will be no out of bounds error.
            var linesRead = File.ReadLines(Settings.Default.FilePath);
            List<SendEmailArgs> emailList = new List<SendEmailArgs>();
            foreach (var lineRead in linesRead)
            {
                if (!(lineRead.StartsWith(Settings.Default.CommentString) && !string.IsNullOrWhiteSpace(lineRead)))
                {
                    string[] inputArr = lineRead.Split(Settings.Default.StringSplitArg);
                    string dueDayInput = inputArr[12];
                    string dueDay = dueDayInput.Substring(inputArr[12].IndexOf(" ") + 1).ToLower();
                    string prefix = dueDayInput.Substring(0, dueDayInput.IndexOf(" "));
                    string emailRecipients = Settings.Default.CreatorEmail;
                    DateTime dueDate;
                    if (prefix == Settings.Default.FirstPrefix)
                    {
                        dueDate = FirstDay(DateTime.Now.Year, DateTime.Now.Month, dueDay);
                    }
                    else
                    {
                        dueDate = LastDay(DateTime.Now.Year, DateTime.Now.Month, dueDay);
                    }
                    //bot starts to run
                    try
                    {
                        DataSet ds = CallSproc(inputArr, dueDate);
                        issueNumber = Convert.ToInt32(ds.Tables[0].Rows[0][0].ToString());
                        if (!(emailRecipients.Contains(inputArr[15])))
                        {
                            emailRecipients += Settings.Default.Semicolon + inputArr[15];
                        }
                        emailBody += Settings.Default.TestingConnection + issueNumber + Settings.Default.NewLine;
                        SendEmailArgs email = new SendEmailArgs(emailRecipients, Settings.Default.Subject + issueNumber, Settings.Default.TestingConnection + issueNumber);
                        emailList.Add(email);
                    }
                    //bot failed to insert issue
                    catch (SqlException)
                    {
                        await SendEmail(inputArr[15], Settings.Default.ErrorSubject, lineRead + Settings.Default.NewLine + inputArr[4]);
                    }
                    catch (Exception ex)
                    {
                        await SendEmail(inputArr[15], Settings.Default.ErrorSubject, ex + Settings.Default.NewLine + lineRead + Settings.Default.NewLine + inputArr[4]);
                    }
                }
            }
            await SendIndividualEmails(emailList);
        }
        //loop to asynchronously send all email notices individually
        private static async Task SendIndividualEmails(List<SendEmailArgs> emailList)
        {
            List<Task> listOfTasks = new List<Task>();
            foreach (SendEmailArgs email in emailList)
            {
                listOfTasks.Add(SendEmail(string.Join(";", email.ToAddresses), email.Subject, email.Body));
            }
            await Task.WhenAll(listOfTasks);
        }
    }
}

