import React, { useState } from 'react';
import { useLocation } from 'react-router-dom';
 
import { navItems } from '../../../configs/NavigationItems';
 
import './Navigator.css';
import DynamicRoomsList from './DynamicRoomsList';
import Filters from './Filters';
import AdminNav from './AdminNav';
import ConnectionStatus from './ConnectionStatus';


const Navigator: React.FC = () => {
  const [isCollapsed, setIsCollapsed] = useState(false);
  const location = useLocation();


  // Toggle the navigation collapse state
  const toggleNav = () => setIsCollapsed(prev => !prev);

  return (
    <div className={`navigator ${isCollapsed ? 'collapsed' : ''}`}>
      <button onClick={toggleNav} className="toggle-button">
        {isCollapsed ? '«' : '»'}
      </button>

      <DynamicRoomsList isCollapsed={isCollapsed} location={location} />
      
      {/* Conditionally render Filters when not collapsed */}
      {!isCollapsed && (
        <Filters/>
      )}
      
      <AdminNav isCollapsed={isCollapsed} navItems={navItems} location={location} />
      <ConnectionStatus />
    </div>
  );
};

export default Navigator;
