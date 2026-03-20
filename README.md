# Gitlab2Pushsafer

A minimal ASP.NET Core web service that listens for GitLab pipeline webhook events and forwards them as push notifications via [Pushsafer](https://www.pushsafer.com).

## How it works

GitLab sends a `POST` request to `/ci-status` whenever a pipeline changes status. The service extracts the project name and pipeline status, then calls the Pushsafer API to deliver a push notification to your devices.

| Pipeline status | Notification color | Icon |
|---|---|---|
| `running` | Light blue | 4 |
| `success` | Green | 20 |
| `failed` | Red | 6 |
| anything else | Grey | 4 |

## Configuration

| Key | Description |
|---|---|
| `Pushsafer__AuthKey` | Your Pushsafer private or alias key |
| `GitLab__WebhookToken` | Secret token to validate incoming webhook requests |

Configuration can be provided via environment variables (recommended for Docker), `appsettings.json`, or `appsettings.local.json` (local development only, never published to the image).

## Running with Docker

```yaml
services:
  gitlab2pushsafer:
    image: docker.coders.pizza/gitlab2pushsafer:latest
    ports:
      - "8080:8080"
    environment:
      - Pushsafer__AuthKey=your_pushsafer_key
      - GitLab__WebhookToken=your_webhook_token
```

## Building the Docker image

Uses the [.NET SDK container support](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container) — no Dockerfile needed.

```bash
# amd64
dotnet publish --os linux --arch x64 /t:PublishContainer

# arm64
dotnet publish --os linux --arch arm64 /t:PublishContainer
```

## GitLab webhook setup

Webhooks can be configured at the **project** level (single repo) or at the **group** level (all projects in a group).

### Project-level

1. Open your GitLab project
2. Go to **Settings → Webhooks**
3. Click **Add new webhook**
4. Fill in the fields:
   - **URL**: `https://your-host/ci-status`
   - **Secret token**: the same value as `GitLab__WebhookToken`
5. Under **Trigger**, enable **Pipeline events**
6. Optionally enable **SSL verification** if your host has a valid certificate
7. Click **Add webhook**
8. Use the **Test → Pipeline events** button to send a test payload and verify the connection

### Group-level

1. Open your GitLab group
2. Go to **Settings → Webhooks**
3. Follow the same steps as above — the webhook will fire for all projects within the group

## Security

All incoming requests must include the `X-Gitlab-Token` header matching the configured `GitLab__WebhookToken`. Requests with a missing or invalid token are rejected with `401 Unauthorized`.
