// DynamicRooms.tsx
import React from 'react';
import { Link } from 'react-router-dom';
import { useWebSocketContext } from '../../../contexts/webSocketContext';

interface DynamicRoomsProps {
  isCollapsed: boolean;
  location: any;
}

const DynamicRoomsList: React.FC<DynamicRoomsProps> = ({ isCollapsed, location }) => {

    
  const { rooms } = useWebSocketContext();

  return (
    <div className="dynamic-rooms-container">
      {!isCollapsed && <h3 className="group-label">Multi-Agent Chat Rooms</h3>}
      <nav>
        <ul>
          {rooms.length > 0 ? (
            rooms.map((room) => (
              <li
                key={room.Name}
                className={location.pathname === `/rooms/${encodeURIComponent(room.Name)}` ? 'active' : ''}
              >
                <Link to={`/rooms/${encodeURIComponent(room.Name)}`}>
                  {room.Emoji && <span className="nav-emoji">{room.Emoji}</span>}
                  {!isCollapsed && <span>{room.Name}</span>}
                </Link>
              </li>
            ))
          ) : (
            <li>
              <span className="no-rooms">No rooms available</span>
            </li>
          )}
        </ul>
      </nav>
    </div>
  );
};

export default DynamicRoomsList;
