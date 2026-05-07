export interface User {
  id: string;
  username: string;
  email?: string | null;
  phone: string;
  avatarUrl?: string | null;
  displayName?: string | null;
  status: string;
  isVerified: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  user: User;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export interface Conversation {
  id: string;
  type: string;
  name?: string | null;
  description?: string | null;
  avatarUrl?: string | null;
  lastMessage?: Message | null;
  unreadCount: number;
  lastActivityAt: string;
  members?: Array<{
    userId: string;
    role: string;
  }>;
}

export interface Message {
  id: string;
  conversationId: string;
  senderId?: string | null;
  content?: string | null;
  messageType: string;
  mediaUrl?: string | null;
  isEdited?: boolean;
  isDeleted?: boolean;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface Call {
  id: string;
  conversationId: string;
  initiatorId: string;
  callType: string;
  status: string;
}
