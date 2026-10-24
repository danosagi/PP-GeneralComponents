# General Components

Plugins and Custom APIs for Dataverse that can be reused across projects. Each capability ships in its own solution under the **General Components** publisher (prefix `gen`).

## GeneralComponents.NotificationTemplates

Plugin behind the Custom API `gen_RenderNotificationTemplate` in solution **NotificationTemplatesAutomations**.

- **Request:** `TemplateCode` (String), `RecordId` (Guid), optional `InputValues_Optional` (String, JSON object such as `{"Reason":"Late submission"}`).
- **Response:** `Subject` (String) and `Body` (String).
- **Reads:** every read runs as the calling user. Callers need the Notification Template Renderer role or read access to the template tables.
- **Errors:** each failure message starts with an error code such as `NotificationTemplateRequiredValueMissing` and never contains data values.

Template rules, value formats and deployment steps are documented with the solution's implementation plan.

## Build and test

```
dotnet build GeneralComponents.sln -c Release
dotnet test GeneralComponents.sln
```

The plugin targets .NET Framework 4.6.2 and is strong-name signed with `GeneralComponents.NotificationTemplates.snk`. Register it in sandbox isolation with database storage.

## Versioning

Keep the assembly version at 1.0.x.y for in-place updates of the registered assembly. Changing the major or minor version registers a second assembly side by side and needs a planned migration of the plugin type and Custom API.
