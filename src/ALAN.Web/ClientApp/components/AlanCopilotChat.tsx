import React from 'react';
import { CopilotKit } from '@copilotkit/react-core';
import { CopilotPopup } from '@copilotkit/react-ui';
import '@copilotkit/react-ui/styles.css';

interface AlanCopilotChatProps {
  chatApiBaseUrl: string;
}

export const AlanCopilotChat: React.FC<AlanCopilotChatProps> = ({ chatApiBaseUrl }) => {
  // CopilotKit expects an AG-UI compatible endpoint
  const agentEndpoint = `${chatApiBaseUrl}/agui`;

  return (
    <CopilotKit
      runtimeUrl={agentEndpoint}
      agent="alan-agent"
    >
      <div style={{ height: '600px', display: 'flex', flexDirection: 'column' }}>
        <CopilotPopup
          instructions="You are ALAN (Autonomous Learning Agent Network), an AI assistant with access to accumulated knowledge and memories. Help users by leveraging your context and learned experiences."
          labels={{
            title: "Chat with ALAN",
            initial: "Hi! I'm ALAN. How can I assist you today?",
          }}
          defaultOpen={true}
        />
      </div>
    </CopilotKit>
  );
};
