# OutreachGenie

A LinkedIn outreach automation platform with AI-powered messaging.

## 📁 Repository Structure

```
outreachgenie-app/
├── backend/           # .NET 10 backend solution (SLNX format)
├── frontend/          # React + TypeScript frontend application
└── README.md          # This file
```

## 🚀 Getting Started

### Visual Studio Solution

The repository uses a .NET 10 solution (SLNX format) that includes both backend and frontend projects.

```bash
cd backend
dotnet sln list           # List projects in the solution
dotnet sln add <project>  # Add projects to the solution
```

The solution includes:
- **Frontend**: React + TypeScript application (OutreachGenie.Frontend.esproj)
- Backend projects can be added as needed

### Frontend Application

The frontend is a modern React 18 application built with TypeScript, Vite, and shadcn/ui.

```bash
cd frontend
npm install
npm run dev
```

See [frontend/README.md](./frontend/README.md) for detailed frontend documentation.

## 📖 Documentation

- [Frontend Documentation](./frontend/README.md) - React application setup and development
- [Frontend Code Standards](./frontend/frontend.md) - Code quality rules and best practices

## 🤝 Contributing

Please follow the coding standards defined in the respective documentation files for each part of the application.

## 📝 License

[Add your license here]
