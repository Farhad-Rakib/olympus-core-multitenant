# Git Upload Instructions

This document explains how to initialize this template repository and push it to GitHub.

## 1. Initialize local Git repository

Run from the project root:

    git init
    git branch -M main

## 2. Review files before first commit

    git status

If you need to verify ignored build artifacts are not tracked:

    git check-ignore -v src/**/bin src/**/obj

## 3. Stage and commit

    git add .
    git commit -m "Initial commit: OlympusCore clean architecture template"

## 4. Create remote repository on GitHub

Create a new empty repository in GitHub, for example:

- Repository name: olympuscore-template
- Visibility: private or public
- Do not add README, .gitignore, or license from the GitHub UI (this repo already contains local files)

## 5. Add remote and push

Replace YOUR_GITHUB_USER with your account name.

HTTPS:

    git remote add origin https://github.com/YOUR_GITHUB_USER/olympuscore-template.git
    git push -u origin main

SSH:

    git remote add origin git@github.com:YOUR_GITHUB_USER/olympuscore-template.git
    git push -u origin main

## 6. Verify push

    git remote -v
    git log --oneline -n 5

## 7. Ongoing workflow

Use this routine for daily work:

    git status
    git add .
    git commit -m "Describe your change"
    git push

## Optional: add tags for template releases

    git tag v1.0.0
    git push origin v1.0.0

Recommended release strategy:

- v1.0.0 for first stable template release
- Increment minor version for backward-compatible template enhancements
- Increment major version for breaking template structure changes
