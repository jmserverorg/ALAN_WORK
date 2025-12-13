'use client';

import { useState } from 'react';
import './HumanInputPanel.css';

interface HumanInputPanelProps {
  apiUrl: string;
}

function HumanInputPanel({ apiUrl }: HumanInputPanelProps) {
  const [input, setInput] = useState('');
  const [status, setStatus] = useState<{ type: 'success' | 'error', message: string } | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim()) return;

    try {
      const response = await fetch(`${apiUrl}/api/input`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          type: 'UserInput',
          content: input,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      setStatus({ type: 'success', message: 'Input sent successfully!' });
      setInput('');
      
      setTimeout(() => setStatus(null), 3000);
    } catch (err) {
      setStatus({ 
        type: 'error', 
        message: err instanceof Error ? err.message : 'Failed to send input' 
      });
    }
  };

  const handlePause = async () => {
    try {
      const response = await fetch(`${apiUrl}/api/pause`, {
        method: 'POST',
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      setStatus({ type: 'success', message: 'Pause command sent!' });
      setTimeout(() => setStatus(null), 3000);
    } catch (err) {
      setStatus({ 
        type: 'error', 
        message: err instanceof Error ? err.message : 'Failed to pause agent' 
      });
    }
  };

  const handleResume = async () => {
    try {
      const response = await fetch(`${apiUrl}/api/resume`, {
        method: 'POST',
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      setStatus({ type: 'success', message: 'Resume command sent!' });
      setTimeout(() => setStatus(null), 3000);
    } catch (err) {
      setStatus({ 
        type: 'error', 
        message: err instanceof Error ? err.message : 'Failed to resume agent' 
      });
    }
  };

  return (
    <div className="panel human-input-panel">
      <h2>Human Steering</h2>
      
      <form onSubmit={handleSubmit} className="input-form">
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Provide guidance or feedback to the agent..."
          rows={4}
          className="input-textarea"
        />
        <div className="button-group">
          <button type="submit" className="btn-primary">
            Send Input
          </button>
          <button type="button" onClick={handlePause} className="btn-secondary">
            Pause Agent
          </button>
          <button type="button" onClick={handleResume} className="btn-secondary">
            Resume Agent
          </button>
        </div>
      </form>

      {status && (
        <div className={`status-message ${status.type}`}>
          {status.message}
        </div>
      )}
    </div>
  );
}

export default HumanInputPanel;
