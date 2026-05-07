import { Injectable } from '@angular/core';
import { interval, startWith, switchMap } from 'rxjs';
import { SignalrService } from './signalr.service';

@Injectable({ providedIn: 'root' })
export class PresenceService {
  constructor(private readonly signalr: SignalrService) {}

  heartbeat$ = interval(20_000).pipe(
    startWith(0),
    switchMap(() => this.signalr.invoke('Heartbeat').catch(() => undefined))
  );
}
