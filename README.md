# Limbo.Umbraco.UnusedMedia

Unused media dashboard for Umbraco 8.

## Installation

Via <a href="https://www.nuget.org/packages/Limbo.Umbraco.UnusedMedia/1.0.0-beta004" target="_blank">NuGet</a>:

```
dotnet add package Limbo.Umbraco.UnusedMedia --version 1.0.0-beta004
```

or:

```
Install-Package Limbo.Umbraco.UnusedMedia -Version 1.0.0-beta004
```

## Usage

Go to the **Content** section in Umbraco and select the **Unused media** tab 😎

## Configuration

The package has no real configuration, but various parts can be overriden in the code using dependency injection. 

### User List

The dashboard will feature two list of users - one for filtering by creator and another for filtering by writer (last updated by). Both lists are populated by the `UnusedMediaBackOfficeHelper.GetUsers` method.

The default implementation, as shown below, gets all users, filters out users that are not approved, and them orderds them all by name.

```csharp
protected virtual IEnumerable<IUser> GetUsers(HttpContextBase context, IUser currentUser) {
    return _userService
        .GetAll(0, int.MaxValue, out _)
        .Where(x => x.UserState == UserState.Active)
        .OrderBy(x => x.Name);
}
```

Say we wish to filter out all Limbo employees for non-admin users, we could inject our own helper class via DI, and then override the method with the following implementation:

```csharp
protected override IEnumerable<IUser> GetUsers(HttpContextBase context, IUser currentUser) {

    bool isAdmin = currentUser.Groups.Any(x => x.Alias == "admin");

    return _userService
        .GetAll(0, int.MaxValue, out _)
        .Where(x => x.UserState == UserState.Active && (isAdmin || !x.Email.EndsWith("@limbo.works")))
        .OrderBy(x => x.Name);

}
```
