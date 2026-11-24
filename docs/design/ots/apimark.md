## ApiMark

### Purpose

ApiMark is an API documentation generator that produces Markdown documentation from the HelpCards
library assembly and its XML documentation file. It is chosen because it generates hierarchical
per-type and per-member Markdown files that can be bundled into the NuGet package, making all
public types and members discoverable by consumers and automated agents.

### Features Used

- **Assembly-based API documentation generation**: reads the compiled library assembly
  (`DemaConsulting.HelpCards.dll`) and its companion XML documentation file to extract public
  type and member documentation comments.
- **Hierarchical Markdown output**: emits a top-level API index (`api.md`), namespace pages, type
  pages, and member pages using a folder structure that mirrors the namespace hierarchy.
- **Documentation enforcement**: `ApiMarkEnforceDocs=Public` with `ApiMarkEnforceDocsSeverity=Error`
  fails the build when a public API member lacks an XML documentation summary, so the shipped
  reference cannot develop gaps.
- **NuGet package inclusion**: the generated Markdown files are included in the `api/` folder of
  the NuGet package so package consumers can read API documentation without visiting an external
  site.

### Integration Pattern

ApiMark is integrated through the `DemaConsulting.ApiMark.MSBuild` NuGet package reference in
`DemaConsulting.HelpCards.csproj`. It runs during `dotnet build` once per build (for the first
target framework, because the public surface is identical across target frameworks) and writes
Markdown output to the `api/` folder of the project, which is excluded from source control by the
`.gitignore` rule `src/**/api/`. During `dotnet pack`, ApiMark packs those files into the package
under `api/` when `ApiMarkPackDocs=true`. FileAssert validates the resulting `.nupkg` archive to
confirm that `api/DemaConsulting.HelpCards.Catalog.md`, `api/api.md`, and related files are
present.
