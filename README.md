# HelpCards

[![GitHub forks][badge-forks]][link-forks]
[![GitHub stars][badge-stars]][link-stars]
[![GitHub contributors][badge-contributors]][link-contributors]
[![License][badge-license]][link-license]
[![Build][badge-build]][link-build]
[![Quality Gate][badge-quality]][link-quality]
[![Security][badge-security]][link-security]
[![NuGet][badge-nuget]][link-nuget]

HelpCards is a .NET library for authoring help and knowledge content as Markdown cards with YAML front matter,
embedded in assemblies and served as command-line help for people and as searchable knowledge for AI agents.

The diagram below is a placeholder: replace its TODO boxes to show how an application calls your library's
subsystems, which inputs it takes, and which outputs it produces.

![Placeholder architecture diagram: replace the TODO boxes for your application, library subsystems, support units, dependencies, inputs, and outputs with the real ones][architecture]

## Features

The repository provides:

- **Markdown Cards**: Help and knowledge content authored as Markdown files with restricted-subset front matter
- **Resource Embedding**: A packaged MSBuild targets file embeds the cards in your assembly
- **Dependency-Free**: Implemented exclusively against the .NET Base Class Library
- **Multi-Platform Support**: Builds and runs on Windows, Linux, and macOS
- **Multi-Runtime Support**: Targets .NET 8, 9, 10, and 11
- **xUnit v3**: Modern unit testing with xUnit framework version 3
- **Comprehensive CI/CD**: GitHub Actions workflows with quality checks and builds
- **Linting Enforcement**: LintMark, TextLint, and yamllint enforced on every CI run
- **Continuous Compliance**: Compliance evidence generated automatically on every CI run, following
  the [Continuous Compliance][link-continuous-compliance] methodology
- **SonarCloud Integration**: Quality gate and security analysis on every build
- **Documentation Generation**: Automated build notes, user guide, code quality reports,
  requirements, justifications, and trace matrix
- **Requirements Traceability**: Requirements linked to passing tests with auto-generated trace matrix

## Installation

Install the library using the .NET CLI:

```bash
dotnet add package DemaConsulting.HelpCards
```

## Usage

Put Markdown cards in a `Help` folder of your project; the package embeds them in the assembly. Then load
them as a source and build a catalog:

```csharp
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

var catalog = HelpCatalog.Create(builder => builder
    .AddSource(HelpSource.FromAssembly(typeof(Program).Assembly)));
var card = catalog.Find("getting-started");
Console.WriteLine(card is null ? catalog.RenderIndex() : catalog.Render(card));
```

## Documentation

Generated documentation includes:

- **Build Notes**: Release information and changes
- **User Guide**: Comprehensive usage documentation
- **Code Quality Report**: CodeQL and SonarCloud analysis results
- **Requirements**: Functional and non-functional requirements
- **Requirements Justifications**: Detailed requirement rationale
- **Trace Matrix**: Requirements to test traceability

## Contributing

Contributions are welcome. See [CONTRIBUTING.md][link-contributing] for development setup, coding
standards, and the pull request process.

## License

Copyright (c) DEMA Consulting. Licensed under the MIT License. See [LICENSE][link-license] for details.

By contributing to this project, you agree that your contributions will be licensed under the MIT License.

<!-- Badge References -->
[architecture]: https://raw.githubusercontent.com/demaconsulting/HelpCards/main/docs/images/architecture.png
[badge-forks]: https://img.shields.io/github/forks/demaconsulting/HelpCards?style=plastic
[badge-stars]: https://img.shields.io/github/stars/demaconsulting/HelpCards?style=plastic
[badge-contributors]: https://img.shields.io/github/contributors/demaconsulting/HelpCards?style=plastic
[badge-license]: https://img.shields.io/github/license/demaconsulting/HelpCards?style=plastic
[badge-build]: https://img.shields.io/github/actions/workflow/status/demaconsulting/HelpCards/build_on_push.yaml?style=plastic
[badge-quality]: https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_HelpCards&metric=alert_status
[badge-security]: https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_HelpCards&metric=security_rating
[badge-nuget]: https://img.shields.io/nuget/v/DemaConsulting.HelpCards?style=plastic

<!-- Link References -->
[link-forks]: https://github.com/demaconsulting/HelpCards/network/members
[link-stars]: https://github.com/demaconsulting/HelpCards/stargazers
[link-contributors]: https://github.com/demaconsulting/HelpCards/graphs/contributors
[link-license]: https://github.com/demaconsulting/HelpCards/blob/main/LICENSE
[link-build]: https://github.com/demaconsulting/HelpCards/actions/workflows/build_on_push.yaml
[link-quality]: https://sonarcloud.io/dashboard?id=demaconsulting_HelpCards
[link-security]: https://sonarcloud.io/dashboard?id=demaconsulting_HelpCards
[link-nuget]: https://www.nuget.org/packages/DemaConsulting.HelpCards
[link-continuous-compliance]: https://github.com/demaconsulting/ContinuousCompliance
[link-contributing]: https://github.com/demaconsulting/HelpCards/blob/main/CONTRIBUTING.md
