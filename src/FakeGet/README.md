# FakeGet

This is a minimal NuGet client to prototype authentication.

Push a package with `fakeget push <package.nupkg>`

Enable interactive login with: `fakeget push <package.nupkg> --interactive`

Upload the package to a specific package source: `fakeget push <package.nupkg> --source https://api.nuget.org/v3/index.json`

List commands and options with:

```ps1
fakeget -h
fakeget push -h
```