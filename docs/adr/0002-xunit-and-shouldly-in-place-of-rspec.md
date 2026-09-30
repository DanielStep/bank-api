# xUnit v3 and Shouldly in place of RSpec

The challenge rubric asks for RSpec, but the solution is written in .NET 10, as agreed with HR. The specs use xUnit v3 with nested classes named in describe/context/it style, Shouldly assertions, and `WebApplicationFactory` for in-process API specs.

## Considered Options

- **NSpec**: the closest match to RSpec's style, but its last release was 3.1.0 in 2017 and its test adapter no longer finds any specs under `dotnet test` on .NET 10.

## Consequences

Lack of string literal test explainations, though test names are still clear and follow the same pattern