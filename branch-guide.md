# Branch guide

`main` is the integration branch. Start focused branches from an up-to-date
`main` and submit pull requests back to it.

Use descriptive names such as `feature/destination-discovery`, `fix/trace-cancellation`, or
`docs/setup`. Codex-created branches use `codex/<description>`.

Before requesting review, build and test the solution as described in
[CONTRIBUTING.md](CONTRIBUTING.md). Explain the problem, resulting behavior,
and validation. Keep unrelated changes in separate pull requests.

Maintainers choose the merge method and configure any required GitHub checks
or reviews. Delete merged topic branches when they are no longer needed.

## Versions and releases

`VERSION` supplies the shared MSBuild version. Use `MAJOR.MINOR.PATCH`, optionally
with a prerelease suffix. The current `0.x` series is under active development;
review the changelog for compatibility changes before upgrading.

For a release, update VERSION and prepare the matching CHANGELOG.md
section, then merge the changes into main after CI passes.

Run the Release workflow from main with the matching version and
publish disabled to inspect a release candidate.

To publish, run Release with publish enabled and inspect that run's
candidate before approving the release environment. The workflow
publishes the prepared packages and creates the GitHub release/tag.

Do not create the release tag manually beforehand.
