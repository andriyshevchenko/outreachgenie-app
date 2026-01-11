#!/usr/bin/env node

/**
 * SLOC (Source Lines of Code) Calculator
 * 
 * This script calculates the physical lines of source code in the OutreachGenie project.
 * It uses cloc (Count Lines of Code) tool to analyze the codebase.
 * 
 * Features:
 * - Excludes generated files, migrations, and dependencies
 * - Provides detailed breakdown by language and file
 * - Generates summary statistics
 */

import { execSync } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const projectRoot = path.resolve(__dirname, '..');

// Configuration
const config = {
  excludeDirs: [
    'node_modules',
    'dist',
    'coverage',
    'bin',
    'obj',
    'Migrations',
    '.git',
    '.github',
    'playwright-report',
    '.playwright-mcp',
    'history',
    '.vs',
    '.vscode'
  ],
  excludeExts: [
    'json',
    'lock',
    'lockb',
    'md'
  ]
};

function runCloc(options = {}) {
  const excludeDirsArg = `--exclude-dir=${config.excludeDirs.join(',')}`;
  const excludeExtsArg = `--exclude-ext=${config.excludeExts.join(',')}`;
  
  let clocCmd = `npx cloc . ${excludeDirsArg} ${excludeExtsArg}`;
  
  if (options.byFile) {
    clocCmd += ' --by-file';
  }
  
  if (options.byLanguage) {
    clocCmd += ' --by-file-by-lang';
  }
  
  try {
    const output = execSync(clocCmd, {
      cwd: projectRoot,
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'pipe']
    });
    return output;
  } catch (error) {
    console.error('Error running cloc:', error.message);
    process.exit(1);
  }
}

function printHeader() {
  console.log('\n' + '='.repeat(80));
  console.log('  OutreachGenie SLOC (Source Lines of Code) Report');
  console.log('='.repeat(80) + '\n');
}

function printFooter() {
  console.log('\n' + '='.repeat(80));
  console.log('  Report generated: ' + new Date().toISOString());
  console.log('='.repeat(80) + '\n');
}

function main() {
  const args = process.argv.slice(2);
  const showByFile = args.includes('--by-file');
  const showByLanguage = args.includes('--by-lang');
  
  printHeader();
  
  if (args.includes('--help') || args.includes('-h')) {
    console.log('Usage: npm run sloc [options]');
    console.log('\nOptions:');
    console.log('  --help, -h      Show this help message');
    console.log('  --by-file       Show statistics by file');
    console.log('  --by-lang       Show statistics by language and file');
    console.log('\nExamples:');
    console.log('  npm run sloc');
    console.log('  npm run sloc -- --by-file');
    console.log('  npm run sloc -- --by-lang');
    printFooter();
    return;
  }
  
  console.log('Calculating SLOC...\n');
  
  const options = {
    byFile: showByFile,
    byLanguage: showByLanguage
  };
  
  const output = runCloc(options);
  console.log(output);
  
  printFooter();
}

main();
