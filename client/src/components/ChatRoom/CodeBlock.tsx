import React from 'react';
 
import Mermaid from 'react-mermaid2';
 

interface CodeProps {
  inline?: boolean;
  className?: string;
  children?: React.ReactNode;
  node?: any;
}

export const CodeBlock: React.FC<CodeProps> = ({ inline, className, children, ...props }) => {
  const match = /language-(\w+)/.exec(className || '');
  
  // Render Mermaid diagrams if language is "mermaid"
  if (!inline && match && match[1] === 'mermaid') {
    return <Mermaid chart={String(children)} />;
  }
 
  return <span className={className} {...props}>{children}</span>;
};

