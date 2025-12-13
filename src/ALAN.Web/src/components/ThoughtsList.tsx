'use client';

import { useCopilotReadable } from '@copilotkit/react-core';
import './ThoughtsList.css';

interface Thought {
  id: string;
  type: string;
  content: string;
  timestamp: string;
}

interface ThoughtsListProps {
  thoughts: Thought[];
}

function ThoughtsList({ thoughts }: ThoughtsListProps) {
  // Make thoughts readable by CopilotKit
  useCopilotReadable({
    description: 'Recent thoughts from the autonomous agent',
    value: thoughts,
  });

  const getThoughtIcon = (type: string) => {
    switch (type.toLowerCase()) {
      case 'analysis':
        return '🔍';
      case 'decision':
        return '🎯';
      case 'planning':
        return '📋';
      case 'reflection':
        return '💭';
      default:
        return '💡';
    }
  };

  return (
    <div className="panel thoughts-list">
      <h2>Recent Thoughts</h2>
      {thoughts.length === 0 ? (
        <p className="empty-message">No thoughts yet...</p>
      ) : (
        <div className="thoughts-container">
          {thoughts.slice().reverse().map((thought) => (
            <div key={thought.id} className="thought-item">
              <div className="thought-header">
                <span className="thought-icon">{getThoughtIcon(thought.type)}</span>
                <span className="thought-type">{thought.type}</span>
                <span className="thought-time">
                  {new Date(thought.timestamp).toLocaleTimeString()}
                </span>
              </div>
              <div className="thought-content">{thought.content}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default ThoughtsList;
