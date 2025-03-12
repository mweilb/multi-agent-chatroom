/// <reference types="react" />

declare module 'react-mermaid2' {
    import { ComponentType } from 'react';
  
    interface MermaidProps {
      chart: string;
    }
  
    const Mermaid: ComponentType<MermaidProps>;
  
    export default Mermaid;
  }
  