## Summary

- Describe the main change in one or two bullet points.
- Link any related issue or discussion.

## Checklist

- [ ] My PR title follows Conventional Commits (for example: `feat: add health endpoint`)
- [ ] My commits follow Conventional Commits
- [ ] I updated documentation when needed
- [ ] I added or updated tests when needed
- [ ] I ran the relevant local validation commands

## Validation

```powershell
dotnet restore .\Dotnetstore.MinimalApi.slnx
dotnet build .\Dotnetstore.MinimalApi.slnx --configuration Release --no-restore
dotnet run --project .\tests\Dotnetstore.MinimalApi.Api.WebApi.Tests\Dotnetstore.MinimalApi.Api.WebApi.Tests.csproj --configuration Release --no-build
```

## Screenshots or logs

Add screenshots, API samples, workflow links, or logs if they help reviewers.

