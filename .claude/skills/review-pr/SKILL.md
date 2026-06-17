---
name: review-pr
description: Review the current branch's changes against SDK architectural patterns and report violations grouped by severity.
---

# PR Review for Privy Unity SDK

Review the current branch's changes against our SDK architectural patterns and code correctness rules. For each file changed, check the rules in `agent_docs/pr_review_rules.md` and report violations. Be strict — our SDK's consistency depends on this.

## Instructions

1. Determine the parent branch:
   - If `$ARGUMENTS` is provided, use that as the base branch.
   - Otherwise, detect the parent branch by running: `git log --decorate --simplify-by-decoration --oneline --first-parent HEAD | grep -v "HEAD" | head -1` to find the nearest branch point. Alternatively, check `git config branch.$(git branch --show-current).merge` for the upstream tracking branch, or fall back to the merge-base with `main`.
2. Run `git diff <parent-branch>...HEAD` to get only this branch's changes (excluding the parent's commits).
3. Read `agent_docs/pr_review_rules.md` to load the full rule set.
4. For each changed/added `.cs` file, evaluate against the rules.
5. Report findings grouped by severity: **Blocking** (must fix), **Warning** (should fix), **Nit** (style preference).
6. If no violations found, confirm the PR looks good.

## Output Format

For each violation found:

```
### [Severity] File: path/to/file.cs

**Rule**: [Rule name]
**Line(s)**: [line numbers]
**Issue**: [What's wrong]
**Fix**: [What to do instead]
```

At the end, provide a summary: number of blocking/warning/nit issues, and an overall verdict (Approve, Request Changes, or Approve with Nits).

## Comment Style

- Be terse. One sentence per finding is the norm. Two is long. Never write a paragraph when a sentence will do.
- Ask questions over demands. "Can we make this `internal`?" lands better than "Make this `internal`."
- Prefix minor style comments with `nit:` to signal they won't block merge.
- Explain the "why" only when non-obvious. A missing `I` prefix is self-explanatory; a subtle async deadlock is not.
- No filler. Don't open with "Great work!" or pad with "Love the approach, however...". Go straight to the point.
- Only comment on lines with real issues — do not comment for the sake of it.
