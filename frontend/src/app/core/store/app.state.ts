import { Call, Conversation, Message, User } from '../models';

export interface AppState {
  auth: {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
  };
  conversations: {
    list: Conversation[];
    activeConversationId: string | null;
    isLoading: boolean;
  };
  messages: {
    byConversationId: Record<string, Message[]>;
    hasMore: Record<string, boolean>;
    cursors: Record<string, string>;
  };
  presence: {
    onlineUsers: Set<string>;
    typingUsers: Record<string, string[]>;
  };
  calls: {
    activeCall: Call | null;
    incomingCall: Call | null;
    callState: 'idle' | 'ringing' | 'connecting' | 'active' | 'ended';
  };
  ui: {
    theme: 'light' | 'dark';
    notifications: Array<{ id: string; type: string; message: string }>;
  };
}
