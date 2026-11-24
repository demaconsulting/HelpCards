## xUnit Verification

This document provides the verification evidence for the xUnit OTS software item. Requirements
for this OTS item are defined in the xUnit OTS Software Requirements document.

### Required Functionality

xUnit v3 (xunit.v3 and xunit.runner.visualstudio) is the unit-testing framework used by the
project. It discovers and runs all test methods and writes TRX result files that feed into coverage
reporting and requirements traceability. Passing tests confirm the framework is functioning
correctly.

### Verification Approach

xUnit is verified by self-validation evidence from the CI pipeline. Each scenario names a specific
test method that xUnit must discover, execute, and record in a TRX result file. A passing pipeline
run for all scenarios constitutes evidence that both requirements are satisfied.

### Test Scenarios

#### HelpCards_Embedding_DefaultDirectory_UsesAssemblyNamePrefix

**Scenario**: xUnit discovers and runs this test; the test verifies that a project using the
default Help folder embeds its files under the assembly name prefix.

**Expected**: xUnit executes the test, the test passes, and the result appears in the TRX output.

**Requirement coverage**: `HelpCards-OTS-xUnit-Execute`, `HelpCards-OTS-xUnit-Report`.

#### HelpCards_Embedding_OverriddenDirectory_EmbedsEveryFileUnderPrefix

**Scenario**: xUnit discovers and runs this test; the test verifies that a project that overrides
the embedded folder embeds every nested file under the prefix.

**Expected**: xUnit executes the test, the test passes, and the result appears in the TRX output.

**Requirement coverage**: `HelpCards-OTS-xUnit-Execute`, `HelpCards-OTS-xUnit-Report`.

#### HelpCards_Embedding_EmbeddedResource_ReadsAuthoredContent

**Scenario**: xUnit discovers and runs this test; the test verifies that an embedded resource can
be read back with its authored content.

**Expected**: xUnit executes the test, the test passes, and the result appears in the TRX output.

**Requirement coverage**: `HelpCards-OTS-xUnit-Execute`, `HelpCards-OTS-xUnit-Report`.

### Requirements Coverage

- **`HelpCards-OTS-xUnit-Execute`**: HelpCards_Embedding_DefaultDirectory_UsesAssemblyNamePrefix,
  HelpCards_Embedding_OverriddenDirectory_EmbedsEveryFileUnderPrefix,
  HelpCards_Embedding_EmbeddedResource_ReadsAuthoredContent
- **`HelpCards-OTS-xUnit-Report`**: HelpCards_Embedding_DefaultDirectory_UsesAssemblyNamePrefix,
  HelpCards_Embedding_OverriddenDirectory_EmbedsEveryFileUnderPrefix,
  HelpCards_Embedding_EmbeddedResource_ReadsAuthoredContent
