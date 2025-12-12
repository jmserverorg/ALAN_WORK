# ALAN.Web Frontend Build

This directory contains the React + TypeScript frontend using CopilotKit for the chat interface.

## Prerequisites

- Node.js 20+ and npm
- TypeScript
- .NET 8.0 SDK

## Building the Frontend

The frontend must be built **before** running the ASP.NET Core application:

```bash
# From the ALAN.Web directory
cd src/ALAN.Web

# Install dependencies (first time only, or when package.json changes)
npm install

# Build the frontend bundle
npm run build
```

This will create the compiled JavaScript bundle in `wwwroot/dist/copilot-chat.bundle.js`.

## Development Workflow

For active development with auto-rebuild:

```bash
# Terminal 1 - Watch mode for frontend (rebuilds on file changes)
cd src/ALAN.Web
npm run dev

# Terminal 2 - Run the ASP.NET Core application
cd src/ALAN.Web
dotnet run
```

## Project Structure

```
ALAN.Web/
├── ClientApp/                  # TypeScript/React source code
│   ├── components/
│   │   └── AlanCopilotChat.tsx  # Main CopilotKit chat component
│   └── index.tsx                # Entry point
├── package.json                 # npm dependencies
├── tsconfig.json                # TypeScript configuration
├── webpack.config.js            # Webpack build configuration
└── wwwroot/
    └── dist/                    # Compiled output (gitignored)
        └── copilot-chat.bundle.js
```

## Technologies Used

- **CopilotKit**: AG-UI compatible chat UI framework
- **React 18**: UI framework
- **TypeScript**: Type-safe JavaScript
- **Webpack**: Module bundler

## Notes

- The `node_modules/` and `wwwroot/dist/` directories are gitignored
- You must run `npm install` and `npm run build` after cloning the repository
- The bundle size is large (~2MB) due to CopilotKit dependencies - this is normal
