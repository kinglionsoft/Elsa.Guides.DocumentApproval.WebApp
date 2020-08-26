using System;
using System.Collections;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Timers.Activities;
using Elsa.Activities.Workflows.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Scripting.Liquid;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Guides.DocumentApproval.WebApp
{
    public class DocumentApprovalWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<ReceiveHttpRequest>(
                    x =>
                    {
                        x.Method = HttpMethod.Post.Method;
                        x.Path = new Uri("/documents", UriKind.Relative);
                        x.ReadContent = true;
                    }
                )
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "Document";
                        x.ValueExpression = new JavaScriptExpression<ExpandoObject>("lastResult().Body");
                    }
                )
                .Then<WriteLine>(
                    x =>
                    {
                        x.TextExpression = new JavaScriptExpression<string>("`Document received from ${Document.Author.Name}`");
                    }
                )
                .Then<WriteHttpResponse>(
                    x =>
                    {
                        x.Content = new LiteralExpression(
                            "<h1>Request for Approval Sent</h1><p>Your document has been received and will be reviewed shortly.</p>"
                        );
                        x.ContentType = "text/html";
                        x.StatusCode = HttpStatusCode.OK;
                        x.ResponseHeaders = new LiteralExpression("X-Powered-By=Elsa Workflows");
                    }
                )
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "Approved";
                        x.ValueExpression = new LiquidExpression<int>("0");
                    }
                )
                .Then<Fork>(x => x.Branches = new[] { "Jack", "Lucy" },
                    fork =>
                    {
                        fork.When("Jack")
                            .Then<WriteLine>(x =>
                            {
                                x.TextExpression = new JavaScriptExpression<string>(
                                    "`Jack approve url: \n ${signalUrl('Approve:Jack')}`"
                                );
                            })
                            .Then<Fork>(x =>
                                {
                                    x.Branches = new[] { "Approve", "Reject" };
                                },
                                fork2 =>
                                {
                                    fork2
                                        .When("Approve")
                                        .Then<Signaled>(x => x.Signal = new LiteralExpression("Approve:Jack"))
                                        .Then<SetVariable>(x =>
                                        {
                                            x.VariableName = "Approved";
                                            x.ValueExpression = new JavaScriptExpression<int>("Approved == 0 ? 1 : Approved");
                                        })
                                        .Then("JoinJack");

                                    fork2
                                        .When("Reject")
                                        .Then<Signaled>(x => x.Signal = new LiteralExpression("Reject:Jack"))
                                        .Then<SetVariable>(x =>
                                        {
                                            x.VariableName = "Approved";
                                            x.ValueExpression = new JavaScriptExpression<int>("2");
                                        })
                                        .Then("JoinJack");
                                },
                                "ForkJack"
                            )
                            .Join(x => x.Mode = Join.JoinMode.WaitAny, name: "JoinJack")
                            .Then("JoinJackLucy")
                            ;

                        fork.When("Lucy")
                            .Then<WriteLine>(x =>
                            {
                                x.TextExpression = new JavaScriptExpression<string>(
                                    "`Lucy approve url: \n ${signalUrl('Approve:Lucy')}`"
                                );
                            })
                            .Then<Fork>(x =>
                                {
                                    x.Branches = new[] { "Approve", "Reject" };
                                },
                                fork2 =>
                                {
                                    fork2
                                        .When("Approve")
                                        .Then<Signaled>(x => x.Signal = new LiteralExpression("Approve:Lucy"))
                                        .Then<SetVariable>(x =>
                                        {
                                            x.VariableName = "Approved";
                                            x.ValueExpression = new JavaScriptExpression<int>("Approved == 0 ? 1 : Approved");
                                        })
                                        .Then("JoinLucy");

                                    fork2
                                        .When("Reject")
                                        .Then<Signaled>(x => x.Signal = new LiteralExpression("Reject:Lucy"))
                                        .Then<SetVariable>(x =>
                                        {
                                            x.VariableName = "Approved";
                                            x.ValueExpression = new JavaScriptExpression<int>("2");
                                        })
                                        .Then("JoinLucy");
                                },
                                "ForkLucy"
                            )
                            .Join(x => x.Mode = Join.JoinMode.WaitAny, name: "JoinLucy")
                            .Then("JoinJackLucy")
                            ;
                    }
                )
                .Join(x => x.Mode = Join.JoinMode.WaitAll, name: "JoinJackLucy")
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Approved: ${Approved}`"), name: "AfterJoinJackLucy")
                .Then<IfElse>(
                    x => x.ConditionExpression = new JavaScriptExpression<bool>("Approved == 0"),
                    ifElse =>
                    {
                        ifElse
                            .When(OutcomeNames.True)
                            .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Document ${Document.Id} approved!`"));

                        ifElse
                            .When(OutcomeNames.False)
                            .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Document ${Document.Id} rejected!`"));
                    }
                );
        }
    }
}