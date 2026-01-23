# Contributing to DotNet API Gateway

Thank you for your interest in contributing to DotNet API Gateway! We welcome contributions from the community.

## Getting Started

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/dotnet-api-gateway.git
   cd dotnet-api-gateway
   ```

### Development Setup

1. **Prerequisites**
   - .NET 10.0 SDK or later ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
   - Git
   - A code editor (Visual Studio Code, Visual Studio, or JetBrains Rider recommended)

2. **Build the Project**
   ```bash
   # Restore dependencies
   dotnet restore
   
   # Build the solution
   dotnet build
   
   # Run tests
   dotnet test
   ```

3. **Run the Gateway Locally**
   ```bash
   dotnet run --project DotNetApiGateway.csproj
   ```
   The gateway will be available at `http://localhost:5000`

## Making Changes

### Creating a Feature Branch

```bash
git checkout -b feature/your-feature-name
# or for bug fixes
git checkout -b bugfix/issue-description
```

Use descriptive branch names that clearly indicate the feature or fix.

### Code Style

This project follows standard C# conventions:

- **Naming**: Use PascalCase for public members, camelCase for private/local variables
- **Formatting**: Follow the conventions in `.editorconfig` (automatically applied by most editors)
- **XML Documentation**: Add XML doc comments to public classes and methods:
  ```csharp
  /// <summary>
  /// Brief description of what this method does.
  /// </summary>
  /// <param name="paramName">Description of the parameter.</param>
  /// <returns>Description of return value.</returns>
  public string DoSomething(string paramName)
  {
      // Implementation
  }
  ```

- **Author Headers**: Preserve existing author attribution headers in files
- **Line Length**: Keep lines reasonably short for readability
- **Async/Await**: Use async patterns consistently where appropriate

### Testing

- Write tests for new features and bug fixes
- Ensure all tests pass before submitting a PR:
  ```bash
  dotnet test
  ```
- Follow the existing test structure and naming conventions
- Include both happy path and edge case tests

### Commit Guidelines

- Write clear, descriptive commit messages
- Reference related issues if applicable (e.g., "Fix #123")
- Keep commits focused on a single change
- Use imperative mood: "Add feature" not "Added feature"

Example:
```
Add JWT validation for protected routes

- Implement JwtValidationService
- Add configuration options for JWT validation
- Include unit tests
- Fixes #42
```

## Submitting a Pull Request

1. **Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request**
   - Go to the original repository on GitHub
   - Click "New Pull Request"
   - Select your branch and write a clear PR description
   - Reference any related issues

3. **PR Guidelines**
   - Keep PRs focused on a single feature or fix
   - Provide a clear description of changes
   - Include screenshots or examples if relevant (especially for UI changes)
   - Ensure all checks pass (build, tests, code analysis)
   - Update documentation if needed

4. **Code Review**
   - Address feedback constructively
   - Keep the conversation professional and collaborative
   - Push additional commits for changes (don't rebase/force-push)

## Reporting Issues

### Bug Reports

When reporting a bug, please include:

1. **Description**: Clear summary of the issue
2. **Steps to Reproduce**: Detailed instructions to reproduce the bug
3. **Expected Behavior**: What should happen
4. **Actual Behavior**: What actually happens
5. **Environment**:
   - OS and version
   - .NET SDK version
   - Gateway version
6. **Logs**: Any relevant error messages or logs

### Feature Requests

For feature requests:

1. **Title**: Clear, concise description
2. **Motivation**: Why this feature is needed
3. **Proposed Solution**: How you envision the feature working
4. **Alternatives Considered**: Other approaches you've thought about
5. **Examples**: Use cases where this would be helpful

### Reporting Security Vulnerabilities

**Do NOT** open public issues for security vulnerabilities. Instead, please use GitHub's [Private Vulnerability Reporting](https://github.com/sarmkadan/dotnet-api-gateway/security/advisories/new) or email **rutova2@gmail.com**.

## Project Structure

```
├── BackgroundServices/      # Long-running background tasks
├── Configuration/           # Configuration models and validators
├── Constants/              # Enums and constants
├── Controllers/            # API endpoints
├── Middleware/             # HTTP middleware pipeline
├── Models/                 # Data models
├── Repositories/           # Data access layer
├── Services/               # Business logic
├── Utilities/              # Helper utilities
├── Formatters/             # Response formatting
├── Integration/            # External API integration
├── Events/                 # Event bus implementation
├── Exceptions/             # Custom exception types
├── docs/                   # Documentation
├── examples/               # Code examples
└── Program.cs              # Application entry point
```

## License

By contributing to DotNet API Gateway, you agree that your contributions will be licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Questions?

Feel free to reach out:
- Open a discussion on GitHub
- Check existing [documentation](docs/)
- Review [examples](examples/)

Thank you for contributing to DotNet API Gateway!
