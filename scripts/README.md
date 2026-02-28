# Scripts

## run-tests.sh

Wrapper around `dotnet test` that logs results to `artifacts/test-results/`.

Each run produces three files (timestamped):
- `.log` - full console output
- `.trx` - structured XML results (CI/CD compatible)
- `.html` - human-readable report

### Usage

```bash
# All tests
bash scripts/run-tests.sh

# Specific test project
bash scripts/run-tests.sh tests/CompanyIntel.AppHost.Tests

# Filter by test class
bash scripts/run-tests.sh tests/CompanyIntel.AppHost.Tests RagEvaluationTests

# Filter by test method
bash scripts/run-tests.sh tests/CompanyIntel.AppHost.Tests "RagEvaluationTests.EvaluateRagResponseQuality"
```

### Output

```
artifacts/test-results/
  20260228T172600.log    # console log
  20260228T172600.trx    # TRX results
  20260228T172600.html   # HTML report
```
