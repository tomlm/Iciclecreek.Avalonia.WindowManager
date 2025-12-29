# Repository Configuration Guide

This document describes the recommended GitHub repository settings for this project.

## Branch Protection Rules

To ensure code quality and require pull requests for all code changes (except for repository admins), configure the following branch protection rules for the `main` branch:

### Accessing Branch Protection Settings

1. Go to the repository on GitHub
2. Click on **Settings** → **Branches**
3. Under "Branch protection rules", click **Add rule** or edit the existing rule for `main`

### Recommended Settings for `main` Branch

#### Basic Settings
- **Branch name pattern**: `main`
- **Require a pull request before merging**: ✅ Enabled
  - **Require approvals**: 1 (recommended)
  - **Dismiss stale pull request approvals when new commits are pushed**: ✅ Enabled (recommended)
  - **Require review from Code Owners**: ✅ Enabled (uses `.github/CODEOWNERS` file)

#### Status Checks
- **Require status checks to pass before merging**: ✅ Enabled
  - **Require branches to be up to date before merging**: ✅ Enabled (recommended)
  - **Status checks that are required**:
    - `build` (from BuildAndRunTests workflow)

#### Additional Settings
- **Require conversation resolution before merging**: ✅ Enabled (recommended)
- **Require linear history**: ⬜ Optional (prevents merge commits)
- **Allow force pushes**: ⬜ Disabled
  - **Specify who can force push**: Select "Repository administrators" or specific roles if you want admins to have this ability
- **Allow deletions**: ⬜ Disabled

#### Administrator Override
- **Do not allow bypassing the above settings**: ⬜ Disabled
  - This allows repository administrators to bypass these rules when necessary
  - Admins can merge without PR approval, but it's tracked in audit logs

## Code Owners

The `.github/CODEOWNERS` file has been configured to automatically request reviews from:
- **@tomlm** for all changes

## GitHub Copilot for Pull Requests

GitHub Copilot can provide AI-powered code reviews on pull requests. To enable this feature:

### Prerequisites
1. Your organization must have a GitHub Copilot Enterprise subscription
2. The repository must be part of an organization with Copilot enabled

### Enabling Copilot Reviews

#### Option 1: Using GitHub Copilot Pull Request Summaries (Enterprise)
If you have GitHub Copilot Enterprise:
1. Go to repository **Settings** → **Copilot**
2. Enable **Copilot Pull Request Summaries**
3. Configure automatic review triggers

#### Option 2: Using GitHub Copilot Workspace or Actions
For automatic reviews, you can:
1. Use the GitHub Copilot extension in your workflow
2. Add reviewers can use Copilot suggestions when reviewing PRs

#### Option 3: Manual Configuration
If automated Copilot reviews are not available:
- Reviewers will use GitHub Copilot in their IDE to help review code
- Use Copilot chat to ask questions about PR changes

### Alternative: Using GitHub Actions for Automated Reviews
If GitHub Copilot Enterprise is not available, consider using community actions:
- AI-powered PR review actions from the GitHub Marketplace
- Custom scripts that leverage AI APIs for code review

## Applying These Settings

These settings must be configured by a repository administrator through the GitHub web interface:

1. Ensure you have admin access to the repository
2. Follow the steps in each section above
3. Test the configuration by creating a test PR
4. Verify that:
   - PRs are required for merging to main
   - Status checks run and must pass
   - Code owners are automatically requested for review
   - Only admins can bypass rules if needed

## Verification

After configuration, test by:
1. Creating a branch with changes
2. Opening a PR to main
3. Verifying that:
   - Direct push to main is blocked (for non-admins)
   - Code owner review is requested
   - Status checks run
   - Merge is blocked until requirements are met

## Support

For questions about these configurations, contact the repository administrator.
