# TransformationRule

Defines a single transformation instruction applied to API requests or responses during gateway processing. Each rule specifies a target key, an operation to perform, the phase in which it executes, and ordering constraints that control execution sequence within a transformation pipeline.

## API

### `string Id`

Unique identifier for the rule. Used for logging, debugging, and rule management operations. Must be non-null and non-empty.

### `string Description`

Human-readable explanation of the rule's purpose. Intended for documentation and operational visibility. Must be non-null.

### `TransformationPhase Phase`

Specifies when the rule executes in the request/response lifecycle. Typical values include request pre-processing, request post-processing, response pre-processing, and response post-processing phases defined by the `TransformationPhase` enumeration.

### `TransformationOperation Operation`

The action to perform on the target key. Determines whether the rule adds, removes, renames, replaces, or otherwise modifies the specified key and its associated value. Defined by the `TransformationOperation` enumeration.

### `string Key`

The target field or header name on which the operation acts. Interpretation depends on the operation type—for example, the source key in a rename operation or the key to remove in a delete operation. Must be non-null.

### `string? Value`

Optional operand for the transformation. Used by operations that require a secondary value, such as the new key name in a rename operation or the replacement value in a set operation. May be `null` for operations that do not require it.

### `int Order`

Execution priority within the same phase. Lower values execute first. Rules with identical `Order` values have non-deterministic relative ordering and should be avoided in production configurations.

### `bool IsEnabled`

Controls whether the rule is active. When `false`, the transformation pipeline skips the rule entirely during execution. Allows rules to be temporarily disabled without removal.

### `void Validate()`

Throws `ValidationException` when the rule configuration is invalid. Checks include: `Id` and `Key` must be non-null and non-empty; `Value` must be non-null when the operation requires it (e.g., rename, set); `Order` must be non-negative; `Phase` and `Operation` must be defined enumeration values. Call before adding a rule to a pipeline to fail fast on misconfiguration.

## Usage

```csharp
// Add a header to all incoming requests before routing
var addHeaderRule = new TransformationRule
{
    Id = "add-x-tenant-id",
    Description = "Injects tenant identifier header for downstream routing",
    Phase = TransformationPhase.RequestPreProcessing,
    Operation = TransformationOperation.Set,
    Key = "X-Tenant-Id",
    Value = "tenant-12345",
    Order = 10,
    IsEnabled = true
};

addHeaderRule.Validate();
pipeline.AddRule(addHeaderRule);
```

```csharp
// Rename a legacy response field to current schema
var renameFieldRule = new TransformationRule
{
    Id = "rename-legacy-status",
    Description = "Maps deprecated 'statusCode' field to 'status'",
    Phase = TransformationPhase.ResponsePreProcessing,
    Operation = TransformationOperation.Rename,
    Key = "statusCode",
    Value = "status",
    Order = 20,
    IsEnabled = true
};

renameFieldRule.Validate();
pipeline.AddRule(renameFieldRule);
```

## Notes

- `Validate()` must be called explicitly before rule execution; the pipeline does not automatically validate rules on addition. Skipping validation may result in runtime exceptions during request processing.
- Rules with `IsEnabled = false` are retained in the collection but silently skipped. This preserves ordering and configuration without requiring removal and re-insertion.
- When multiple rules target the same key within the same phase, execution order follows the `Order` property. Ties produce non-deterministic behavior and should be avoided.
- The `Value` property is only meaningful for operations that consume it. Setting `Value` on operations that ignore it (e.g., `Remove`) has no effect and does not cause validation failure.
- This type is not thread-safe. Concurrent modification of rule properties or simultaneous calls to `Validate()` from multiple threads require external synchronization.
- `Validate()` throws on first encountered violation; it does not accumulate multiple errors. Fix the reported error and re-validate to uncover subsequent issues.
