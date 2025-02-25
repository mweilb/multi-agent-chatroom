import React, {JSX} from 'react';
import mermaid from 'mermaid';

// Initialize Mermaid
mermaid.initialize({
  startOnLoad: true,
  theme: 'default',
  securityLevel: 'loose',
  fontFamily: 'monospace',
});

interface MermaidProps {
  chart: string;
}

export default class MermaidComponent extends React.Component<MermaidProps> {
  container: HTMLDivElement | null = null;

  componentDidMount(): void {
    this.renderMermaidDiagram();
  }

  componentDidUpdate(): void {
    this.renderMermaidDiagram();
  }

  async renderMermaidDiagram(): Promise<void> {
    if (this.container) {
      try {
        const { svg, bindFunctions } = await mermaid.render('mermaid-svg', this.props.chart);
        this.container.innerHTML = svg;
        bindFunctions?.(this.container);
      } catch (error) {
        console.error('Error rendering Mermaid diagram:', error);
      }
    }
  }

  render(): JSX.Element {
    return (
      <div
        className="mermaid"
        ref={(ref) => {
          this.container = ref;
        }}
      />
    );
  }
}
