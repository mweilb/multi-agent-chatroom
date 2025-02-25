import React from 'react';
import Mermaid from './Mermaid'; // Adjust the path as needed

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

  // Fallback for non-mermaid code blocks
  return <span className={className} {...props}>{children}</span>;
};
