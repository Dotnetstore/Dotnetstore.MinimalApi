# Branch protection guidance

This repository uses GitHub Actions checks, Conventional Commits validation, CODEOWNERS, and release automation. To keep `main` stable, configure branch protection with the following recommendations.

## Recommended branch protection settings for `main`

### Require a pull request before merging

Recommended options:

- Require approvals: `1` or more
- Dismiss stale approvals when new commits are pushed
- Require review from code owners via `.github/CODEOWNERS`
- Require conversation resolution before merging

### Require status checks to pass before merging

Recommended required checks:

- `Build and test`
- `Validate PR title`
- `Validate commit messages`
- `Apply automatic labels`

Optional checks depending on your release policy:

- `Publish release artifact`
- `Build and publish container image`
- `Create release PR or tag`
- `Update release draft`

### Additional recommended settings

- Require branches to be up to date before merging
- Require linear history if you prefer squash or rebase merges only
- Restrict who can push directly to `main`
- Do not allow force pushes
- Do not allow deletions

## Why these checks matter

- `Build and test` ensures the solution restores, builds, and passes tests
- `Validate PR title` ensures release and changelog automation can interpret PR intent
- `Validate commit messages` keeps commit history compatible with semantic versioning
- `Apply automatic labels` keeps release notes, triage, and dashboards organized

## CODEOWNERS

The repository includes `.github/CODEOWNERS` so GitHub can automatically request reviews for:

- source code changes in `src/`
- test changes in `tests/`
- workflow and repository automation changes in `.github/`
- documentation changes in `docs/` and root markdown files

## Release automation alignment

Because the repository uses Conventional Commits and Release Please:

- use `feat:` for user-facing features
- use `fix:` for bug fixes
- use `docs:` for documentation-only changes
- use `build:` or `ci:` for automation and workflow updates

These conventions help:

- `Release Please` determine the next semantic version
- `Release Drafter` categorize release notes
- PR title and commit validation checks stay meaningful

## Suggested merge strategy

A good default is:

- enable **Squash merge**
- disable merge commits if you want a cleaner history
- keep PR titles compliant with Conventional Commits, since squash merge commonly uses the PR title as the final commit message

