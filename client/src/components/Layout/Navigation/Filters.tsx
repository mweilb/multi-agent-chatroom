import React, { useState } from 'react';
import { messageSectionDefinitions } from '../../../configs/SectionDefinitions';

interface FiltersProps {
   
}

const Filters: React.FC<FiltersProps> = ({ 
}) => {
  const [hiddenSections, setHiddenSections] = useState<Set<string>>(new Set());
  const [hiddenFieldLabels, setHiddenFieldLabels] = useState<Set<string>>(new Set());

  // Toggle a specific section's visibility
  const toggleSection = (sectionTitle: string) => {
    const isCurrentlyHidden = hiddenSections.has(sectionTitle);
    const newVisibility = isCurrentlyHidden ? true : false;
    setHiddenSections((prev) => {
      const updated = new Set(prev);
      if (isCurrentlyHidden) {
        updated.delete(sectionTitle);
      } else {
        updated.add(sectionTitle);
      }
      return updated;
    });
    window.dispatchEvent(
      new CustomEvent('toggleSectionVisibility', {
        detail: { sectionTitle, visible: newVisibility },
      })
    );
  };

  // Toggle a field label's visibility
  const toggleField = (fieldLabel: string) => {
    const isCurrentlyHidden = hiddenFieldLabels.has(fieldLabel);
    const newVisibility = isCurrentlyHidden ? true : false;
    setHiddenFieldLabels((prev) => {
      const updated = new Set(prev);
      if (isCurrentlyHidden) {
        updated.delete(fieldLabel);
      } else {
        updated.add(fieldLabel);
      }
      return updated;
    });
    window.dispatchEvent(
      new CustomEvent('toggleFieldVisibility', {
        detail: { fieldLabel, visible: newVisibility },
      })
    );
  };

  // Handlers for global toggle buttons
  const showAllSections = () => {
    setHiddenSections(new Set());
    messageSectionDefinitions.forEach((section) => {
      window.dispatchEvent(
        new CustomEvent('toggleSectionVisibility', {
          detail: { sectionTitle: section.key, visible: true },
        })
      );

      if (section.groups) {
        section.groups.forEach((group) => {
          window.dispatchEvent(
            new CustomEvent('toggleSectionVisibility', {
              detail: { sectionTitle: group.key, visible: true },
            })
          );
        });
      }

    });
  };

  const hideAllSections = () => {
    // Create a set containing all section titles and group names.
    const allHidden = new Set(
      messageSectionDefinitions.reduce<string[]>((acc, section) => {
        acc.push(section.key);
        if (section.groups) {
          section.groups.forEach((group) => acc.push(group.key));
        }
        return acc;
      }, [])
    );

    setHiddenSections(allHidden);
    messageSectionDefinitions.forEach((section) => {
      window.dispatchEvent(
        new CustomEvent('toggleSectionVisibility', {
          detail: { sectionTitle: section.key, visible: false },
        })
      );

      if (section.groups) {
        section.groups.forEach((group) => {
          window.dispatchEvent(
            new CustomEvent('toggleSectionVisibility', {
              detail: { sectionTitle: group.key, visible: false },
            })
          );
        });
      }

    });
  };

  const expandAllSections = () => {
    window.dispatchEvent(
      new CustomEvent('message-section-toggle', {
        detail: { category: 'all', isOpen: true },
      })
    );
  };

  const collapseAllSections = () => {
    window.dispatchEvent(
      new CustomEvent('message-section-toggle', {
        detail: { category: 'all', isOpen: false },
      })
    );
  };

  // Compute unique field labels from all message section definitions
  const uniqueFieldLabels = Array.from(
    messageSectionDefinitions.reduce((acc, section) => {
      if (section.groups) {
        section.groups.forEach((group) => {
          group.fields.forEach((field) => acc.add(field.label));
        });
      }
      if (section.fields) {
        section.fields.forEach((field) => acc.add(field.label));
      }
      return acc;
    }, new Set<string>())
  );

  return (
    <div className="nav-group filters">
      <div className="filters-controls">
        {/* Global toggle buttons */}
        <div className="global-toggle-container">
          <button onClick={showAllSections}>Show All</button>
          <button onClick={hideAllSections}>Hide All</button>
          <button onClick={expandAllSections}>Expand All</button>
          <button onClick={collapseAllSections}>Collapse All</button>
        </div>

        {/* Section toggles */}
        <div className="toggle-group sections">
          <div className="sub-group-label">Sections</div>
          {messageSectionDefinitions.map((section) => (
            <div key={section.title} className="section-toggle-group">
              <button onClick={() => toggleSection(section.key)}>
                {hiddenSections.has(section.key)
                  ? `Show: ${section.title}`
                  : `Hide: ${section.title}`}
              </button>
              <div  className="group-toggle-group">
              {section.groups &&
                section.groups.map((group) => (
                  <button key={group.name} onClick={() => toggleSection(group.key)}>
                    {hiddenSections.has(group.key)
                      ? `Show: ${group.name}`
                      : `Hide: ${group.name}`}
                  </button>
                ))}
                </div>
            </div>
        ))}
        </div>
       

        {/* Field toggles */}
        <div className="toggle-group fields">
          <div className="sub-group-label">Fields</div>
          {uniqueFieldLabels.map((label) => (
            <button key={label} onClick={() => toggleField(label)}>
              {hiddenFieldLabels.has(label) ? `Show: ${label}` : `Hide: ${label}`}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Filters;
