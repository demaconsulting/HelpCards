## FileAssert Verification

This document provides the verification evidence for the FileAssert OTS software item. Requirements
for this OTS item are defined in the FileAssert OTS Software Requirements document.

### Required Functionality

DemaConsulting.FileAssert validates HTML and PDF documents produced during the build, asserting
that each document exists and contains expected content. It provides OTS evidence for Pandoc
and WeasyPrint and independently confirms file assertion is functioning. Self-validation proves
the tool itself is operational before ReqStream consumes the results.

### Verification Approach

FileAssert is verified by two complementary layers of evidence. First, the CI pipeline runs
`fileassert --validate --results artifacts/fileassert-self-validation.trx` after all documents
have been generated, exercising FileAssert's built-in self-validation suite and recording
functional test results for ReqStream.

Second, FileAssert is used throughout the pipeline to validate every generated document before
ReqStream runs — Build Notes, Code Quality, Review Plan, Review Report, Design, Verification,
and User Guide. If FileAssert were non-functional, these validation steps would fail or produce
incorrect results, causing `reqstream --enforce` to report missing test coverage and fail the
build. A passing CI build therefore constitutes transitive evidence that FileAssert correctly
asserted document content at each stage of the pipeline.

### Test Scenarios

#### FileAssert_Results

**Scenario**: FileAssert self-validation exercises the `--results` option, generating TRX test
results containing both passing and failing outcomes.

**Expected**: Writes a TRX results file with correctly classified pass and fail entries.

**Requirement coverage**: `HelpCards-OTS-FileAssert`.

#### FileAssert_File

**Scenario**: FileAssert self-validation exercises a test configuration that matches files by glob
pattern and asserts file-level criteria such as existence and count.

**Expected**: Passes when the matching files satisfy the asserted criteria.

**Requirement coverage**: `HelpCards-OTS-FileAssert`.

#### FileAssert_Text

**Scenario**: FileAssert self-validation exercises a test configuration that asserts the text content
of a file, such as a `contains` assertion.

**Expected**: Passes when the specified content is present.

**Requirement coverage**: `HelpCards-OTS-FileAssert`.

#### FileAssert_Html

**Scenario**: FileAssert self-validation exercises a test configuration that asserts the structure of
an HTML document.

**Expected**: Passes when the document satisfies the asserted structure.

**Requirement coverage**: `HelpCards-OTS-FileAssert`.

#### FileAssert_Pdf

**Scenario**: FileAssert self-validation exercises a test configuration that asserts criteria of a PDF
document, such as its page count and text.

**Expected**: Passes when the document satisfies the asserted criteria.

**Requirement coverage**: `HelpCards-OTS-FileAssert`.

### Requirements Coverage

- **`HelpCards-OTS-FileAssert`**: FileAssert_Results, FileAssert_File, FileAssert_Text, FileAssert_Html,
  FileAssert_Pdf
