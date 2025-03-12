// src/components/ChatRoom/DynamicChatRoom.tsx
import React from 'react';
import { useParams } from 'react-router-dom';  // Importing hook to access route parameters
import ChatRoom from '../ChatRoom/ChatRoom';   // Importing the ChatRoom component to display the dynamic chat

/**
 * DynamicChatRoom component is responsible for rendering the `ChatRoom` component with dynamic room details.
 * The room name is extracted from the URL parameters using `useParams` to render a unique chat room.
 */
const DynamicChartRoomRouter: React.FC = () => {
  // Extract the `roomName` parameter from the URL path
  const { roomName } = useParams<{ roomName: string }>();

  /**
   * You can optionally fetch additional room details, such as the room's participants, or other data,
   * from your WebSocket context or an API if needed. This component focuses on dynamically rendering 
   * the `ChatRoom` based on the room name.
   */

  return (
    <ChatRoom
      chatType={`${roomName}`}  // Set the chat type to the dynamic room name from URL
      title={`Room: ${roomName}`}  // Display the room name as the title
      userId="John Doe"           // The user ID is hardcoded here, but you could fetch this dynamically
    />
  );
};

export default DynamicChartRoomRouter;
