## Codex Remote — GitHub Publication Policy

For authorized implementation tasks in `PapayaModding/UABS`:

1. Complete the requested changes.
2. Run available tests and validation.
3. Review the Git diff and ensure no unrelated files are included.
4. Commit the changes with a descriptive message.
5. Push to `origin/v2-recovery` without force.
6. Verify the commit exists on GitHub.
7. Include the commit SHA and GitHub URL in the final report.

**Do not stop after implementation and testing without publishing**, unless a blocker prevents safe publication.

### Safety rules

- Never push to `main`.
- Never force-push.
- Never include unrelated changes, Unity caches, `obj/`, secrets, or generated stress datasets.
- Never bypass failed publication safeguards.
- If tests fail, report the failure rather than claiming completion.
- If pushing is unavailable or the remote branch has diverged, report the blocker and provide a PR-based alternative.
- Do not publish generated AssetBundle datasets without separate authorization.

### Final report requirements

Every completed implementation task must state:

- Changes made.
- Tests performed and results.
- Commit SHA.
- GitHub commit URL.
- Publication status.

**Default expectation: An implementation task is not complete until its reviewed code changes are published to `origin/v2-recovery`.**
