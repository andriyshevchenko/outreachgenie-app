# Scripts Directory

This directory contains utility scripts for the OutreachGenie project.

## calculate-sloc.js

Calculates Source Lines of Code (SLOC) for the project using the `cloc` tool.

### Usage

```bash
# Basic SLOC count
npm run sloc

# Show detailed breakdown by file
npm run sloc -- --by-file

# Show breakdown by language and file
npm run sloc -- --by-lang

# Show help
npm run sloc -- --help
```

### What is counted

- **TypeScript files** (.ts, .tsx): Frontend code
- **C# files** (.cs): Backend code
- **CSS files**: Stylesheets
- **JavaScript files**: Scripts and config
- **MSBuild scripts**: .NET build configuration
- **Config files**: Various configuration formats

### What is excluded

- **Generated files**: Migrations, build outputs
- **Dependencies**: node_modules, bin, obj
- **Documentation**: .md files
- **Lock files**: package-lock.json, bun.lockb
- **Git metadata**: .git, .github
- **Build artifacts**: dist, coverage, bin, obj
- **Reports**: playwright-report, history

### Output

The script provides:
- Total lines of code by language
- Blank lines count
- Comment lines count
- Physical lines of source code (SLOC)
