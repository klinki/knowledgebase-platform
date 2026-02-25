---
description: C# (.NET) Development Conventions and Guidelines for Inviser
---

# C# (.NET) Development Conventions

These are the conventions that should be followed when working with C# code in the Inviser project. They focus on maintaining clean, consistent, and predictable .NET APIs and business logic.

## Naming Conventions
- **Classes/Methods/Properties**: `PascalCase` (`UserService`, `GetUserById`)
- **Private fields**: `_camelCase` with underscore prefix (`_context`, `_userManager`)
- **Parameters**: `camelCase` (`userId`, `createdAt`)
- **Interfaces**: Prefix with `I` (`IUserService`, `IRepository`)
- **Constants**: `PascalCase` (`MaxRetryCount`)

## Types and Nullability
- Enable nullable reference types: `<Nullable>enable</Nullable>`
- Use `?` for nullable types: `User?`, `string?`
- Prefer `string.Empty` over `""`
- Use `nameof()` instead of magic strings to prevent runtime errors.

## Using Statements
- Group imports: System, Third-party, Project-specific
- Use file-scoped namespaces when possible.
- Avoid fully qualified names inside a namespace.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Inviser.Controllers.Dto;
using Inviser.GeneratedModels;
using Inviser.Services;

namespace Inviser.Controllers;

// Code here
```

## Error Handling
- Use custom domain exceptions (`DomainException`, `EntityNotFoundException`).
- Return proper HTTP status codes in controllers.
- Use `ProblemDetails` for error responses.
- Avoid swallowing exceptions without logging.

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    if (!UserExists(id))
    {
        return NotFound();
    }
    throw;
}
```

## Controller Patterns
- Use attribute routing: `[Route("api/[controller]")]`
- Always inherit from `ControllerBase` for APIs.
- Use the `[ApiController]` attribute.
- Return `ActionResult<T>` for async methods.
- Use `[FromBody]`, `[FromRoute]` attributes explicitly.

## Dependency Injection
- Always use **Constructor Injection** for dependencies.
- Use interfaces to implement dependencies (Services, Repositories).
- Register services in `Startup.cs` / `Program.cs`.
