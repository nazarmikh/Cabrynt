# Engineering Notes

## Dependency Health

- `SharpCompress 0.30.1` is reported as a transitive dependency warning through `MongoDB.Driver 3.8.1`.
- The application does not accept or extract user-provided archive files.
- The warning is accepted as residual dependency risk until the upstream dependency chain provides a fix.

## Current Quality Baseline

- Critical Hot Chocolate dependency warning removed.
- Snappier warning removed.
- EF Core package versions aligned across backend and test projects.
- Backend integration tests pass.
