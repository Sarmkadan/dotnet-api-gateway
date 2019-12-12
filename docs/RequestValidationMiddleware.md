# RequestValidationMiddleware

Middleware component responsible for validating incoming HTTP requests before they reach downstream services. It inspects request headers, query parameters, and body content to ensure they conform to expected schemas and business rules, collecting any validation issues into a structured list for downstream error handling.

## API

### `RequestValidationMiddleware`

Initializes a new instance of the `RequestValidationMiddleware` with the required request delegate and validation configuration.

| Parameter | Type | Description |
|-----------|------|-------------|
| `next` | `RequestDelegate` | The next middleware in the pipeline. |
| `validationConfig` | `IValidationConfiguration` | Configuration defining validation rules for requests. |

### `InvokeAsync`

Asynchronously invokes the middleware to validate the current HTTP context.
