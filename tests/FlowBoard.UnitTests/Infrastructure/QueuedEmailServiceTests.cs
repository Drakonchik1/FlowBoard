using System.Linq.Expressions;
using FlowBoard.Infrastructure.Services;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace FlowBoard.UnitTests.Infrastructure;

public sealed class QueuedEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_EnqueuesHangfireJobWithoutBlockingOnSmtp()
    {
        var backgroundJobClient = new CapturingBackgroundJobClient();
        var service = new QueuedEmailService(backgroundJobClient);

        await service.SendEmailAsync("user@example.com", "Subject", "<p>Body</p>");

        Assert.Equal(1, backgroundJobClient.EnqueueCount);
    }

    private sealed class CapturingBackgroundJobClient : IBackgroundJobClient
    {
        public int EnqueueCount { get; private set; }

        public string Create(Job job, IState state)
        {
            EnqueueCount++;
            return "job-id";
        }

        public bool ChangeState(string jobId, IState state, string expectedState) => throw new NotSupportedException();

        public string Enqueue(Expression<Action> methodCall)
        {
            EnqueueCount++;
            return "job-id";
        }

        public string Enqueue(Expression<Func<Task>> methodCall)
        {
            EnqueueCount++;
            return "job-id";
        }

        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            EnqueueCount++;
            return "job-id";
        }

        public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        {
            EnqueueCount++;
            return "job-id";
        }

        public string Schedule(Expression<Action> methodCall, TimeSpan delay) => throw new NotSupportedException();

        public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay) => throw new NotSupportedException();

        public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) => throw new NotSupportedException();

        public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) => throw new NotSupportedException();

        public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt) => throw new NotSupportedException();

        public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt) => throw new NotSupportedException();

        public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt) => throw new NotSupportedException();

        public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt) => throw new NotSupportedException();

        public bool Delete(string jobId) => throw new NotSupportedException();

        public bool Requeue(string jobId) => throw new NotSupportedException();
    }
}
