# Testing conventions

## Stack

| Role | Package |
|---|---|
| Test framework | xUnit |
| Mocking | Moq |
| Assertions | AwesomeAssertions |

Test projects are not AOT-compiled; Moq's runtime code generation is acceptable in this context.

## Project structure

Each source project (except `Mked.Console`) has a mirror test project under `tests/`:

```
tests/
├── Mked.Domain.Tests/
├── Mked.Application.Tests/
├── Mked.Infrastructure.Tests/
└── Mked.Controls.Tests/
```

See `docs/architecture/solution-structure.md` for the full project layout.

## Test naming

```
{ClassUnderTest}_{Scenario}_{ExpectedOutcome}
```

Examples:

```csharp
public class MarkdownDocument_Parse_Tests
{
    [Fact]
    public void EmptyInput_ReturnsEmptyDocument() { ... }

    [Fact]
    public void BoldText_ParsesAsBoldInlineNode() { ... }
}
```

## Pattern

All tests follow Arrange / Act / Assert with blank-line separation between sections:

```csharp
[Fact]
public void OpenFile_WhenFileExists_ReturnsOkWithDocument()
{
    // Arrange
    var repo = new FakeMarkdownRepository();
    repo.Add("/tmp/test.md", "# Hello");
    var useCase = new OpenFileUseCase(repo);

    // Act
    var result = useCase.Execute("/tmp/test.md");

    // Assert
    result.IsOk.Should().BeTrue();
    result.Value.Title.Should().Be("Hello");
}
```

## In-memory fakes vs mocks

**Prefer fakes** — hand-rolled in-memory implementations of domain interfaces — for use case tests. Fakes exercise the contract without coupling tests to call sequences.

**Use Moq** when the test needs to verify a specific interaction (that a method was called, how many times, with what arguments) rather than just an outcome.

```csharp
// Fake — preferred for use case tests
var repo = new FakeMarkdownRepository();
repo.Add("/tmp/test.md", "# Hello");

// Mock — use when verifying an interaction
var mockWatcher = new Mock<IFileWatcher>();
// ... exercise code ...
mockWatcher.Verify(w => w.Watch("/tmp/test.md"), Times.Once);
```

## Layer-specific guidance

| Layer | What to test | What not to test |
|---|---|---|
| Domain | Value objects, `Result`/`Option` combinators, domain invariants | Anything touching I/O |
| Application | Use cases in isolation via fakes of domain interfaces | File system, terminal |
| Infrastructure | Adapter behaviour against a real temp directory or `MemoryStream` | Domain logic already covered |
| Controls | Widget state transitions, render output for given input, key event handling | Actual ANSI terminal output |

## Running tests

```powershell
dotnet test
```

For a specific project:

```powershell
dotnet test tests/Mked.Application.Tests
```
