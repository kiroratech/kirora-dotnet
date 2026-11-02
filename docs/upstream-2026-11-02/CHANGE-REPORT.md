# Official upstream OpenAPI comparison

Source: https://github.com/kiroratech/openapi
Upstream immutable commit: `30043317efd68ffceeab882019431e57b7463018`.

Classification: **different**. Operation counts: 183 pinned vs 184 upstream. Shared method/path pairs: 182.
API version: `1.0.0` → `1.0.0`; OpenAPI: `3.1.0` → `3.1.0`.

Pinned SHA-256: `4b31b357845dc09f89018879db8f238bb742df6a4c5f023b3210dded9919c8fa`
Upstream SHA-256: `b6b5b3a071f06b91b9d6ca271e10f4baca1e3dcb32d75303d83b67b1b7547653`

License byte-identical: True.

## Endpoint-level changes

- Added `GET /v1/brand-kit` (`getBrandKit`).
- Added `GET /v1/journeys/{id}/activation-check` (`getJourneyActivationCheck`).
- Removed `GET /v1/brand-theme` (`getBrandTheme`).

Shared operations below include direct changes and transitive referenced-schema changes. Structural filtering excludes descriptions/titles/examples, but preserves schema properties named description/summary. It is a conservative structural comparison, not a general JSON Schema equivalence or automatic breaking-change proof.

