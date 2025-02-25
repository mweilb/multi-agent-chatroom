import mermaid from "mermaid";
import React from "react";

mermaid.initialize({
  startOnLoad: true,
  theme: 'default',
  securityLevel: 'loose',
  fontFamily: 'monospace',
});

interface MermaidComponentProps {
  chart: string;
}

export default class MermaidComponent extends React.Component<MermaidComponentProps> {
  componentDidMount() {
    mermaid.contentLoaded();
  }

  render() {
    return <div className="mermaid">{this.props.chart}</div>;
  }
}
