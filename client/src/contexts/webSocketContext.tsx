import React, {
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from 'react';
import { WebSocketBaseMessage } from '../models/WebSocketBaseMessage';
import { WebSocketReplyChatRoomMessage } from '../models/WebSocketReplyChatRoomMessages';
import { WebSocketGetRoomsMessage, WebSocketRoom } from '../models/WebSocketGetRoomsMessage';


// For non-room messages.
type NonRoomMessage = WebSocketReplyChatRoomMessage;

// Store non-room messages keyed by action.
type ActionMessages = { [action: string]: NonRoomMessage[] };

interface IWebSocketContext {
  getMessages(action: string): WebSocketReplyChatRoomMessage[];
  sendMessage: (message: WebSocketBaseMessage) => void;
  connectionStatus: 'Connected' | 'Disconnected' | 'Reconnecting';
  rooms: WebSocketRoom[];
}

const WebSocketContext = createContext<IWebSocketContext | undefined>(undefined);

interface WebSocketProviderProps {
  url: string;
  retryInterval?: number;
  maxRetries?: number;
  children: ReactNode;
}

export const WebSocketProvider: React.FC<WebSocketProviderProps> = ({
  url,
  retryInterval = 5000,
  maxRetries = 10,
  children,
}) => {
  const [websocket, setWebsocket] = useState<WebSocket | null>(null);
  // General messages for non-room actions.
  const [actionMessages, setActionMessages] = useState<ActionMessages>({});
  // Dedicated state for rooms.
  const [rooms, setRooms] = useState<WebSocketRoom[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<
    'Connected' | 'Disconnected' | 'Reconnecting'
  >('Disconnected');
  const reconnectAttempts = useRef(0);
  const reconnectTimer = useRef<number | null>(null);

  const initializeWebSocket = () => {
    const socketConnection = new WebSocket(url);

    socketConnection.onopen = () => {
      console.log('WebSocket connected');
      setConnectionStatus('Connected');
      reconnectAttempts.current = 0;
      // Request rooms immediately after connection is established.
      const requestRoomsMessage: WebSocketBaseMessage = {
        UserId: '', // Customize as needed.
        TransactionId: 'rooms-get-' + Date.now(),
        Action: 'rooms',
        SubAction: 'get',
        Content: '',
      };
      socketConnection.send(JSON.stringify(requestRoomsMessage));
    };

    socketConnection.onmessage = (event) => {
      let incomingMessage: any | null = null;
      try {
        incomingMessage = JSON.parse(event.data);
      } catch (err) {
        console.error('Invalid JSON:', event.data);
        return;
      }
      if (
        !incomingMessage ||
        !incomingMessage.UserId ||
        !incomingMessage.TransactionId ||
        !incomingMessage.Action ||
        !incomingMessage.SubAction
      ) {
        console.error('Malformed message:', incomingMessage);
        return;
      }

      if (incomingMessage.Action === 'rooms') {
        // Process room messages separately.
        const roomsResponse = incomingMessage as WebSocketGetRoomsMessage;
        // Update rooms state, defaulting to an empty array if no rooms are provided.
        setRooms(roomsResponse.Rooms ?? []);
      } else {
        // Process non-room messages.
        setActionMessages((prevMessages) => {
          const actionKey = incomingMessage.Action;
          const currentMessages = prevMessages[actionKey] || [];
          const index = currentMessages.findIndex(
            (msg) => msg.TransactionId === incomingMessage.TransactionId
          );
          let updatedMessages;
          if (index !== -1) {
            updatedMessages = currentMessages.map((msg, idx) =>
              idx === index ? incomingMessage : msg
            );
          } else {
            updatedMessages = [...currentMessages, incomingMessage];
          }
          return { ...prevMessages, [actionKey]: updatedMessages };
        });
      }
    };

    socketConnection.onclose = () => {
      console.log('WebSocket disconnected');
      setConnectionStatus('Reconnecting');
      handleReconnect();
    };

    socketConnection.onerror = (err) => {
      console.error('WebSocket error:', err);
      socketConnection.close();
    };

    setWebsocket(socketConnection);
  };

  const handleReconnect = () => {
    if (reconnectAttempts.current < maxRetries) {
      reconnectAttempts.current += 1;
      console.log(`Reconnecting attempt ${reconnectAttempts.current}/${maxRetries}`);
      reconnectTimer.current = window.setTimeout(initializeWebSocket, retryInterval);
    } else {
      console.error('Max reconnection attempts reached.');
      setConnectionStatus('Disconnected');
    }
  };

  useEffect(() => {
    initializeWebSocket();
    return () => {
      if (websocket) websocket.close();
      if (reconnectTimer.current) clearTimeout(reconnectTimer.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [url]);

  // When sending a message, assume it's new and simply append it.
  const sendMessage = (message: WebSocketBaseMessage) => {
    if (message.Action === 'rooms') {
      // For room messages, we don't need to save the request
    } else {
      setActionMessages((prevMessages) => {
        const actionKey = message.Action;
        const currentMessages = prevMessages[actionKey] || [];
        const newMessage: WebSocketReplyChatRoomMessage = {
          // spread the existing message (which might be missing some properties)
          ...message,
          // supply default values for missing properties:
          Hints: {
            agent: {
              // Use the agent content if available; otherwise, use message.content
              content: message.Content,
            }
          },
          AgentName: 'User', // if not provided
          Emoji: '🤓',  
        };
        return { ...prevMessages, [actionKey]: [...currentMessages, newMessage] };
      });
    }

    if (websocket && websocket.readyState === WebSocket.OPEN) {
      websocket.send(JSON.stringify(message));
    } else {
      console.error('WebSocket is not open. Message not sent:', message);
    }
  };

  // Getter for non-room messages.
  const getMessages = useCallback(
    (action: string): WebSocketReplyChatRoomMessage[] =>
      (actionMessages[action] || []) as WebSocketReplyChatRoomMessage[],
    [actionMessages]
  );

  return (
    <WebSocketContext.Provider
      value={{ getMessages, sendMessage, connectionStatus, rooms }}
    >
      {children}
    </WebSocketContext.Provider>
  );
};

export const useWebSocketContext = (): IWebSocketContext => {
  const context = useContext(WebSocketContext);
  if (!context) {
    throw new Error('useWebSocketContext must be used within a WebSocketProvider');
  }
  return context;
};
