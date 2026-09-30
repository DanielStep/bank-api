# xUnit v3 and Shouldly in place of RSpec

The challenge rubric asks for RSpec, but the solution is written in .NET 10, as agreed with the reviewer. The specs use xUnit v3 with nested classes named in describe/context/it style, Shouldly assertions, and `WebApplicationFactory` for in-process API specs.

## Considered Options

- **NSpec**: the closest match to RSpec's style, but its last release was 3.1.0 in 2017 and its test adapter no longer finds any specs under `dotnet test` on .NET 10.
- **Reqnroll (Gherkin)**: well maintained, but `.feature` files and generated code are too heavy for unit-level domain specs.
- **LightBDD**: incompatible with xUnit v3 4.x. When combined with it, `dotnet test` reported a pass while silently dropping the scenarios.
- **FluentAssertions 8**: needs a commercial licence for commercial use, so Shouldly was chosen instead.

## Consequences

A committed `global.json` switches `dotnet test` to Microsoft Testing Platform. xUnit v3 4.x needs this to run on the .NET 10 SDK.
