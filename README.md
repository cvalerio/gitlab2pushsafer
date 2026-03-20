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

1. Go to your GitLab project → **Settings → Webhooks**
2. Set the URL to `https://your-host/ci-status`
3. Set the **Secret token** to match `GitLab__WebhookToken`
4. Enable **Pipeline events**
5. Save

## Security

All incoming requests must include the `X-Gitlab-Token` header matching the configured `GitLab__WebhookToken`. Requests with a missing or invalid token are rejected with `401 Unauthorized`.
