import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Conversation, Message } from '../../core/models';
import { ChatApiService } from '../../core/services/chat-api.service';
import { SignalrService } from '../../core/services/signalr.service';

@Injectable({ providedIn: 'root' })
export class ChatStore {
  private readonly activeConversation = signal<string | null>(null);
  private readonly conversationsState = signal<Conversation[]>([]);
  private readonly messagesByConversation = signal<Record<string, Message[]>>({});
  private readonly typingUsersByConversation = signal<Record<string, string[]>>({});
  private readonly readMarkerByConversation = signal<Record<string, string>>({});
  private readonly loadingState = signal(false);

  readonly activeConversationId = this.activeConversation.asReadonly();
  readonly conversations = this.conversationsState.asReadonly();
  readonly typingUsers = this.typingUsersByConversation.asReadonly();
  readonly isLoading = this.loadingState.asReadonly();

  constructor(
    private readonly signalr: SignalrService,
    private readonly chatApi: ChatApiService) {}

  messagesForActiveConversation(): Message[] {
    const conversationId = this.activeConversation();
    if (!conversationId) return [];
    return this.messagesByConversation()[conversationId] ?? [];
  }

  async connectRealtime(): Promise<void> {
    await this.signalr.connect();
    this.signalr.on<Message>('ReceiveMessage', message => {
      this.messagesByConversation.update(state => {
        const current = state[message.conversationId] ?? [];
        return { ...state, [message.conversationId]: [...current, message] };
      });
      this.bumpConversationActivity(message.conversationId, message);
    });
    this.signalr.on<{ conversationId: string; userId: string; isTyping: boolean }>('UserTyping', event => {
      this.typingUsersByConversation.update(state => {
        const current = new Set(state[event.conversationId] ?? []);
        if (event.isTyping) current.add(event.userId);
        else current.delete(event.userId);
        return { ...state, [event.conversationId]: Array.from(current) };
      });
    });
  }

  async setActiveConversation(conversationId: string): Promise<void> {
    if (this.activeConversation() === conversationId) return;
    const previous = this.activeConversation();
    if (previous) await this.signalr.invoke('LeaveConversation', previous);

    this.loadingState.set(true);
    this.activeConversation.set(conversationId);
    await this.signalr.invoke('JoinConversation', conversationId);
    if (!this.messagesByConversation()[conversationId]) {
      const messages = await firstValueFrom(this.chatApi.getMessages(conversationId, 50));
      this.messagesByConversation.update(state => ({ ...state, [conversationId]: messages }));
    }
    this.loadingState.set(false);
  }

  async sendMessage(content: string): Promise<void> {
    const conversationId = this.activeConversation();
    if (!conversationId) throw new Error('No active conversation selected');
    await this.signalr.invoke('SendMessage', {
      conversationId,
      content,
      messageType: 'text'
    });
  }

  async loadConversations(): Promise<void> {
    this.loadingState.set(true);
    const result = await firstValueFrom(this.chatApi.getConversations());
    this.conversationsState.set(result.items);
    this.loadingState.set(false);
  }

  async startTyping(): Promise<void> {
    const conversationId = this.activeConversation();
    if (!conversationId) return;
    await this.signalr.invoke('StartTyping', conversationId);
  }

  async stopTyping(): Promise<void> {
    const conversationId = this.activeConversation();
    if (!conversationId) return;
    await this.signalr.invoke('StopTyping', conversationId);
  }

  async markRead(lastReadMessageId: string): Promise<void> {
    const conversationId = this.activeConversation();
    if (!conversationId) return;
    this.readMarkerByConversation.update(state => ({ ...state, [conversationId]: lastReadMessageId }));
    await this.signalr.invoke('MarkMessagesRead', conversationId, lastReadMessageId);
  }

  private bumpConversationActivity(conversationId: string, message: Message): void {
    this.conversationsState.update(list => {
      const index = list.findIndex(c => c.id === conversationId);
      if (index < 0) return list;
      const updated = {
        ...list[index],
        lastActivityAt: message.createdAt,
        lastMessage: message
      };
      const clone = [...list];
      clone.splice(index, 1);
      return [updated, ...clone];
    });
  }
}
