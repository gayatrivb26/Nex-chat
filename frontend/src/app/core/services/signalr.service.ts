import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { TokenService } from '../auth/token.service';

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private connection?: signalR.HubConnection;
  private connectPromise?: Promise<void>;
  readonly isConnected = signal(false);

  constructor(private readonly tokens: TokenService) {}

  async connect(): Promise<void> {
    if (this.connection && this.isConnected()) return;
    if (this.connectPromise) return this.connectPromise;

    this.connectPromise = (async () => {
      const wsBase = (window as any).__APP_CONFIG__?.wsBase ?? '/hubs';
      const base = wsBase.endsWith('/') ? wsBase.slice(0, -1) : wsBase;
      const hubUrl = `${base}/chat`;

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, { accessTokenFactory: () => this.tokens.accessToken() ?? '' })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      this.connection.onclose(() => this.isConnected.set(false));
      this.connection.onreconnected(() => this.isConnected.set(true));
      await this.connection.start();
      this.isConnected.set(true);
    })();

    try {
      await this.connectPromise;
    } finally {
      this.connectPromise = undefined;
    }
  }

  on<T>(eventName: string, handler: (payload: T) => void): void {
    this.connection?.on(eventName, handler);
  }

  invoke<T = unknown>(methodName: string, ...args: unknown[]): Promise<T> {
    if (!this.connection) throw new Error('SignalR is not connected');
    return this.connection.invoke<T>(methodName, ...args);
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this.isConnected.set(false);
  }
}
