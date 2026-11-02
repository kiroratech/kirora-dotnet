# Phase 2 implementation inventory

35 new operations; the existing GET /v1/me public API is preserved. All paths and model contracts come from the unchanged pinned spec.

## Endpoints and public methods

### Contacts (20)

- `GET /v1/contacts` → `Contacts.ListAsync`; operation `listContacts`.
- `POST /v1/contacts/identify` → `Contacts.IdentifyAsync`; operation `identifyContact`.
- `POST /v1/contacts/batch` → `Contacts.IdentifyBatchAsync`; operation `identifyContactsBatch`.
- `GET /v1/contacts/{id}` → `Contacts.GetAsync`; operation `getContact`.
- `PATCH /v1/contacts/{id}` → `Contacts.UpdateAsync`; operation `updateContact`.
- `DELETE /v1/contacts/{id}` → `Contacts.DeleteAsync`; operation `deleteContact`.
- `POST /v1/contacts/delete` → `Contacts.DeleteBatchAsync`; operation `deleteContacts`.
- `POST /v1/contacts/{id}/merge` → `Contacts.MergeAsync`; operation `mergeContacts`.
- `GET /v1/contacts/{id}/consent-changes` → `Contacts.ListConsentChangesAsync`; operation `listContactConsentChanges`.
- `GET /v1/contacts/{id}/export` → `Contacts.ExportAsync`; operation `getContactExport`.
- `GET /v1/contacts/{id}/lists` → `Contacts.ListListsAsync`; operation `listContactLists`.
- `GET /v1/contacts/{id}/segments` → `Contacts.ListSegmentsAsync`; operation `listContactSegments`.
- `GET /v1/contacts/{id}/topics` → `Contacts.ListTopicsAsync`; operation `listContactTopics`.
- `PUT /v1/contacts/{id}/topics/{topicId}` → `Contacts.SetTopicAsync`; operation `setContactTopic`.
- `GET /v1/contacts/{id}/events` → `Contacts.ListEventsAsync`; operation `listContactEvents`.
- `GET /v1/contacts/{id}/journeys` → `Contacts.ListJourneysAsync`; operation `listContactJourneys`.
- `GET /v1/contacts/{id}/messages` → `Contacts.ListMessagesAsync`; operation `listContactMessages`.
- `GET /v1/contacts/{id}/installations` → `Contacts.ListInstallationsAsync`; operation `listPushInstallations`.
- `POST /v1/contacts/{id}/installations` → `Contacts.RegisterInstallationAsync`; operation `registerPushInstallation`.
- `DELETE /v1/contacts/{id}/installations/{externalInstallationId}` → `Contacts.DeleteInstallationAsync`; operation `deletePushInstallation`.

### Lists (8)

- `GET /v1/lists` → `Lists.ListAsync`; operation `listLists`.
- `POST /v1/lists` → `Lists.CreateAsync`; operation `createList`.
- `GET /v1/lists/{id}` → `Lists.GetAsync`; operation `getList`.
- `PATCH /v1/lists/{id}` → `Lists.UpdateAsync`; operation `updateList`.
- `DELETE /v1/lists/{id}` → `Lists.DeleteAsync`; operation `deleteList`.
- `POST /v1/lists/{id}/contacts` → `Lists.AddContactsAsync`; operation `addListContacts`.
- `POST /v1/lists/{id}/contacts/delete` → `Lists.RemoveContactsAsync`; operation `deleteListContacts`.
- `GET /v1/lists/{id}/usage` → `Lists.GetUsageAsync`; operation `getListUsage`.

### Events (2)

- `POST /v1/events/track` → `Events.TrackAsync`; operation `trackEvent`.
- `POST /v1/events/batch` → `Events.TrackBatchAsync`; operation `trackEventsBatch`.

### Transactional (3)

- `POST /v1/transactional/messages` → `Transactional.SendAsync`; operation `sendTransactionalMessage`.
- `POST /v1/transactional/messages/batch` → `Transactional.SendBatchAsync`; operation `sendTransactionalMessagesBatch`.
- `POST /v1/transactional/messages/{id}/cancel` → `Transactional.CancelAsync`; operation `cancelScheduledTransactionalMessage`.

### Messages (2)

- `GET /v1/messages/{id}` → `Messages.GetAsync`; operation `getMessage`.
- `GET /v1/messages/{id}/content` → `Messages.GetContentAsync`; operation `getMessageContent`.

## Files changed or added

- Modified: `README.md`
- New: `examples/Kirora.Examples/CoreResourceExamples.cs`
- New: `spec/core-operations.json`
- New: `spec/core-unions.json`
- New: `src/Kirora/Http/CoreJsonContext.g.cs`
- Modified: `src/Kirora/Http/KiroraHttpTransport.cs`
- New: `src/Kirora/Http/Operation.cs`
- New: `src/Kirora/Http/Operations.g.cs`
- Modified: `src/Kirora/Kirora.csproj`
- Modified: `src/Kirora/KiroraClient.cs`
- New: `src/Kirora/KiroraRequestOptions.cs`
- New: `src/Kirora/Models/CoreModels.g.cs`
- Modified: `src/Kirora/Models/FoundationModels.g.cs`
- New: `src/Kirora/Resources/ContactsResource.g.cs`
- New: `src/Kirora/Resources/EventsResource.g.cs`
- New: `src/Kirora/Resources/ListsResource.g.cs`
- New: `src/Kirora/Resources/MessagesResource.g.cs`
- New: `src/Kirora/Resources/ResourceHelpers.cs`
- New: `src/Kirora/Resources/TransactionalResource.g.cs`
- New: `src/Kirora/Serialization/ContractValidator.cs`
- New: `src/Kirora/Serialization/Optional.cs`
- New: `tests/Kirora.Tests/CoreBehaviorTests.cs`
- New: `tests/Kirora.Tests/CoreContractTests.cs`
- New: `tests/Kirora.Tests/Fixtures/core-examples.json`
- Modified: `tests/Kirora.Tests/Kirora.Tests.csproj`
- New: `tools/generate-core-fixtures.py`
- New: `tools/generate-core.py`
- Modified: `tools/generate-foundation-models.py`
- Modified: `src/Kirora/Resources/README.md`
- New: `docs/PHASE2.md` (this inventory).

## Decisions and limitations

- The pinned official specification is byte-for-byte unchanged. Derived metadata records operation policies and union schemas.
- New response types retain the complete documented data/meta envelopes. The existing GetMeAsync return type is unchanged.
- All 20 Contacts, 8 Lists and 3 Transactional spec operations are implemented. Events scope is track/batch; message detail and content are exposed under Messages, matching actual spec paths.
- Membership reads use Contacts.ListListsAsync and Contacts.ListAsync with ListId; no separate check endpoint exists.
- Event catalog/analytics operations (7), Messages listing/deliveries/export (5), and other resource areas are deferred to later phases. See the full pinned spec for their contracts.
- The original foundation FieldError model now omits absent optional null properties when serialized; its public property types remain unchanged.
- Local schema validation is a bounded subset for core request contracts; server business/authorization rules remain authoritative. No live API credentials were used.
- Examples compile as part of the console project. The default entry point remains a read-only /v1/me call; mutation examples require explicit application invocation.
- No publication, package version change or repository migration was performed.

## Next phase

Implement remaining Events and Messages methods, then Campaigns/Journeys and content resources; add opt-in integration coverage and release/API-compatibility checks.
