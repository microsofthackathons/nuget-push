# TestServer

A server that tests:

1. AAD authentication
1. GitHub workflow run webhook

## Prerequisites

To run GitHub webhook:

1. If using the Action in the `microsofthackathons` GitHub organization, create GitHub PAT:
    1. Navigate to: https://github.com/settings/tokens
    1. Create a PAT using the `Generate new token` button. Make sure to add the `public_repo` scope
    1. Press `Enable SSO` then `Authorize` for `microsofthackathons` organization
    1. Save secret locally: `dotnet user-secrets set "GitHub:Token" "<GITHUB PAT>"`
1. Run the app: `dotnet run`
1. Configure GitHub's webhook
    1. Install ngrok: `scoop install ngrok`
    1. Acquire a public URL for the app: `ngrok http 5000`
    1. Navigate to: https://github.com/microsofthackathons/nuget-push/settings/hooks
    1. Set `Payload URL` to: `https://<NGROK ID>.ngrok.io/github-webhook`
    1. Set `Content type` to: `application/json`
1. Now kick off a GitHub Action: https://github.com/microsofthackathons/nuget-push/actions/workflows/main.yml