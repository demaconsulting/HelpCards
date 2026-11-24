## ApiMark Verification

This document provides the verification evidence for the ApiMark OTS software item. Requirements
for this OTS item are defined in the ApiMark OTS Software Requirements document.

### Required Functionality

ApiMark shall generate Markdown API documentation from the library assembly and its XML
documentation file, and the documentation shall be packed into the `api/` folder of the NuGet
package.

### Verification Approach

ApiMark is verified through artifact content validation using FileAssert. After the NuGet package
is built, FileAssert asserts that the `.nupkg` zip archive contains `api/api.md` (which names the
`DemaConsulting.HelpCards.Catalog` namespace), `api/DemaConsulting.HelpCards.Catalog.md`, and at
least 100 documentation files under `api/**/*.md`. Passing this assertion proves ApiMark executed
correctly and produced meaningful output that was bundled into the package. The test is named in
the OTS requirements and tracked by ReqStream.

### Test Environment

The standard CI pipeline environment after the Release build produces the library assembly, XML
documentation file, and NuGet package. No network access or external services are required.

### Acceptance Criteria

- The FileAssert NuGet package assertion passes without error.
- The `.nupkg` archive contains `api/api.md` (exactly one match) that mentions
  `DemaConsulting.HelpCards.Catalog`.
- The `.nupkg` archive contains `api/DemaConsulting.HelpCards.Catalog.md` (exactly one match).
- The `.nupkg` archive contains at least 100 files matching `api/**/*.md`.
- The requirement `HelpCards-OTS-ApiMark` is linked to the named test identifier in the
  ReqStream trace matrix.

### Test Scenarios

#### ApiMark_NuGetPackageContainsDocs

**Scenario**: The NuGet package is built in Release configuration and FileAssert inspects the
resulting `.nupkg` archive for the generated API documentation.

**Expected**: The archive contains exactly one `api/api.md` naming the
`DemaConsulting.HelpCards.Catalog` namespace, exactly one
`api/DemaConsulting.HelpCards.Catalog.md`, and at least 100 `api/**/*.md` files.

**Requirement coverage**: `HelpCards-OTS-ApiMark`.

### Requirements Coverage

- **`HelpCards-OTS-ApiMark`**: ApiMark_NuGetPackageContainsDocs
