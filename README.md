# OutreachGenie Frontend

A modern React application for LinkedIn outreach automation with real-time updates and a clean, accessible UI.

## 🎯 Overview

This is the frontend application for OutreachGenie, built with React 18, TypeScript, and shadcn/ui. It provides an intuitive interface for managing outreach campaigns, viewing analytics, and interacting with the AI agent.

## 🏗️ Technology Stack

- **React 18** - Modern React with hooks
- **TypeScript** - Type-safe development
- **Vite** - Fast build tool and dev server
- **shadcn/ui** - Beautiful, accessible UI components
- **TanStack Query** - Powerful data fetching and caching
- **Tailwind CSS** - Utility-first styling
- **SignalR** - Real-time updates from backend
- **Vitest** - Fast unit testing
- **Playwright** - End-to-end testing

## 🚀 Getting Started

### Prerequisites

- Node.js 18 or higher
- npm or yarn

### Installation

```bash
# Clone the repository
git clone <YOUR_GIT_URL>
cd <YOUR_PROJECT_NAME>

# Install dependencies
npm install

# Start the development server
npm run dev
```

The application will be available at `http://localhost:8080`

## 📋 Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run build:dev` - Build in development mode
- `npm run lint` - Run ESLint
- `npm run preview` - Preview production build
- `npm test` - Run unit tests
- `npm run test:ui` - Run tests with UI
- `npm run test:coverage` - Generate coverage report
- `npm run test:e2e` - Run end-to-end tests
- `npm run test:e2e:ui` - Run E2E tests with UI
- `npm run test:e2e:debug` - Debug E2E tests

## 🧪 Testing

This project maintains high test coverage:

- Unit tests with Vitest and React Testing Library
- E2E tests with Playwright
- Coverage requirements: ≥90% statements, ≥85% branches

See [frontend.md](./frontend.md) for detailed testing guidelines and quality standards.

## 📁 Project Structure

```
src/
├── components/       # Reusable UI components
│   ├── chat/        # Chat-related components
│   ├── layout/      # Layout components (Sidebar, etc.)
│   └── ui/          # shadcn/ui components
├── pages/           # Page components
├── lib/             # Utilities and helpers
│   ├── api.ts      # API client
│   └── signalr.ts  # SignalR connection
├── test/            # Test setup and utilities
└── types/           # TypeScript type definitions
```

## 🔗 Backend Integration

This frontend connects to the OutreachGenie backend API:

- REST API for CRUD operations
- SignalR for real-time updates
- Backend should be running on `http://localhost:5000`

## 📖 Documentation

- [frontend.md](./frontend.md) - Code quality rules and standards
- Component documentation in Storybook (coming soon)

## 🎨 Design System

This project uses shadcn/ui components with Tailwind CSS. All components follow:

- Accessibility best practices (ARIA, keyboard navigation)
- Consistent design tokens
- Dark mode support (via next-themes)

## 🚢 Deployment

Build the production bundle:

```bash
npm run build
```

The built files will be in the `dist/` directory, ready to deploy to any static hosting service (Netlify, Vercel, etc.).

## 🤝 Contributing

Please follow the coding standards defined in [frontend.md](./frontend.md):

- Type safety (no `any`)
- Pure, deterministic components
- Comprehensive testing
- Accessible UI

## 📝 License

[Add your license here]
