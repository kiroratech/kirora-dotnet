# Official upstream OpenAPI comparison

Source: https://github.com/kiroratech/openapi
Upstream immutable commit: `80060d50bfa80f3c82b89a363a229c5b1c39506a`.

Classification: **different**. Operation counts: 184 pinned vs 184 upstream. Shared method/path pairs: 184.
API version: `1.0.0` → `1.0.0`; OpenAPI: `3.1.0` → `3.1.0`.

Pinned SHA-256: `b6b5b3a071f06b91b9d6ca271e10f4baca1e3dcb32d75303d83b67b1b7547653`
Upstream SHA-256: `b6bec37ca55ab8c0355d94fa608b21a2d8d100dbed8792abc6783c9c6b09bb4d`

License byte-identical: True.

## Endpoint-level changes


Shared operations below include direct changes and transitive referenced-schema changes. Structural filtering excludes descriptions/titles/examples, but preserves schema properties named description/summary. It is a conservative structural comparison, not a general JSON Schema equivalence or automatic breaking-change proof.

- `POST /v1/journeys/{id}/activate` (`activateJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignSnapshot`, `EventCondition`, `HtmlSnapshot`, `Journey`, `JourneyStep`, `JourneyTrigger`, `TemplateSnapshot`.
- `POST /v1/campaigns/{id}/archive` (`archiveCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `POST /v1/forms/{id}/archive` (`archiveForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`, `FormHiddenValue`, `FormTheme`.
- `POST /v1/campaigns/{id}/cancel` (`cancelCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `POST /v1/campaigns/{id}/ab-test/winner` (`chooseAbTestWinner`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `POST /v1/campaigns` (`createCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `ContactFieldConditionInput`, `DesignSnapshotInput`, `EventCondition`, `EventConditionInput`, `HtmlSnapshotInput`, `TemplateSnapshotInput`.
- `POST /v1/conversion-goals` (`createConversionGoal`): direct structural and/or referenced schema changes.
  Referenced schemas: `ConversionGoal`, `CreateConversionGoalInput`.
- `POST /v1/forms` (`createForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `CreateFormInput`, `Form`, `FormHiddenValue`, `FormHiddenValueInput`, `FormTheme`, `FormThemeInput`.
- `POST /v1/journeys` (`createJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `ContactFieldCondition`, `ContactFieldConditionInput`, `CreateJourneyInput`, `DesignSnapshot`, `DesignSnapshotInput`, `EventCondition`, `EventConditionInput`, `HtmlSnapshot`, `HtmlSnapshotInput`, `Journey`, `JourneyStep`, `JourneyTrigger`, `JourneyTriggerInput`, `TemplateSnapshot`, `TemplateSnapshotInput`.
- `POST /v1/segments` (`createSegment`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `ContactFieldCondition`, `ContactFieldConditionInput`, `EventCondition`, `EventConditionInput`.
- `POST /v1/shared-blocks` (`createSharedBlock`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `ContactFieldCondition`, `ContactFieldConditionInput`, `EventCondition`, `EventConditionInput`.
- `POST /v1/templates` (`createTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `ContactFieldCondition`, `ContactFieldConditionInput`, `CreateDesignTemplateInput`, `CreateHtmlTemplateInput`, `CreateTemplateInput`, `DesignTemplate`, `EventCondition`, `EventConditionInput`, `HtmlTemplate`, `Template`.
- `POST /v1/campaigns/{id}/duplicate` (`duplicateCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `POST /v1/journeys/{id}/duplicate` (`duplicateJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignSnapshot`, `EventCondition`, `HtmlSnapshot`, `Journey`, `JourneyStep`, `JourneyTrigger`, `TemplateSnapshot`.
- `POST /v1/templates/{id}/duplicate` (`duplicateTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignTemplate`, `EventCondition`, `HtmlTemplate`, `Template`.
- `POST /v1/journeys/{id}/end` (`endJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignSnapshot`, `EndedJourney`, `EventCondition`, `HtmlSnapshot`, `JourneyStep`, `JourneyTrigger`, `TemplateSnapshot`.
- `GET /v1/campaigns/{id}` (`getCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `GET /v1/forms/{id}` (`getForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`, `FormHiddenValue`, `FormTheme`.
- `POST /v1/reports/funnel` (`getFunnelReport`): direct structural and/or referenced schema changes.
  Referenced schemas: `FunnelDefinitionInput`.
- `GET /v1/journeys/{id}` (`getJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignSnapshot`, `EventCondition`, `HtmlSnapshot`, `Journey`, `JourneyStep`, `JourneyTrigger`, `TemplateSnapshot`.
- `GET /v1/segments/{id}` (`getSegment`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `EventCondition`.
- `GET /v1/segments/{id}/usage` (`getSegmentUsage`): prose/examples only.
- `GET /v1/shared-blocks/{id}` (`getSharedBlock`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `EventCondition`.
- `GET /v1/templates/{id}` (`getTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignTemplate`, `EventCondition`, `HtmlTemplate`, `Template`.
- `GET /v1/templates/gallery` (`getTemplateGallery`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `EventCondition`.
- `GET /v1/templates/{id}/versions/{version}` (`getTemplateVersion`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignTemplateVersion`, `EventCondition`, `HtmlTemplateVersion`, `TemplateVersion`.
- `POST /v1/contacts/identify` (`identifyContact`): direct structural and/or referenced schema changes.
  Referenced schemas: `IdentifyContactInput`, `TopicChoiceInput`.
- `POST /v1/contacts/batch` (`identifyContactsBatch`): direct structural and/or referenced schema changes.
  Referenced schemas: `IdentifyContactInput`, `TopicChoiceInput`.
- `GET /v1/campaigns` (`listCampaigns`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `EventCondition`.
- `GET /v1/conversion-goals` (`listConversionGoals`): direct structural and/or referenced schema changes.
  Referenced schemas: `ConversionGoal`.
- `GET /v1/journeys` (`listJourneys`): direct structural and/or referenced schema changes.
  Referenced schemas: `JourneyTrigger`.
- `GET /v1/segments` (`listSegments`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `EventCondition`.
- `GET /v1/templates` (`listTemplates`): direct structural and/or referenced schema changes.
  Referenced schemas: `TemplateListItem`.
- `POST /v1/journeys/{id}/pause` (`pauseJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignSnapshot`, `EventCondition`, `HtmlSnapshot`, `Journey`, `JourneyStep`, `JourneyTrigger`, `TemplateSnapshot`.
- `POST /v1/segments/preview` (`previewSegment`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeConditionInput`, `ContactFieldConditionInput`, `EventConditionInput`.
- `POST /v1/forms/{id}/publish` (`publishForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`, `FormHiddenValue`, `FormTheme`.
- `POST /v1/templates/{id}/publish` (`publishTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignTemplate`, `EventCondition`, `HtmlTemplate`, `Template`.
- `POST /v1/templates/{id}/render` (`renderTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `RenderTemplateInput`.
- `POST /v1/templates/render` (`renderTemplateContent`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeConditionInput`, `ContactFieldConditionInput`, `EventConditionInput`, `RenderDesignContentInput`, `RenderHtmlContentInput`, `RenderTemplateContentInput`.
- `POST /v1/templates/{id}/versions/{version}/restore` (`restoreTemplateVersion`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `ContactFieldCondition`, `DesignTemplate`, `EventCondition`, `HtmlTemplate`, `Template`.
- `POST /v1/campaigns/{id}/schedule` (`scheduleCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `POST /v1/campaigns/{id}/send` (`sendCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `POST /v1/templates/test-send` (`sendTemplateTest`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeConditionInput`, `ContactFieldConditionInput`, `EventConditionInput`, `SendDesignTemplateTestInput`, `SendHtmlTemplateTestInput`, `SendTemplateTestInput`.
- `POST /v1/transactional/messages` (`sendTransactionalMessage`): direct structural and/or referenced schema changes.
  Referenced schemas: `SendTransactionalInput`, `TransactionalDeliverFirstInput`, `TransactionalDeliverInput`.
- `POST /v1/transactional/messages/batch` (`sendTransactionalMessagesBatch`): direct structural and/or referenced schema changes.
  Referenced schemas: `SendTransactionalInput`, `TransactionalDeliverFirstInput`, `TransactionalDeliverInput`.
- `PUT /v1/contacts/{id}/topics/{topicId}` (`setContactTopic`): direct structural and/or referenced schema changes.
  Referenced schemas: `SetContactTopicInput`.
- `POST /v1/forms/{id}/submissions` (`submitForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `SubmitFormInput`.
- `POST /v1/campaigns/{id}/trigger` (`triggerCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `POST /v1/campaigns/{id}/unarchive` (`unarchiveCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `POST /v1/forms/{id}/unarchive` (`unarchiveForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`, `FormHiddenValue`, `FormTheme`.
- `POST /v1/forms/{id}/unpublish` (`unpublishForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`, `FormHiddenValue`, `FormTheme`.
- `POST /v1/campaigns/{id}/unschedule` (`unscheduleCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `EventCondition`.
- `PATCH /v1/campaigns/{id}` (`updateCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `CampaignAbTest`, `CampaignContent`, `ContactFieldCondition`, `ContactFieldConditionInput`, `DesignSnapshotInput`, `EventCondition`, `EventConditionInput`, `HtmlSnapshotInput`, `TemplateSnapshotInput`.
- `PATCH /v1/conversion-goals/{id}` (`updateConversionGoal`): direct structural and/or referenced schema changes.
  Referenced schemas: `ConversionGoal`.
- `PATCH /v1/forms/{id}` (`updateForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`, `FormHiddenValue`, `FormHiddenValueInput`, `FormTheme`, `FormThemeInput`, `UpdateFormInput`.
- `PATCH /v1/journeys/{id}` (`updateJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `ContactFieldCondition`, `ContactFieldConditionInput`, `DesignSnapshot`, `DesignSnapshotInput`, `EventCondition`, `EventConditionInput`, `HtmlSnapshot`, `HtmlSnapshotInput`, `Journey`, `JourneyStep`, `JourneyTrigger`, `JourneyTriggerInput`, `TemplateSnapshot`, `TemplateSnapshotInput`, `UpdateJourneyInput`.
- `PATCH /v1/segments/{id}` (`updateSegment`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `ContactFieldCondition`, `ContactFieldConditionInput`, `EventCondition`, `EventConditionInput`.
- `PATCH /v1/shared-blocks/{id}` (`updateSharedBlock`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `ContactFieldCondition`, `ContactFieldConditionInput`, `EventCondition`, `EventConditionInput`.
- `PATCH /v1/templates/{id}` (`updateTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `AttributeCondition`, `AttributeConditionInput`, `ContactFieldCondition`, `ContactFieldConditionInput`, `DesignTemplate`, `EventCondition`, `EventConditionInput`, `HtmlTemplate`, `Template`, `UpdateTemplateInput`.

## Schema-level changes

319 → 334 components; 17 added, 2 removed, 37 structurally modified, 4 modified only in prose/examples. Exact before/after values are in comparison.json.

- Added `CreateDesignTemplateInput`.
- Added `CreateHtmlTemplateInput`.
- Added `DesignSnapshot`.
- Added `DesignSnapshotInput`.
- Added `DesignTemplate`.
- Added `DesignTemplateVersion`.
- Added `FormHiddenValue`.
- Added `FormHiddenValueInput`.
- Added `HtmlSnapshot`.
- Added `HtmlSnapshotInput`.
- Added `HtmlTemplate`.
- Added `HtmlTemplateVersion`.
- Added `RenderDesignContentInput`.
- Added `RenderHtmlContentInput`.
- Added `SendDesignTemplateTestInput`.
- Added `SendHtmlTemplateTestInput`.
- Added `TopicChoiceInput`.
- Removed `TransactionalDeliverFirstInput`.
- Removed `TransactionalDeliverInput`.

### AttributeCondition

- `/properties/value/anyOf` (removed).
- `/properties/value/maxLength` (added).
- `/properties/value/type` (added).
- `/properties/values` (added).

### AttributeConditionInput

- `/properties/value/anyOf` (removed).
- `/properties/value/maxLength` (added).
- `/properties/value/type` (added).
- `/properties/values` (added).

### CampaignAbTest

- `/properties/versions/items/properties/email/properties/templateSnapshot/additionalProperties` (removed).
- `/properties/versions/items/properties/email/properties/templateSnapshot/oneOf` (added).
- `/properties/versions/items/properties/email/properties/templateSnapshot/properties` (removed).
- `/properties/versions/items/properties/email/properties/templateSnapshot/required` (removed).
- `/properties/versions/items/properties/email/properties/templateSnapshot/type` (removed).

### CampaignContent

- `/properties/email/properties/templateSnapshot/additionalProperties` (removed).
- `/properties/email/properties/templateSnapshot/oneOf` (added).
- `/properties/email/properties/templateSnapshot/properties` (removed).
- `/properties/email/properties/templateSnapshot/required` (removed).
- `/properties/email/properties/templateSnapshot/type` (removed).

### ContactFieldCondition

- `/properties/value/anyOf` (removed).
- `/properties/value/maxLength` (added).
- `/properties/value/type` (added).
- `/properties/values` (added).

### ContactFieldConditionInput

- `/properties/value/anyOf` (removed).
- `/properties/value/maxLength` (added).
- `/properties/value/type` (added).
- `/properties/values` (added).

### ConversionGoal

- `/properties/where/anyOf/0/items/properties/value/anyOf` (removed).
- `/properties/where/anyOf/0/items/properties/value/maxLength` (added).
- `/properties/where/anyOf/0/items/properties/value/type` (added).

### CreateConversionGoalInput

- `/properties/where/items/properties/value/anyOf` (removed).
- `/properties/where/items/properties/value/maxLength` (added).
- `/properties/where/items/properties/value/type` (added).

### CreateFormInput

- `/properties/hiddenFields/additionalProperties/$ref` (added).
- `/properties/hiddenFields/additionalProperties/anyOf` (removed).

### CreateJourneyInput

- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/anyOf` (removed).
- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/maxLength` (added).
- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/type` (added).
- `/properties/steps/items/oneOf/1/properties/event/properties/where/items/properties/value/anyOf` (removed).
- `/properties/steps/items/oneOf/1/properties/event/properties/where/items/properties/value/maxLength` (added).
- `/properties/steps/items/oneOf/1/properties/event/properties/where/items/properties/value/type` (added).
- `/properties/steps/items/oneOf/4/properties/where/items/properties/value/anyOf` (removed).
- `/properties/steps/items/oneOf/4/properties/where/items/properties/value/maxLength` (added).
- `/properties/steps/items/oneOf/4/properties/where/items/properties/value/type` (added).

### CreateTemplateInput

- `/additionalProperties` (removed).
- `/oneOf` (added).
- `/properties` (removed).
- `/required` (removed).
- `/type` (removed).

### EndedJourney

- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/anyOf` (removed).
- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/maxLength` (added).
- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/type` (added).

### EventCondition

- `/properties/where/items/properties/value/anyOf` (removed).
- `/properties/where/items/properties/value/maxLength` (added).
- `/properties/where/items/properties/value/type` (added).

### EventConditionInput

- `/properties/where/items/properties/value/anyOf` (removed).
- `/properties/where/items/properties/value/maxLength` (added).
- `/properties/where/items/properties/value/type` (added).

### Form

- `/properties/hiddenFields/additionalProperties/$ref` (added).
- `/properties/hiddenFields/additionalProperties/anyOf` (removed).

### FormTheme

- `/properties/inputs/properties/width/anyOf` (removed).
- `/properties/inputs/properties/width/enum` (added).
- `/properties/inputs/properties/width/type` (added).
- `/properties/inputs/properties/widthPx` (added).

### FormThemeInput

- `/properties/inputs/properties/width/anyOf` (removed).
- `/properties/inputs/properties/width/enum` (added).
- `/properties/inputs/properties/width/type` (added).
- `/properties/inputs/properties/widthPx` (added).

### FunnelDefinitionInput

- `/properties/steps/items/properties/where/items/properties/value/anyOf` (removed).
- `/properties/steps/items/properties/where/items/properties/value/maxLength` (added).
- `/properties/steps/items/properties/where/items/properties/value/type` (added).

### IdentifyContactInput

- `/properties/topics/additionalProperties/$ref` (added).
- `/properties/topics/additionalProperties/anyOf` (removed).

### Journey

- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/anyOf` (removed).
- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/maxLength` (added).
- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/type` (added).

### JourneyStep

- `/anyOf/1/properties/event/properties/where/items/properties/value/anyOf` (removed).
- `/anyOf/1/properties/event/properties/where/items/properties/value/maxLength` (added).
- `/anyOf/1/properties/event/properties/where/items/properties/value/type` (added).
- `/anyOf/5/properties/where/items/properties/value/anyOf` (removed).
- `/anyOf/5/properties/where/items/properties/value/maxLength` (added).
- `/anyOf/5/properties/where/items/properties/value/type` (added).

### JourneyTrigger

- `/oneOf/0/properties/where/items/properties/value/anyOf` (removed).
- `/oneOf/0/properties/where/items/properties/value/maxLength` (added).
- `/oneOf/0/properties/where/items/properties/value/type` (added).
- `/oneOf/3/properties/where/items/properties/value/anyOf` (removed).
- `/oneOf/3/properties/where/items/properties/value/maxLength` (added).
- `/oneOf/3/properties/where/items/properties/value/type` (added).
- `/oneOf/4/properties/where/items/properties/value/anyOf` (removed).
- `/oneOf/4/properties/where/items/properties/value/maxLength` (added).
- `/oneOf/4/properties/where/items/properties/value/type` (added).

### JourneyTriggerInput

- `/oneOf/0/properties/where/items/properties/value/anyOf` (removed).
- `/oneOf/0/properties/where/items/properties/value/maxLength` (added).
- `/oneOf/0/properties/where/items/properties/value/type` (added).
- `/oneOf/3/properties/where/items/properties/value/anyOf` (removed).
- `/oneOf/3/properties/where/items/properties/value/maxLength` (added).
- `/oneOf/3/properties/where/items/properties/value/type` (added).
- `/oneOf/4/properties/where/items/properties/value/anyOf` (removed).
- `/oneOf/4/properties/where/items/properties/value/maxLength` (added).
- `/oneOf/4/properties/where/items/properties/value/type` (added).

### RenderTemplateContentInput

- `/additionalProperties` (removed).
- `/oneOf` (added).
- `/properties` (removed).
- `/required` (removed).
- `/type` (removed).

### RenderTemplateInput

- `/properties/context` (added).
- `/properties/draft` (added).
- `/properties/version/anyOf` (removed).
- `/properties/version/default` (removed).
- `/properties/version/exclusiveMinimum` (added).
- `/properties/version/type` (added).

### SendTemplateTestInput

- `/additionalProperties` (removed).
- `/oneOf` (added).
- `/properties` (removed).
- `/required` (removed).
- `/type` (removed).

### SendTransactionalInput

- `/properties/deliver` (removed).
- `/properties/fallbackOrder` (added).

### SetContactTopicInput

- `/properties/channels` (added).
- `/properties/subscribed/anyOf` (removed).
- `/properties/subscribed/type` (added).
- `/required` (removed).

### SubmitFormInput

- `/properties/choices` (added).
- `/properties/fields/additionalProperties/anyOf` (removed).
- `/properties/fields/additionalProperties/maxLength` (added).
- `/properties/fields/additionalProperties/type` (added).

### Template

- `/additionalProperties` (removed).
- `/oneOf` (added).
- `/properties` (removed).
- `/required` (removed).
- `/type` (removed).

### TemplateListItem

- `/properties/kind` (added).
- `/required` (changed).

### TemplateSnapshot

- `/additionalProperties` (removed).
- `/oneOf` (added).
- `/properties` (removed).
- `/required` (removed).
- `/type` (removed).

### TemplateSnapshotInput

- `/additionalProperties` (removed).
- `/oneOf` (added).
- `/properties` (removed).
- `/required` (removed).
- `/type` (removed).

### TemplateVersion

- `/additionalProperties` (removed).
- `/oneOf` (added).
- `/properties` (removed).
- `/required` (removed).
- `/type` (removed).

### UpdateFormInput

- `/properties/hiddenFields/additionalProperties/$ref` (added).
- `/properties/hiddenFields/additionalProperties/anyOf` (removed).

### UpdateJourneyInput

- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/anyOf` (removed).
- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/maxLength` (added).
- `/properties/exitConditions/items/oneOf/0/properties/where/items/properties/value/type` (added).
- `/properties/steps/items/oneOf/1/properties/event/properties/where/items/properties/value/anyOf` (removed).
- `/properties/steps/items/oneOf/1/properties/event/properties/where/items/properties/value/maxLength` (added).
- `/properties/steps/items/oneOf/1/properties/event/properties/where/items/properties/value/type` (added).
- `/properties/steps/items/oneOf/4/properties/where/items/properties/value/anyOf` (removed).
- `/properties/steps/items/oneOf/4/properties/where/items/properties/value/maxLength` (added).
- `/properties/steps/items/oneOf/4/properties/where/items/properties/value/type` (added).

### UpdateTemplateInput

- `/properties/html` (added).

## Other structural changes

See otherStructuralChanges and otherComponentStructuralChanges in comparison.json. Documentation changes can still carry behavioral guidance and should be reviewed; this tool does not prove deployed server behavior.
