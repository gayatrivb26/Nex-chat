import { Injectable, computed, signal } from '@angular/core';
import { Call } from '../models';
import { SignalrService } from './signalr.service';
import { CallSignalingService } from './call-signaling.service';

@Injectable({ providedIn: 'root' })
export class CallStateService {
  private readonly incoming = signal<Call | null>(null);
  private readonly active = signal<Call | null>(null);
  private readonly state = signal<'idle' | 'ringing' | 'connecting' | 'active' | 'ended'>('idle');
  private subscribed = false;

  readonly incomingCall = this.incoming.asReadonly();
  readonly activeCall = this.active.asReadonly();
  readonly callState = this.state.asReadonly();
  readonly hasActiveCall = computed(() => this.state() === 'active' || this.state() === 'connecting');

  constructor(
    private readonly signalr: SignalrService,
    private readonly callSignaling: CallSignalingService) {}

  async initialize(): Promise<void> {
    await this.signalr.connect();
    if (this.subscribed) return;

    this.signalr.on<Call>('CallIncoming', call => {
      this.incoming.set(call);
      this.state.set('ringing');
    });
    this.signalr.on<string>('CallAnswered', callId => {
      const incoming = this.incoming();
      if (incoming?.id === callId) {
        this.active.set(incoming);
        this.incoming.set(null);
      }
      this.state.set('active');
    });
    this.signalr.on<string>('CallRejected', _ => {
      this.incoming.set(null);
      this.active.set(null);
      this.state.set('ended');
      queueMicrotask(() => this.state.set('idle'));
    });
    this.signalr.on<string>('CallEnded', _ => {
      this.incoming.set(null);
      this.active.set(null);
      this.state.set('ended');
      queueMicrotask(() => this.state.set('idle'));
    });

    this.subscribed = true;
  }

  async startOutgoing(call: Call): Promise<void> {
    this.active.set(call);
    this.state.set('connecting');
  }

  async endCurrentCall(): Promise<void> {
    const callId = this.active()?.id ?? this.incoming()?.id;
    if (callId) {
      await this.callSignaling.endCall(callId);
    }
    this.incoming.set(null);
    this.active.set(null);
    this.state.set('ended');
    queueMicrotask(() => this.state.set('idle'));
  }

  async rejectIncoming(): Promise<void> {
    const callId = this.incoming()?.id;
    if (callId) await this.callSignaling.rejectCall(callId);
    this.incoming.set(null);
    this.state.set('idle');
  }
}
