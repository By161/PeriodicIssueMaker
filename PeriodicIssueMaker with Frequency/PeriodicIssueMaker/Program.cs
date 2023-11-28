/* Periodic Issue Maker
 * Reads in a job file that lists issues to be created and calls the stored procedure to insert issues according to the job configuration file.
 * Author: Brandon Yuen
 */

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
        static Dictionary<string, int> DaysOfWeek = new Dictionary<string, int>() 
        { { "monday", 1 }, { "tuesday", 2 }, { "wednesday", 3 }, { "thursday", 4 }, { "friday", 5 }, { "saturday", 6 }, { "sunday", 0 }, };
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
                                new SqlParameter("@bg_reported_user", Settings.Default.BotUserId),
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
        private static async Task SendEmail(string emailAddress, string subject, string emailMessage)
        {
            SendEmailArgs sendEmailArgs = new SendEmailArgs(emailAddress, subject, emailMessage);
            EmailHelper emailHelper = new EmailHelper();
            await emailHelper.SendEmail(sendEmailArgs);
        }
        //helper methods to compare the input date to current date and return a DateTime based off input string
        //gets the date of the last specified day in the month
        public static DateTime LastDay(int year, int month, string day)
        {
            DateTime dt;
            dt = new DateTime(year, month, DateTime.DaysInMonth(year, month), System.Globalization.CultureInfo.CurrentCulture.Calendar);
            int daysOffset = Convert.ToInt32(dt.DayOfWeek) - DaysOfWeek[day];
            if (daysOffset < 0) daysOffset += 7;
            dt = dt.AddDays(-daysOffset);
            return dt;
        }
        //gets the date of the first specified day in the month
        public static DateTime FirstDay(int year, int month, string day)
        {
            DateTime dt;
            dt = new DateTime(year, month, 1, System.Globalization.CultureInfo.CurrentCulture.Calendar);
            int daysOffset = DaysOfWeek[day] - Convert.ToInt32(dt.DayOfWeek);
            if (daysOffset < 0) daysOffset += 7;
            dt = dt.AddDays(daysOffset);
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
                //line is whitespace
                if (string.IsNullOrWhiteSpace(lineRead))
                {
                    continue;
                }
                //line is a comment
                else if (lineRead.StartsWith(Settings.Default.CommentString)) 
                {
                    continue;
                }
                //line is proper command
                string[] inputArr = lineRead.Split(Settings.Default.StringSplitArg);
                if ((inputArr.Length != 15) || (!(IsInt(inputArr[0]) && IsInt(inputArr[3]) && IsInt(inputArr[5]) &&
                    IsInt(inputArr[6]) && IsInt(inputArr[7]) && IsInt(inputArr[8])
                    && IsInt(inputArr[9]) && IsInt(inputArr[10]) && IsInt(inputArr[11]))))
                {
                    return false;
                }
            }
            return true;
        }
        //helper method to check if input is a number
        private static bool IsInt(string input)
        {
            return int.TryParse(input, out int result);
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
                if (!string.IsNullOrWhiteSpace(lineRead) && !(lineRead.StartsWith(Settings.Default.CommentString)))
                {
                    string[] inputArr = lineRead.Split(Settings.Default.StringSplitArg);
                    string dueDay = inputArr[12].Substring(inputArr[12].IndexOf(" ") + 1).ToLower();
                    string prefix = inputArr[12].Substring(0, inputArr[12].IndexOf(" "));
                    
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
                        if (CheckMostRecentJobDate(inputArr[4], inputArr[2]))
                        {
                            DataSet ds = CallSproc(inputArr, dueDate);
                            issueNumber = Convert.ToInt32(ds.Tables[0].Rows[0][0].ToString());
                            emailBody += Settings.Default.TestingURL + issueNumber + Settings.Default.NewLine;
                            SendEmailArgs email = new SendEmailArgs(Settings.Default.CreatorEmail, Settings.Default.Subject + issueNumber, Settings.Default.TestingURL + issueNumber);
                            emailList.Add(email);
                        }
                    }
                    //bot failed to insert issue
                    catch (SqlException)
                    {
                        await SendEmail(Settings.Default.CreatorEmail, Settings.Default.ErrorSubject, lineRead + Settings.Default.NewLine + inputArr[4]);
                    }
                    catch (Exception ex)
                    {
                        await SendEmail(Settings.Default.CreatorEmail, Settings.Default.ErrorSubject, ex + Settings.Default.NewLine + lineRead + Settings.Default.NewLine + inputArr[4]);
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

        //return true if required amount of days from inputted frequency has passed today
        private static bool CheckMostRecentJobDate(string jobIssueDescription, string frequency)
        {
            DataSet ds = SqlHelper.ExecuteDataset(Settings.Default.IssueTrackerConnectionString, CommandType.StoredProcedure, "GetJobMostRecentCompletionTimeStamp",
                                new SqlParameter("@JobDescription", jobIssueDescription),
                                new SqlParameter("@ReportedBy", Settings.Default.BotUserId));
            if (ds.Tables.Count == 0) 
            {
                return true;
            }
            else
            {
                DateTime dateTime = (DateTime)ds.Tables[0].Rows[0][0];
                int DaysSinceLastJob = (int)(DateTime.Now - dateTime).TotalDays;
                frequency = frequency.ToLower();
                if (dateTime.Year == 1)
                {
                    return true;
                }
                switch (frequency)
                {
                    case "daily":
                        if (DateTime.Now.Day < dateTime.Day + 1)
                        {
                            return false;
                        }
                        else break;
                    case "weekly":
                        if (DateTime.Now.Day < dateTime.Day + 7)
                        {
                            return false;
                        }
                        else break;
                    case "monthly":
                        if (DateTime.Now.Month < dateTime.Month + 1)
                        {
                            return false;
                        }
                        else break;

                    case "yearly":
                        if (DateTime.Now.Year < dateTime.Year + 1)
                        {
                            return false;
                        }
                        else break;

                    case "biweekly":
                        if (DateTime.Now.Day < dateTime.Day + 14)
                        {
                            return false;
                        }
                        else break;

                    case "semi-monthly":
                        if (DateTime.Now.Day < dateTime.Day + (DateTime.DaysInMonth(dateTime.Year, dateTime.Month)/2))
                        {
                            return false;
                        }
                        else break;

                    case "quarterly":
                        if (DateTime.Now.Month < dateTime.Month + 4)
                        {
                            return false;
                        }
                        else break;
                    case "semi-annually":
                        if (dateTime.Month == 1 && DateTime.Now.Month == 6 && DateTime.Now.Day == 1)
                        {
                            return true;
                        }
                        else if (dateTime.Month == 6 && DateTime.Now.Month == 12 && DateTime.Now.Day == 1)
                        {
                            return true;
                        }
                        else break;

                    default: return true;
                }
                return true;
            }
        }
    }
}

