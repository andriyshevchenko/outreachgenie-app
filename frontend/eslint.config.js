import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
  { ignores: ["dist"] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommendedTypeChecked],
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
      parserOptions: {
        project: ["./tsconfig.json", "./tsconfig.app.json", "./tsconfig.node.json"],
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "react-refresh/only-export-components": ["error", { allowConstantExport: true }],

      // [1] Type Safety Rules
      "@typescript-eslint/no-explicit-any": "error",           // `any` is forbidden
      "@typescript-eslint/no-unsafe-assignment": "error",      // Enforce type safety
      "@typescript-eslint/no-unsafe-call": "error",
      "@typescript-eslint/no-unsafe-member-access": "error",
      "@typescript-eslint/no-unsafe-return": "error",
      "@typescript-eslint/explicit-module-boundary-types": "error", // Explicit return types on exports

      // [8] Linting & Formatting Rules
      "@typescript-eslint/no-unused-vars": ["error", {         // No unused exports/variables
        "argsIgnorePattern": "^_",
        "varsIgnorePattern": "^_"
      }],
      "no-console": ["error", { "allow": ["warn", "error"] }], // Structured logging

      // [2] Component Architecture
      "max-lines": ["error", { "max": 150, "skipBlankLines": true, "skipComments": true }],
      "complexity": ["error", 10],                             // Limit cognitive complexity

      // [3] Hooks Rules
      "react-hooks/rules-of-hooks": "error",                   // Never disable hooks rules
      "react-hooks/exhaustive-deps": "error",                  // Exhaustive dependency arrays

      // [11] Absolute Prohibitions
      "no-debugger": "error",                                  // No debugging statements
      "no-alert": "error",                                     // No silent failures
      "no-magic-numbers": ["error", {                          // No magic numbers
        "ignore": [0, 1, -1],
        "ignoreArrayIndexes": true,
        "enforceConst": true
      }],
    },
  },
  // Test files configuration
  {
    files: ["**/*.test.{ts,tsx}", "**/*.spec.{ts,tsx}"],
    rules: {
      "max-lines": "off",                                      // Tests can be longer
      "@typescript-eslint/no-explicit-any": "error",           // Still enforce in tests
    },
  },
);
