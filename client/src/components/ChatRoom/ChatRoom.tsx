import React, { useState, useEffect, useRef, useCallback } from 'react';
import MessageList from './MessageList';  // Component that handles rendering messages
import ChatInput from './ChatInput';      // Component that handles input field and send button
import { useWebSocketContext } from '../../contexts/webSocketContext'; // Custom hook for WebSocket context
import './ChatRoom.css';
import { WebSocketBaseMessage } from '../../models/WebSocketBaseMessage'; // Message model

interface ChatRoomProps {
  /** The type of the chat, used to filter and display relevant messages */
  chatType: string;
  
  /** The title to display for the chat room */
  title: string;
  
  /** The unique user identifier */
  userId: string;
}

const ChatRoom: React.FC<ChatRoomProps> = ({ chatType, title, userId }) => {
  // Destructure WebSocket context to get message retrieval and sending functions
  const { getMessages, sendMessage } = useWebSocketContext();

  // State to store the input text in the message input field
  const [input, setInput] = useState('');

  // Reference to the end of the messages container for auto-scrolling
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Retrieve messages for the current 'chatType' from WebSocket context
  const messages = getMessages(chatType);

  // Auto-scroll to the bottom when messages are updated
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Function to handle sending a new message
  const handleSend = useCallback(() => {
    if (!input.trim()) return; // Prevent sending empty messages
    
    const message: WebSocketBaseMessage = {
      UserId: userId,
      TransactionId: crypto.randomUUID(), // Generate a unique transaction ID for the message
      Action: chatType, // The type of chat
      SubAction: 'ask', // Action sub-type (e.g., 'ask' for questions)
      Content: input, // The content of the message
    };
    
    sendMessage(message); // Send the message using WebSocket context
    setInput(''); // Clear the input field after sending
  }, [input, userId, chatType, sendMessage]);

  // Function to handle the Enter/Shift+Enter behavior for the input field
  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault(); // Prevent the default action (line break)
      handleSend(); // Send the message when Enter is pressed
    }
  }, [handleSend]);

  return (
    <div className="chat-room">
      <div className="chat-room-title">{title}</div>

      <div className="chat-interface">
        {/* Scrollable messages section */}
        <div className="messages-container">
          <MessageList messages={messages} chatType={chatType} /> {/* Render the list of messages */}
          <div ref={messagesEndRef} /> {/* Anchor for auto-scrolling */}
        </div>

        {/* Input section for typing messages */}
        <div className="chat-input">
          <ChatInput
            input={input}                 // The current input value
            onInputChange={setInput}       // Callback for updating input state
            onSend={handleSend}           // Callback for sending the message
            onKeyDown={handleKeyDown}     // Handle key press events (Enter/Shift+Enter)
          />
        </div>
      </div>
    </div>
  );
};

export default ChatRoom;
