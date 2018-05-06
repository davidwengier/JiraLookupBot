using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Atlassian.Jira;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace JiraLookupBot
{
    internal class JiraDialog : IDialog<object>
    {
        private const string Pattern = @"((?<!([A-Za-z]{1,10})-?)[A-Z]+-\d+)";

        private readonly string ServerUrl = ConfigurationManager.AppSettings["JiraUrl"];
        private readonly string JiraUserName = ConfigurationManager.AppSettings["JiraUserName"];
        private readonly string JiraPassword = ConfigurationManager.AppSettings["JiraPassword"];

        public Task StartAsync(IDialogContext context)
        {
            try
            {
                context.Wait(MessageReceivedAsync);
            }
            catch (OperationCanceledException error)
            {
                return Task.FromCanceled(error.CancellationToken);
            }
            catch (Exception error)
            {
                return Task.FromException(error);
            }

            return Task.CompletedTask;
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            Jira j = Jira.CreateRestClient(ServerUrl, JiraUserName, JiraPassword);
            var message = await argument;
            foreach (Match match in Regex.Matches(message.Text, Pattern))
            {
                var issue = await j.Issues.GetIssueAsync(match.Value);

                var c = new HeroCard(issue.Key.Value, issue.Summary, "Status: " + issue.Status.Name + ", Assignee: " + issue.Assignee + ", Resolution: " + issue.Resolution.Name, null, new List<CardAction>
                {
                    new CardAction("openUrl", "Open " + issue.Key.Value, value: new Uri(new Uri(ServerUrl), "/browse/" + issue.Key.Value))
               });
                var msg = context.MakeMessage();
                msg.Attachments.Add(c.ToAttachment());
                await context.PostAsync(message);
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}