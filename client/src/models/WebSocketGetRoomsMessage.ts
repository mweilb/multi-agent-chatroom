import { WebSocketBaseMessage } from './WebSocketBaseMessage';

 
/** Profile of an agent in a chat room */
export interface WebSocketAgentProfile {
  /** Agent's name */
  Name: string;
  /** Agent's emoji */
  Emoji: string;
}

/** WebSocket message for a chat room with its agents */
export interface WebSocketRoom extends WebSocketBaseMessage {
    /** The room's name */
    Name: string;
    /** Room's Emoji */
    Emoji: string;
    /** Agents in the room */
    Agents: WebSocketAgentProfile[];
}

/** WebSocket message carrying a list of chat rooms */
export interface WebSocketGetRoomsMessage extends WebSocketBaseMessage {
  Rooms?: WebSocketRoom[];
}
