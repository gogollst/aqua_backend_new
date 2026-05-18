using Aqua.Data.Sessions;
using MassTransit;
using NHibernate.Linq;

namespace Aqua.Data.Inbox;

/// <summary>
/// MassTransit consume-filter that drops messages whose Id has already been processed by this consumer type.
/// </summary>
public sealed class InboxFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly ISessionScope _scope;
    public InboxFilter(ISessionScope scope) => _scope = scope;

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var consumerName = typeof(T).FullName ?? typeof(T).Name;
        var alreadyProcessed = await _scope.Session.Query<InboxMessage>()
            .AnyAsync(x => x.Id == context.MessageId!.Value && x.Consumer == consumerName, context.CancellationToken);

        if (alreadyProcessed) return;

        await next.Send(context);

        await _scope.Session.SaveAsync(new InboxMessage
        {
            TenantId = context.Headers.Get<string>("X-Aqua-Tenant") ?? "",
            Consumer = consumerName,
        }, context.CancellationToken);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("aqua-inbox");
}
