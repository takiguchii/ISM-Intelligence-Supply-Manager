# Contributing

## Branching

- Base branch for day-to-day work: `develop`
- Allowed prefixes: `feat/`, `fix/`, `mod/`, `css/`, `test/`, `doc/`, `hotfix/`, `refactor/`
- Suggested format: `<prefix>/<area>/<short-description>`

## Commits

- Use Portuguese commit messages.
- Keep messages short and descriptive.
- Prefixes: `feat:`, `fix:`, `mod:`, `css:`, `test:`, `doc:`, `hotfix:`, `refactor:` (ex: `feat: adiciona endpoint de produtos`).

## Git Hooks

To enable automatic local validation (merge conflict markers and EF Core migration immutability checks):

```bash
chmod +x scripts/*.sh .githooks/*
git config core.hooksPath .githooks/
```

## Pull Requests

- Open PRs against `develop`
- Use the repository PR template
- Include test evidence and migration notes when relevant

## Local Workflow

- Prefer `docker compose up` for daily development
- Keep secrets only in local `.env`
- Do not version `.codex`, `.agents`, caches or IDE files

