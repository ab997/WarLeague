# Command Layer Rules and Patterns

- Commands ONLY handle parameters, authorization checks, and call appropriate services - NO business logic in commands
- Services ALWAYS return BaseResult (or derived types) - extract result and use `ResultHelper.Stringify()` for Discord responses
- Authorization/permissions stay in commands or preconditions - NEVER in Core/Services layer
- Business validation belongs in services - use parameters like `canBypassValidation` to allow flexibility while enforcing rules
- Services validate business rules regardless of caller - ensures consistency when called from anywhere
