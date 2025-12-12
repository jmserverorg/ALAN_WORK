import React from 'react';
import ReactDOM from 'react-dom/client';
import { AlanCopilotChat } from './components/AlanCopilotChat';

const rootElement = document.getElementById('copilot-chat-root');
if (rootElement) {
  const chatApiUrl = rootElement.getAttribute('data-chatapi-url') || 'http://localhost:5041/api';
  const root = ReactDOM.createRoot(rootElement);
  root.render(<AlanCopilotChat chatApiBaseUrl={chatApiUrl} />);
}