- `POST /v1/journeys/{id}/activate` (`activateJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/campaigns/{id}/archive` (`archiveCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/forms/{id}/archive` (`archiveForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`.
- `POST /v1/campaigns/{id}/cancel` (`cancelCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/workspaces/{externalId}/cancel-deletion` (`cancelWorkspaceDeletion`): prose/examples only.
- `POST /v1/campaigns/{id}/ab-test/winner` (`chooseAbTestWinner`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/campaigns` (`createCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EmailBlockStyle`, `EmailBlockStyleInput`, `EmailTheme`, `EmailThemeInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `SocialBlock`, `SocialBlockInput`, `TopicCondition`, `TopicConditionInput`.
- `POST /v1/forms` (`createForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `CreateFormInput`, `Form`.
- `POST /v1/journeys` (`createJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EmailBlockStyle`, `EmailBlockStyleInput`, `EmailTheme`, `EmailThemeInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `SocialBlock`, `SocialBlockInput`, `TopicCondition`, `TopicConditionInput`.
- `POST /v1/segments` (`createSegment`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `TopicCondition`, `TopicConditionInput`.
- `POST /v1/shared-blocks` (`createSharedBlock`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EmailBlockStyle`, `EmailBlockStyleInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `SocialBlock`, `SocialBlockInput`, `TopicCondition`, `TopicConditionInput`.
- `POST /v1/templates` (`createTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EmailBlockStyle`, `EmailBlockStyleInput`, `EmailTheme`, `EmailThemeInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `SocialBlock`, `SocialBlockInput`, `TopicCondition`, `TopicConditionInput`.
- `POST /v1/topics` (`createTopic`): direct structural and/or referenced schema changes.
  Referenced schemas: `Topic`.
- `POST /v1/workspaces` (`createWorkspace`): prose/examples only.
- `PUT /v1/events/catalog/{name}` (`defineEventName`): direct structural and/or referenced schema changes.
  Direct contract pointer: `/operation/x-plan-feature`.
- `DELETE /v1/lists/{id}` (`deleteList`): prose/examples only.
- `DELETE /v1/shared-blocks/{id}` (`deleteSharedBlock`): prose/examples only.
- `DELETE /v1/topics/{id}` (`deleteTopic`): prose/examples only.
- `DELETE /v1/workspaces/{externalId}` (`deleteWorkspace`): prose/examples only.
- `POST /v1/campaigns/{id}/duplicate` (`duplicateCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/journeys/{id}/duplicate` (`duplicateJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/templates/{id}/duplicate` (`duplicateTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/journeys/{id}/end` (`endJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `GET /v1/assets/{id}/usage` (`getAssetUsage`): direct structural and/or referenced schema changes.
  Referenced schemas: `AssetUsage`.
- `GET /v1/campaigns/{id}` (`getCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `GET /v1/campaigns/{id}/breakdown` (`getCampaignBreakdown`): prose/examples only.
- `GET /v1/campaigns/{id}/eligibility` (`getCampaignEligibility`): direct structural and/or referenced schema changes.
  Referenced schemas: `CampaignEligibility`.
- `GET /v1/campaigns/{id}/metrics` (`getCampaignMetrics`): direct structural and/or referenced schema changes.
  Referenced schemas: `CampaignMessageCounts`, `CampaignMetrics`, `DeliverabilityHealth`, `DeliverabilityLimits`.
- `GET /v1/contacts/{id}` (`getContact`): prose/examples only.
- `GET /v1/contacts/{id}/export` (`getContactExport`): prose/examples only.
- `GET /v1/events/summary` (`getEventSummary`): direct structural and/or referenced schema changes.
  Referenced schemas: `EventSummary`.
- `GET /v1/forms/{id}` (`getForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`.
- `GET /v1/forms/{id}/metrics` (`getFormMetrics`): prose/examples only.
- `POST /v1/reports/funnel` (`getFunnelReport`): prose/examples only.
- `GET /v1/journeys/{id}` (`getJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `GET /v1/journeys/{id}/metrics` (`getJourneyMetrics`): direct structural and/or referenced schema changes.
  Referenced schemas: `JourneyEmailCounts`.
- `GET /v1/journeys/{id}/step-metrics` (`getJourneyStepMetrics`): direct structural and/or referenced schema changes.
  Referenced schemas: `JourneyEmailCounts`.
- `GET /v1/lists/{id}/usage` (`getListUsage`): direct structural and/or referenced schema changes.
  Referenced schemas: `ListUsage`.
- `GET /v1/reports/overview` (`getOverviewReport`): direct structural and/or referenced schema changes.
  Referenced schemas: `DeliverabilityHealth`, `DeliverabilityLimits`, `Overview`, `OverviewCounts`.
- `GET /v1/segments/{id}` (`getSegment`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `TopicCondition`.
- `GET /v1/shared-blocks/{id}` (`getSharedBlock`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `GET /v1/shared-blocks/{id}/usage` (`getSharedBlockUsage`): direct structural and/or referenced schema changes.
  Referenced schemas: `SharedBlockUsage`.
- `GET /v1/templates/{id}` (`getTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `GET /v1/templates/gallery` (`getTemplateGallery`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `GET /v1/templates/{id}/versions/{version}` (`getTemplateVersion`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TemplateVersion`, `TopicCondition`.
- `GET /v1/topics/{id}` (`getTopic`): direct structural and/or referenced schema changes.
  Referenced schemas: `Topic`.
- `GET /v1/reports/transactional` (`getTransactionalReport`): prose/examples only.
- `GET /v1/workspaces/{externalId}` (`getWorkspace`): prose/examples only.
- `POST /v1/contacts/identify` (`identifyContact`): prose/examples only.
- `POST /v1/contacts/batch` (`identifyContactsBatch`): prose/examples only.
- `GET /v1/reports/campaigns` (`listCampaignReports`): prose/examples only.
- `GET /v1/campaigns` (`listCampaigns`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `TopicCondition`.
- `GET /v1/contacts/{id}/events` (`listContactEvents`): direct structural and/or referenced schema changes.
  Referenced schemas: `Event`.
- `GET /v1/contacts` (`listContacts`): prose/examples only.
- `GET /v1/events` (`listEvents`): direct structural and/or referenced schema changes.
  Referenced schemas: `Event`.
- `GET /v1/forms` (`listForms`): direct structural and/or referenced schema changes.
  Referenced schemas: `FormListItem`.
- `GET /v1/messages/{id}/deliveries` (`listMessageDeliveries`): prose/examples only.
- `GET /v1/segments/{id}/contacts` (`listSegmentContacts`): prose/examples only.
- `GET /v1/segments/{id}/membership-changes` (`listSegmentMembershipChanges`): prose/examples only.
- `GET /v1/segments` (`listSegments`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `TopicCondition`.
- `GET /v1/topics` (`listTopics`): direct structural and/or referenced schema changes.
  Referenced schemas: `Topic`.
- `GET /v1/webhooks/deliveries` (`listWebhookDeliveries`): prose/examples only.
- `GET /v1/webhooks/events` (`listWebhookEvents`): prose/examples only.
- `GET /v1/workspaces` (`listWorkspaces`): prose/examples only.
- `POST /v1/contacts/{id}/merge` (`mergeContacts`): prose/examples only.
- `POST /v1/journeys/{id}/pause` (`pauseJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/segments/preview` (`previewSegment`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldConditionInput`, `EventConditionInput`, `SegmentRuleGroupInput`, `SegmentRulesInput`, `TopicConditionInput`.
- `POST /v1/forms/{id}/publish` (`publishForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`.
- `POST /v1/templates/{id}/publish` (`publishTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/webhooks/deliveries/{deliveryId}/redeliver` (`redeliverWebhookDelivery`): prose/examples only.
- `POST /v1/templates/render` (`renderTemplateContent`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldConditionInput`, `EmailBlockStyleInput`, `EmailThemeInput`, `EventConditionInput`, `SegmentRuleGroupInput`, `SegmentRulesInput`, `SocialBlockInput`, `TopicConditionInput`.
- `POST /v1/templates/{id}/versions/{version}/restore` (`restoreTemplateVersion`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/workspaces/{externalId}/resume` (`resumeWorkspace`): prose/examples only.
- `POST /v1/campaigns/{id}/schedule` (`scheduleCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/campaigns/{id}/send` (`sendCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/templates/test-send` (`sendTemplateTest`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldConditionInput`, `EmailBlockStyleInput`, `EmailThemeInput`, `EventConditionInput`, `SegmentRuleGroupInput`, `SegmentRulesInput`, `SocialBlockInput`, `TopicConditionInput`.
- `POST /v1/transactional/messages` (`sendTransactionalMessage`): direct structural and/or referenced schema changes.
  Referenced schemas: `SendTransactionalInput`, `TransactionalDeliverFirstInput`, `TransactionalDeliverInput`, `TransactionalMessage`, `TransactionalSendResult`, `TransactionalSkippedChannel`.
- `POST /v1/transactional/messages/batch` (`sendTransactionalMessagesBatch`): direct structural and/or referenced schema changes.
  Referenced schemas: `SendTransactionalInput`, `SendTransactionalMessagesBatchResult`, `TransactionalDeliverFirstInput`, `TransactionalDeliverInput`, `TransactionalMessage`, `TransactionalSkippedChannel`.
- `POST /v1/journeys/{id}/simulate` (`simulateJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `JourneyActivationIssue`, `JourneySimulation`.
- `POST /v1/workspaces/{externalId}/suspend` (`suspendWorkspace`): prose/examples only.
- `POST /v1/events/track` (`trackEvent`): direct structural and/or referenced schema changes.
  Referenced schemas: `TrackedEvent`.
  Direct contract pointer: `/operation/x-plan-feature`.
- `POST /v1/events/batch` (`trackEventsBatch`): direct structural and/or referenced schema changes.
  Referenced schemas: `TrackedEvent`.
  Direct contract pointer: `/operation/x-plan-feature`.
- `POST /v1/campaigns/{id}/trigger` (`triggerCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/campaigns/{id}/unarchive` (`unarchiveCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `POST /v1/forms/{id}/unarchive` (`unarchiveForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`.
- `POST /v1/forms/{id}/unpublish` (`unpublishForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`.
- `POST /v1/campaigns/{id}/unschedule` (`unscheduleCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `EmailBlockStyle`, `EmailTheme`, `EventCondition`, `SegmentRuleGroup`, `SegmentRules`, `SocialBlock`, `TopicCondition`.
- `PATCH /v1/campaigns/{id}` (`updateCampaign`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EmailBlockStyle`, `EmailBlockStyleInput`, `EmailTheme`, `EmailThemeInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `SocialBlock`, `SocialBlockInput`, `TopicCondition`, `TopicConditionInput`.
- `PATCH /v1/contacts/{id}` (`updateContact`): prose/examples only.
- `PATCH /v1/forms/{id}` (`updateForm`): direct structural and/or referenced schema changes.
  Referenced schemas: `Form`, `UpdateFormInput`.
- `PATCH /v1/journeys/{id}` (`updateJourney`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EmailBlockStyle`, `EmailBlockStyleInput`, `EmailTheme`, `EmailThemeInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `SocialBlock`, `SocialBlockInput`, `TopicCondition`, `TopicConditionInput`.
- `PATCH /v1/segments/{id}` (`updateSegment`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `TopicCondition`, `TopicConditionInput`.
- `PATCH /v1/shared-blocks/{id}` (`updateSharedBlock`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EmailBlockStyle`, `EmailBlockStyleInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `SocialBlock`, `SocialBlockInput`, `TopicCondition`, `TopicConditionInput`.
- `PATCH /v1/templates/{id}` (`updateTemplate`): direct structural and/or referenced schema changes.
  Referenced schemas: `ContactFieldCondition`, `ContactFieldConditionInput`, `EmailBlockStyle`, `EmailBlockStyleInput`, `EmailTheme`, `EmailThemeInput`, `EventCondition`, `EventConditionInput`, `SegmentRuleGroup`, `SegmentRuleGroupInput`, `SegmentRules`, `SegmentRulesInput`, `SocialBlock`, `SocialBlockInput`, `TopicCondition`, `TopicConditionInput`.
- `PATCH /v1/topics/{id}` (`updateTopic`): direct structural and/or referenced schema changes.
  Referenced schemas: `Topic`.
- `PATCH /v1/workspaces/{externalId}` (`updateWorkspace`): prose/examples only.

## Schema-level changes

309 → 319 components; 10 added, 0 removed, 37 structurally modified, 14 modified only in prose/examples. Exact before/after values are in comparison.json.

- Added `BrandKit`.
- Added `DeliverabilityHealth`.
- Added `DeliverabilityLimits`.
- Added `JourneyActivationCheck`.
- Added `JourneyActivationIssue`.
- Added `TopicCondition`.
- Added `TopicConditionInput`.
- Added `TransactionalDeliverFirstInput`.
- Added `TransactionalDeliverInput`.
- Added `TransactionalSkippedChannel`.

### AssetUsage

- `/properties/brandKit` (added).
- `/properties/brandTheme` (removed).
- `/required/6` (changed).

### CampaignEligibility

- `/properties/abTestSplit` (added).
- `/properties/abTestWarnings/items/enum` (changed).
- `/required` (changed).

### CampaignMessageCounts

- `/properties/simulated` (added).
- `/required` (changed).

### CampaignMetrics

- `/properties/deliverabilityHealth` (added).
- `/required` (changed).

### ContactFieldCondition

- `/properties/field/enum` (changed).

### ContactFieldConditionInput

- `/properties/field/enum` (changed).

### CreateFormInput

- `/properties/notifyUserIds` (added).

### EmailBlockStyle

- `/properties/mobilePadding` (added).

### EmailBlockStyleInput

- `/properties/mobilePadding` (added).

### EmailTheme

- `/properties/baseFontSize` (added).
- `/properties/buttonTextColor` (added).
- `/properties/headingColor` (added).

### EmailThemeInput

- `/properties/baseFontSize` (added).
- `/properties/buttonTextColor` (added).
- `/properties/headingColor` (added).

### Event

- `/oneOf/0/properties/expiresAt` (added).
- `/oneOf/0/required` (changed).

### EventCondition

- `/properties/where/items/properties/operator/enum` (changed).

### EventConditionInput

- `/properties/where/items/properties/operator/enum` (changed).

### EventSummary

- `/properties/sample/properties/properties/items/properties/types/items/enum` (changed).

### Form

- `/properties/notifyUserIds` (added).
- `/required` (changed).

### FormListItem

- `/properties/notifyUserIds` (added).
- `/required` (changed).

### JourneyEmailCounts

- `/properties/simulated` (added).
- `/required` (changed).

### JourneySimulation

- `/properties/path/items/properties/issues` (added).

### ListUsage

- `/properties/goals` (added).
- `/properties/sharedBlocks` (added).
- `/properties/templates` (added).
- `/required` (changed).

### Overview

- `/properties/deliverabilityHealth/$ref` (added).
- `/properties/deliverabilityHealth/additionalProperties` (removed).
- `/properties/deliverabilityHealth/properties` (removed).
- `/properties/deliverabilityHealth/required` (removed).
- `/properties/deliverabilityHealth/type` (removed).

### OverviewCounts

- `/properties/failedBeforeSending` (added).
- `/properties/simulated` (added).
- `/required` (changed).

### SegmentRuleGroup

- `/anyOf/0/properties/all/items/anyOf` (changed).
- `/anyOf/1/properties/any/items/anyOf` (changed).

### SegmentRuleGroupInput

- `/anyOf/0/properties/all/items/anyOf` (changed).
- `/anyOf/1/properties/any/items/anyOf` (changed).

### SegmentRules

- `/anyOf/0/properties/all/items/anyOf/0/anyOf` (changed).
- `/anyOf/1/properties/any/items/anyOf/0/anyOf` (changed).

### SegmentRulesInput

- `/anyOf/0/properties/all/items/anyOf/0/anyOf` (changed).
- `/anyOf/1/properties/any/items/anyOf/0/anyOf` (changed).

### SendTransactionalInput

- `/properties/deliver/$ref` (added).
- `/properties/deliver/enum` (removed).
- `/properties/deliver/type` (removed).

### SendTransactionalMessagesBatchResult

- `/properties/results/items/anyOf/0/properties/skippedChannels` (added).
- `/properties/results/items/anyOf/0/required` (changed).

### SharedBlockUsage

- `/properties/brandKit` (added).
- `/required` (changed).

### SocialBlock

- `/required` (changed).

### SocialBlockInput

- `/required` (changed).

### TemplateVersion

- `/properties/brandKit` (added).
- `/properties/brandTheme` (removed).
- `/required/10` (changed).

### Topic

- `/properties/counts` (added).
- `/required` (changed).

### TrackedEvent

- `/properties/expiresAt` (added).
- `/required` (changed).

### TransactionalMessage

- `/properties/fallbackFor` (added).
- `/required` (changed).

### TransactionalSendResult

- `/properties/skippedChannels` (added).
- `/required` (changed).

### UpdateFormInput

- `/properties/notifyUserIds` (added).

## Other structural changes

See otherStructuralChanges and otherComponentStructuralChanges in comparison.json. Documentation changes can still carry behavioral guidance and should be reviewed; this tool does not prove deployed server behavior.
