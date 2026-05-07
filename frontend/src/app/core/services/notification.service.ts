import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  async requestPermission(): Promise<NotificationPermission> {
    return Notification.requestPermission();
  }

  show(title: string, body: string): void {
    if (Notification.permission === 'granted') {
      new Notification(title, { body });
    }
  }
}
