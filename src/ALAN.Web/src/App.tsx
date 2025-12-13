/// <reference types="vite/client" />

import { CopilotKit } from '@copilotkit/react-core';
import { CopilotSidebar } from '@copilotkit/react-ui';
import '@copilotkit/react-ui/styles.css';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Dashboard from './components/Dashboard';
import './App.css';

function App() {
  // Get the API URL from environment or use default
  const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5001';
  
  return (
    <CopilotKit runtimeUrl={`${apiUrl}/copilotkit`}>
      <BrowserRouter>
        <div className="app-container">
          <CopilotSidebar
            defaultOpen={true}
            clickOutsideToClose={false}
            labels={{
              title: "ALAN Assistant",
              initial: "Ask me anything about the agent's state, thoughts, or actions.",
            }}
          >
            <Routes>
              <Route path="/" element={<Dashboard />} />
            </Routes>
          </CopilotSidebar>
        </div>
      </BrowserRouter>
    </CopilotKit>
  );
}

export default App;
