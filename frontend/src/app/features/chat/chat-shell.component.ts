import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';
import { ChangeDetectionStrategy, Component, OnDestroy, ViewChild, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CallStateService } from '../../core/services/call-state.service';
import { ChatStore } from './chat.store';

@Component({
  standalone: true,
  selector: 'app-chat-shell',
  imports: [FormsModule, ScrollingModule],
  template: `
    <main class="chat">
      @if (callState.callState() === 'ringing' && callState.incomingCall()) {
        <div class="call-banner">
          <span>Incoming {{ callState.incomingCall()!.callType }} call</span>
          <button type="button" (click)="rejectIncomingCall()">Reject</button>
        </div>
      }
      @if (callState.hasActiveCall()) {
        <div class="call-banner active">
          <span>Call in progress...</span>
          <button type="button" (click)="endCall()">End</button>
        </div>
      }
      <aside class="list">
        <header>Chats</header>
        @if (store.isLoading()) {
          <p class="loading">Loading...</p>
        } @else {
          <div class="conversation-list">
            @for (conversation of conversations(); track conversation.id) {
              <button
                type="button"
                class="conversation-item"
                [class.active]="conversation.id === store.activeConversationId()"
                (click)="setConversation(conversation.id)">
                <strong>{{ conversation.name || 'Direct Chat' }}</strong>
                <small>{{ conversation.lastMessage?.content || 'No messages yet' }}</small>
              </button>
            }
          </div>
        }
      </aside>
      <section class="conversation">
        <header class="topbar">Conversation</header>
        <cdk-virtual-scroll-viewport itemSize="72" class="messages">
          <article *cdkVirtualFor="let message of messages()" class="bubble">
            <span>{{ message.content }}</span>
            <time>{{ message.createdAt }}</time>
          </article>
        </cdk-virtual-scroll-viewport>
        @if (typingUsers().length > 0) {
          <p class="typing">{{ typingUsers().join(', ') }} typing...</p>
        }
        <form class="composer" (ngSubmit)="send()">
          <input name="message" [(ngModel)]="draft" placeholder="Message" (input)="onInput()" />
          <button type="submit">Send</button>
        </form>
      </section>
    </main>
  `,
  styles: [`
    .chat { height: 100vh; display: grid; grid-template-columns: 320px 1fr; background: #eef4f2; }
    .call-banner { position: fixed; top: 16px; left: 50%; transform: translateX(-50%); z-index: 10; background: #fff4e5; border: 1px solid #f59e0b; color: #92400e; padding: 8px 12px; border-radius: 8px; display: flex; gap: 8px; align-items: center; }
    .call-banner.active { background: #ecfdf9; border-color: #0f766e; color: #0f766e; }
    .call-banner button { min-height: 32px; padding: 0 10px; border-radius: 6px; border: 0; background: #b91c1c; color: #fff; cursor: pointer; }
    .list { border-right: 1px solid #cedbd7; background: #fbfdfc; display: grid; grid-template-rows: 56px 1fr; padding: 12px; gap: 8px; }
    .list header, .topbar { font-weight: 700; display: flex; align-items: center; }
    .loading { color: #49615a; font-size: 13px; }
    .conversation-list { display: grid; gap: 8px; overflow: auto; }
    .conversation-item { display: grid; gap: 2px; text-align: left; padding: 10px; border-radius: 8px; border: 1px solid #d9e4e1; background: #fff; cursor: pointer; }
    .conversation-item.active { border-color: #0f766e; background: #ecfdf9; }
    .conversation-item small { color: #4f635d; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .conversation { min-width: 0; display: grid; grid-template-rows: 56px 1fr 64px; }
    .topbar { border-bottom: 1px solid #cedbd7; padding: 0 16px; background: #fbfdfc; }
    .messages { height: 100%; padding: 16px; }
    .bubble { width: min(620px, 84%); margin: 0 0 10px auto; padding: 10px 12px; border-radius: 8px; background: #d7f7e8; display: grid; gap: 4px; }
    .bubble time { font-size: 12px; color: #49615a; justify-self: end; }
    .typing { margin: 0; padding: 0 16px 8px; font-size: 12px; color: #49615a; }
    .composer { display: grid; grid-template-columns: 1fr 88px; gap: 10px; padding: 10px; border-top: 1px solid #cedbd7; background: #fbfdfc; }
    input, button { border-radius: 6px; border: 1px solid #c8d6d1; padding: 0 12px; }
    button { border: 0; background: #0f766e; color: white; font-weight: 700; }
    @media (max-width: 760px) { .chat { grid-template-columns: 1fr; } .list { display: none; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChatShellComponent implements OnDestroy {
  @ViewChild(CdkVirtualScrollViewport) viewport?: CdkVirtualScrollViewport;

  draft = '';
  private stopTypingTimer?: ReturnType<typeof setTimeout>;
  readonly conversations = computed(() => this.store.conversations());
  readonly messages = computed(() => this.store.messagesForActiveConversation());
  readonly typingUsers = computed(() => this.store.typingUsers()[this.store.activeConversationId() ?? ''] ?? []);

  constructor(
    readonly store: ChatStore,
    readonly callState: CallStateService) {
    void this.bootstrapRealtime();
  }

  async send(): Promise<void> {
    const text = this.draft.trim();
    if (!text || !this.store.activeConversationId()) return;
    this.draft = '';
    await this.store.sendMessage(text);
    await this.store.stopTyping();
    queueMicrotask(() => this.viewport?.scrollToIndex(this.messages().length - 1));
    const lastMessage = this.messages().at(-1);
    if (lastMessage) await this.store.markRead(lastMessage.id);
  }

  onInput(): void {
    if (!this.store.activeConversationId()) return;
    void this.store.startTyping();
    if (this.stopTypingTimer) clearTimeout(this.stopTypingTimer);
    this.stopTypingTimer = setTimeout(() => void this.store.stopTyping(), 1500);
  }

  ngOnDestroy(): void {
    if (this.stopTypingTimer) clearTimeout(this.stopTypingTimer);
    void this.store.stopTyping();
  }

  private async bootstrapRealtime(): Promise<void> {
    await this.store.connectRealtime();
    await this.callState.initialize();
    await this.store.loadConversations();
    const firstConversationId = this.conversations().at(0)?.id;
    if (firstConversationId) await this.store.setActiveConversation(firstConversationId);
  }

  setConversation(conversationId: string): void {
    const value = conversationId.trim();
    if (!value) return;
    void this.store.setActiveConversation(value);
  }

  rejectIncomingCall(): void {
    void this.callState.rejectIncoming();
  }

  endCall(): void {
    void this.callState.endCurrentCall();
  }
}
