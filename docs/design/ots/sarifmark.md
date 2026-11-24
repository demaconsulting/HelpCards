## SarifMark

### Purpose

DemaConsulting.SarifMark is used to convert CodeQL SARIF scan results into a human-readable
markdown report. It was chosen because it lets the pipeline surface static-analysis findings as
part of the standard build-artifact documentation set, alongside SonarMark's cloud-hosted quality
data, rather than requiring reviewers to open the SARIF file separately.

### Features Used

- SARIF-file reading of CodeQL code-scanning output
- Markdown report generation summarizing SARIF findings
- Self-validation mode (`--validate`), which exercises report generation and enforcement against known
  fixtures; the enforcement mode (non-zero exit code when the SARIF input contains reported issues) is
  verified there but is not used by the workflow

### Integration Pattern

SarifMark is invoked as a `dotnet tool` (`dotnet sarifmark`) from `.github/workflows/build.yaml`
after the CodeQL scanning step to generate `docs/code_quality/generated/codeql-quality.md`. The
workflow does not enforce a clean result: the report documents the findings and CodeQL itself gates the
code scanning. It is a stateless CLI invocation — each run reads a SARIF file,
writes its markdown report, and exits; there is no initialization,
configuration object, or disposal step.
