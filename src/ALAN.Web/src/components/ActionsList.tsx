import { useCopilotReadable } from '@copilotkit/react-core';
import './ActionsList.css';

interface Action {
  id: string;
  name: string;
  status: string;
  result?: string;
  error?: string;
  timestamp: string;
}

interface ActionsListProps {
  actions: Action[];
}

function ActionsList({ actions }: ActionsListProps) {
  // Make actions readable by CopilotKit
  useCopilotReadable({
    description: 'Recent actions taken by the autonomous agent',
    value: actions,
  });

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
      case 'success':
        return '#4caf50';
      case 'running':
      case 'inprogress':
        return '#2196f3';
      case 'failed':
      case 'error':
        return '#f44336';
      default:
        return '#888';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
      case 'success':
        return '✓';
      case 'running':
      case 'inprogress':
        return '⟳';
      case 'failed':
      case 'error':
        return '✗';
      default:
        return '○';
    }
  };

  return (
    <div className="panel actions-list">
      <h2>Recent Actions</h2>
      {actions.length === 0 ? (
        <p className="empty-message">No actions yet...</p>
      ) : (
        <div className="actions-container">
          {actions.slice().reverse().map((action) => (
            <div key={action.id} className="action-item">
              <div className="action-header">
                <span 
                  className="action-status-icon"
                  style={{ color: getStatusColor(action.status) }}
                >
                  {getStatusIcon(action.status)}
                </span>
                <span className="action-name">{action.name}</span>
                <span className="action-time">
                  {new Date(action.timestamp).toLocaleTimeString()}
                </span>
              </div>
              <div 
                className="action-status"
                style={{ color: getStatusColor(action.status) }}
              >
                {action.status}
              </div>
              {action.result && (
                <div className="action-result">
                  <strong>Result:</strong> {action.result}
                </div>
              )}
              {action.error && (
                <div className="action-error">
                  <strong>Error:</strong> {action.error}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default ActionsList;
