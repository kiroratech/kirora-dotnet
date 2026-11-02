using Kirora.Http;
using Kirora.Models;
using Kirora.Resources;

namespace Kirora;

/// <summary>A concurrent client using a caller-owned HttpClient.</summary>
public sealed class KiroraClient
{
    private readonly HttpClient _httpClient;
    private readonly KiroraOptions _options;
    private readonly KiroraHttpTransport _transport;

    /// <summary>Provides the documented configuration or response value.</summary>
    public ContactsResource Contacts { get; }
    /// <summary>Provides the documented configuration or response value.</summary>
    public ListsResource Lists { get; }
    /// <summary>Provides the documented configuration or response value.</summary>
    public EventsResource Events { get; }
    /// <summary>Provides the documented configuration or response value.</summary>
    public TransactionalResource Transactional { get; }
    /// <summary>Provides the documented configuration or response value.</summary>
    public MessagesResource Messages { get; }

    /// <summary>Accesses documented Assets operations.</summary>
    public AssetsResource Assets { get; }

    /// <summary>Accesses documented Attributes operations.</summary>
    public AttributesResource Attributes { get; }

    /// <summary>Accesses documented Campaigns operations.</summary>
    public CampaignsResource Campaigns { get; }

    /// <summary>Accesses documented ConversionGoals operations.</summary>
    public ConversionGoalsResource ConversionGoals { get; }

    /// <summary>Accesses documented EmbedSessions operations.</summary>
    public EmbedSessionsResource EmbedSessions { get; }

    /// <summary>Accesses documented Forms operations.</summary>
    public FormsResource Forms { get; }

    /// <summary>Accesses documented Journeys operations.</summary>
    public JourneysResource Journeys { get; }

    /// <summary>Accesses documented Me operations.</summary>
    public MeResource Me { get; }

    /// <summary>Accesses documented PushProjects operations.</summary>
    public PushProjectsResource PushProjects { get; }

    /// <summary>Accesses documented Reports operations.</summary>
    public ReportsResource Reports { get; }

    /// <summary>Accesses documented Segments operations.</summary>
    public SegmentsResource Segments { get; }

    /// <summary>Accesses documented Senders operations.</summary>
    public SendersResource Senders { get; }

    /// <summary>Accesses documented SendingDomains operations.</summary>
    public SendingDomainsResource SendingDomains { get; }

    /// <summary>Accesses documented SharedBlocks operations.</summary>
    public SharedBlocksResource SharedBlocks { get; }

    /// <summary>Accesses documented Suppressions operations.</summary>
    public SuppressionsResource Suppressions { get; }

    /// <summary>Accesses documented Tags operations.</summary>
    public TagsResource Tags { get; }

    /// <summary>Accesses documented Templates operations.</summary>
    public TemplatesResource Templates { get; }

    /// <summary>Accesses documented Topics operations.</summary>
    public TopicsResource Topics { get; }

    /// <summary>Accesses documented Webhooks operations.</summary>
    public WebhooksResource Webhooks { get; }

    /// <summary>Accesses documented Workspaces operations.</summary>
    public WorkspacesResource Workspaces { get; }

    /// <summary>Creates a client. Configure the supplied handler to disable automatic redirects.</summary>
    /// <remarks>The caller retains ownership of HttpClient and should reuse it. Its Timeout can further constrain RequestTimeout.</remarks>
    public KiroraClient(HttpClient httpClient, KiroraOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _options = options.Snapshot();
        _transport = new KiroraHttpTransport(httpClient, _options);
        Workspaces = new WorkspacesResource(_transport);
        Webhooks = new WebhooksResource(_transport);
        Topics = new TopicsResource(_transport);
        Templates = new TemplatesResource(_transport);
        Tags = new TagsResource(_transport);
        Suppressions = new SuppressionsResource(_transport);
        SharedBlocks = new SharedBlocksResource(_transport);
        SendingDomains = new SendingDomainsResource(_transport);
        Senders = new SendersResource(_transport);
        Segments = new SegmentsResource(_transport);
        Reports = new ReportsResource(_transport);
        PushProjects = new PushProjectsResource(_transport);
        Me = new MeResource(_transport);
        Journeys = new JourneysResource(_transport);
        Forms = new FormsResource(_transport);
        EmbedSessions = new EmbedSessionsResource(_transport);
        ConversionGoals = new ConversionGoalsResource(_transport);
        Campaigns = new CampaignsResource(_transport);
        Attributes = new AttributesResource(_transport);
        Assets = new AssetsResource(_transport);
        Contacts = new ContactsResource(_transport);
        Lists = new ListsResource(_transport);
        Events = new EventsResource(_transport);
        Transactional = new TransactionalResource(_transport);
        Messages = new MessagesResource(_transport);
    }

    /// <summary>Checks the calling key and returns its workspace and permissions, using GET /v1/me.</summary>
    public Task<ApiCaller> GetMeAsync(CancellationToken cancellationToken = default) =>
        _transport.GetDataAsync("/v1/me", KiroraJsonContext.Default.ApiCaller, cancellationToken);

    /// <summary>Returns an immutable tenant view sharing the same caller-owned transport.</summary>
    public KiroraClient ForWorkspace(string externalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        var options = _options.Snapshot();
        options.WorkspaceExternalId = externalId;
        return new KiroraClient(_httpClient, options);
    }
}
