# Quick Start: Applying Repository Configuration

This guide provides step-by-step instructions for applying the repository configuration to enable PR requirements and automated reviews.

## ✅ What's Already Configured (via files in this PR)

The following have been set up automatically through files in this repository:

1. ✅ **CODEOWNERS** - Automatic review assignments (@tomlm)
2. ✅ **PR Automation Workflow** - Welcomes contributors, labels PRs
3. ✅ **PR Labeler** - Auto-labels based on changed files
4. ✅ **Settings File** - Repository settings reference

## 🔧 Manual Configuration Required (Repository Admin Only)

The following settings MUST be configured manually by a repository administrator through the GitHub UI:

### Step 1: Enable Branch Protection for `main`

1. Navigate to: https://github.com/tomlm/Iciclecreek.Avalonia.WindowManager/settings/branches
2. Click **"Add branch protection rule"** (or **"Edit"** if one exists)
3. Configure the following:

#### Basic Settings
- **Branch name pattern**: `main`

#### Pull Request Requirements
- ✅ Check: **"Require a pull request before merging"**
  - Set **"Required number of approvals before merging"** to: `1`
  - ✅ Check: **"Dismiss stale pull request approvals when new commits are pushed"**
  - ✅ Check: **"Require review from Code Owners"**

#### Status Check Requirements
- ✅ Check: **"Require status checks to pass before merging"**
  - ✅ Check: **"Require branches to be up to date before merging"**
  - In the search box, add required status checks:
    - Type `build` and select it (from BuildAndRunTests workflow)

#### Additional Settings
- ✅ Check: **"Require conversation resolution before merging"**
- ❌ Leave UNCHECKED: **"Do not allow bypassing the above settings"**
  - This allows admins to bypass rules when necessary
- ❌ Leave UNCHECKED: **"Allow force pushes"** 
- ❌ Leave UNCHECKED: **"Allow deletions"**

4. Click **"Create"** or **"Save changes"**

### Step 2: Enable GitHub Copilot for PRs (If Available)

If you have **GitHub Copilot Enterprise**:

1. Navigate to: https://github.com/tomlm/Iciclecreek.Avalonia.WindowManager/settings/copilot
2. Enable available Copilot features:
   - ✅ **Copilot Pull Request Summaries**
   - ✅ **Copilot in comments**

If you have **GitHub Copilot Business/Individual**:
- No repository settings needed
- Reviewers will use Copilot in their IDE/editor when reviewing PRs
- Use Copilot Chat to analyze PR changes

### Step 3: Create Labels (Optional)

The repository should have standard labels. To add them:

1. Navigate to: https://github.com/tomlm/Iciclecreek.Avalonia.WindowManager/labels
2. Create these labels if they don't exist:
   - `size/XS` (color: 009900) - Extra small PR
   - `size/S` (color: 77bb00) - Small PR
   - `size/M` (color: eeee00) - Medium PR
   - `size/L` (color: ff9900) - Large PR
   - `size/XL` (color: ee0000) - Extra large PR
   - `documentation` (color: 0075ca)
   - `source code` (color: default)
   - `github-actions` (color: default)
   - `tests` (color: default)

### Step 4: Install GitHub Settings App (Optional)

For automated configuration management:

1. Visit: https://github.com/apps/settings
2. Click **"Install"**
3. Select the repository
4. The app will read `.github/settings.yml` and apply configurations automatically

**Note:** This is optional. Manual configuration (Steps 1-3) is sufficient.

## ✅ Verification

After completing the manual steps, test the configuration:

### Test 1: Direct Push to Main (Should Fail for Non-Admins)
```bash
git checkout main
echo "test" >> README.md
git add README.md
git commit -m "Test direct push"
git push origin main  # Should fail with protection error
```

Expected: ❌ Push rejected with message about branch protection

### Test 2: Create a Test PR
```bash
git checkout -b test-pr-protection
echo "test" >> README.md
git add README.md
git commit -m "Test PR workflow"
git push origin test-pr-protection
```

Then create a PR on GitHub and verify:
- ✅ Code owner (@tomlm) is automatically requested for review
- ✅ Welcome comment is added by PR automation
- ✅ Labels are automatically applied
- ✅ Status checks run (build)
- ✅ Merge button is disabled until approved and checks pass

### Test 3: Admin Override
As an admin, you should still be able to:
- Merge without approval (with a warning)
- Force push if needed (not recommended)

## 🎯 Success Criteria

You'll know the configuration is complete when:

1. ✅ Non-admin users cannot push directly to `main`
2. ✅ All changes to `main` must go through a PR
3. ✅ PRs automatically request @tomlm for review
4. ✅ PRs must have at least 1 approval before merging
5. ✅ PRs must pass status checks (build) before merging
6. ✅ Admins can still bypass rules when necessary
7. ✅ PRs get automatic labels and welcome messages

## 📚 Additional Resources

- Full configuration details: [REPOSITORY_CONFIGURATION.md](./REPOSITORY_CONFIGURATION.md)
- GitHub configuration reference: [settings.yml](./settings.yml)
- Workflow details: [README.md](./README.md)

## 🆘 Troubleshooting

**Issue: Code owner not being requested**
- Check that `.github/CODEOWNERS` file is in the `main` branch
- Verify "Require review from Code Owners" is checked in branch protection

**Issue: Status checks not required**
- Make sure the `build` check has run at least once on a PR
- The check must appear in the list before it can be marked required

**Issue: PR automation not working**
- Check workflow runs: https://github.com/tomlm/Iciclecreek.Avalonia.WindowManager/actions
- Verify the workflow has necessary permissions

## 📝 Summary

This configuration ensures:
- ✅ Code quality through required reviews
- ✅ Automated testing through required status checks
- ✅ Clear PR workflow with automation
- ✅ Admin flexibility for emergency fixes
- ✅ Foundation for GitHub Copilot integration

Once manual steps are completed, the repository will be fully configured! 🎉
