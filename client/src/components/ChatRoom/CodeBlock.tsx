import React, { useState } from 'react';
import MermaidComponent from './MermaidComponent';


interface CodeProps {
  inline?: boolean;
  className?: string;
  children?: React.ReactNode;
  node?: any;
}

// Function to fix the graph syntax
const fixGraphSyntax = (graph: string): string => {
  // Replace occurrences of '[]' containing '()' with '[]' containing '-'
  return graph.replace(/\[\(.*?\)\]/g, '[-]');
};

export const CodeBlock: React.FC<CodeProps> = ({ inline, className, children, ...props }) => {
  const [isDiagramVisible, setIsDiagramVisible] = useState(false);
  const match = /language-(\w+)/.exec(className || '');

  // Function to toggle between text and diagram views
  const toggleView = () => {
    setIsDiagramVisible((prev) => !prev);
  };

  // Render Mermaid diagrams if language is "mermaid"
  if (!inline && match && match[1] === 'mermaid') {
    
    var processedChildren = fixGraphSyntax(String(children)); 
    return (
      <div>
        <button onClick={toggleView} style={{ marginBottom: '10px' }}>
          {isDiagramVisible ? 'Show Text' : 'Show Diagram'}
        </button>
        {isDiagramVisible ? (
          <MermaidComponent chart={String(processedChildren)} />
        ) : (
          <pre className={className} {...props}>
            {processedChildren}
          </pre>
        )}
      </div>
    );
  }

  // Fallback for non-mermaid code blocks
  return (
    <pre className={className} {...props}>
      {children}
    </pre>
  );
};
