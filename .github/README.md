# GitHub Configuration

This directory contains GitHub-specific configuration files for the Iciclecreek.Avalonia.WindowManager repository.

## Files Overview

### CODEOWNERS
Defines code ownership and automatic review assignments. When a PR is opened, the listed owners will automatically be requested to review changes.

**Current Setup:**
- `@tomlm` is the owner for all files

### REPOSITORY_CONFIGURATION.md
Detailed guide for configuring repository settings including:
- Branch protection rules
- GitHub Copilot integration
- Required status checks
- Administrator overrides

### settings.yml
Repository settings file that can be used with:
1. [GitHub Settings App (Probot)](https://github.com/repository-settings/app) - for automated configuration
2. Manual configuration reference - as documentation for settings to apply via GitHub UI

**Key Settings:**
- Branch protection for `main` branch
- Require PR reviews before merging
- Require status checks to pass
- Labels for issue and PR management

### labeler.yml
Configuration for automatic PR labeling based on changed files.

**Labels Applied:**
- `source-code` - Changes to source code
- `documentation` - Changes to documentation
- `github-actions` - Changes to workflows
- `tests` - Changes to test files
- `dependencies` - Changes to dependencies

### Workflows

#### BuildAndRunTests.yml
Runs on PR and push to main. Builds the solution and runs tests.

#### pr-automation.yml
Automated PR management:
- Welcomes new PR contributors
- Adds automatic labels based on file changes
- Analyzes PR size
- Provides review guidelines

#### PublishNuget.yml & PublishIncrementalNuget.yml
Handles NuGet package publishing workflows.

## Setting Up Branch Protection

To enforce the requirement that PRs are needed for code changes (except for admins), a repository administrator must:

1. Go to **Settings** → **Branches** on GitHub
2. Add a branch protection rule for `main`
3. Configure settings as described in `REPOSITORY_CONFIGURATION.md`

**Key Requirements:**
- ✅ Require a pull request before merging
- ✅ Require approvals (at least 1)
- ✅ Require review from Code Owners
- ✅ Require status checks to pass (`build`)
- ✅ Require conversation resolution before merging
- ❌ Do not allow bypassing for admins (unchecked) - This allows admins to bypass when necessary

## GitHub Copilot Integration

GitHub Copilot can provide automated code reviews on PRs. To enable:

### For GitHub Copilot Enterprise Users:
1. Go to repository **Settings** → **Copilot**
2. Enable Copilot features for the repository
3. Configure automatic PR summaries and reviews

### For GitHub Copilot Individual/Business Users:
- Reviewers can use Copilot in their IDE when reviewing PRs
- Use Copilot chat to analyze changes and provide feedback
- Copilot will help write review comments and suggest improvements

### Alternative Automated Review Options:
If Copilot Enterprise is not available, consider:
- GitHub Copilot Workspace for collaborative reviews
- Third-party AI code review actions from GitHub Marketplace
- Custom review automation workflows

## Testing the Configuration

After setup, verify by:

1. **Test PR Requirement:**
   ```bash
   git checkout -b test-branch
   echo "test" > test.txt
   git add test.txt
   git commit -m "Test commit"
   git push origin test-branch
   ```
   Try to push directly to main (should fail for non-admins)

2. **Test PR Workflow:**
   - Create a PR from test-branch to main
   - Verify code owner is requested for review
   - Verify status checks run
   - Verify merge is blocked until approved

3. **Test Automation:**
   - Check that PR gets automatic welcome comment
   - Check that PR gets labeled automatically
   - Check that size label is applied

## Maintenance

- Review and update CODEOWNERS as team structure changes
- Update required status checks as CI/CD evolves
- Adjust branch protection rules based on team needs
- Keep labels in sync with project management needs

## Support

For questions or issues with these configurations, please:
1. Check the `REPOSITORY_CONFIGURATION.md` for detailed instructions
2. Contact the repository administrator (@tomlm)
3. Review GitHub's [branch protection documentation](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
