# OpenAPI Documentation with Scalar UI

## Overview
Replace the legacy Swashbuckle-based Swagger UI with the modern, built-in .NET OpenAPI support and the Scalar API reference interface. This improves performance and provides a more premium documentation experience.

## Primary Goal
To modernize the API documentation by leveraging native .NET 10 OpenAPI capabilities and integrating Scalar for a superior developer experience.

## Core Acceptance Criteria
1.  **Package Migration**: Remove `Swashbuckle.AspNetCore` and add `Microsoft.AspNetCore.OpenApi` along with `Scalar.AspNetCore`.
2.  **Configuration**: Update `Program.cs` to use `AddOpenApi()` and `MapScalarApiReference()`.
3.  **Enum Support**: Enums must be serialized as strings in the OpenAPI document and the Scalar UI.
4.  **Discovery**: Ensure the OpenAPI document is served correctly (typically at `/openapi/v1.json`).
5.  **UI Verification**: The Scalar UI should be accessible at `/scalar/v1` in the Development environment.
6.  **Clean Up**: Ensure no legacy Swagger-related code remains in `Program.cs`.

## Architectural Constraints
- Project targeting .NET 10.0.
- Must maintain compatibility with existing controllers and DTOs.
- Environment-specific (UI should only be active in Development).
- JSON serialization should default to string enums globally for consistency.

## Implementation Status
- [x] Initial Scaffolding
- [x] Remove Swashbuckle Packages [x]
- [x] Add .NET OpenAPI and Scalar Packages [x]
- [x] Update `Program.cs` Configuration (with String Enum support) [x]
- [x] Verify OpenAPI Document Generation [x]
- [x] Verify Scalar UI Integration [x]
- [x] Update README documentation [x]
